using _Project.Code.Core;
using CoreUtils.GameEvents;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     The sock-gremlin the player drives down the drum. This is a "treadmill" runner: the
    ///     gremlin holds a fixed forward position (its Z never changes) while the world scrolls
    ///     toward it (see <see cref="ScrollingItem" />), which is exactly what the fixed drum
    ///     camera needs. So the gremlin only does two things - steer left/right across the drum
    ///     and hop. Steering is analog (Temple Run feel): the further you push, the faster it
    ///     slides, smoothed and clamped to the drum's half-width.
    ///
    ///     Input is code-defined (no Input Action asset to wire) and live only while the game is
    ///     in the <see cref="GameStateNames.Playing" /> state, mirroring <c>Spinner</c> and
    ///     <c>PauseMenuController</c>. While Paused / in menus the gremlin freezes.
    ///
    ///     Setup: the <see cref="CharacterController" /> is both the gremlin's collider and its mover,
    ///     so it must sit on the moving root (Unity won't let those be split onto separate objects).
    ///     This script may live on the root or on a "Code" child - it finds the controller on itself,
    ///     its parent, or a child. The gremlin also scans for overlapping socks/obstacles each frame
    ///     (see <see cref="ScanForItems" />), so items need no trigger script of their own. Assign the
    ///     <c>StateEntered</c> / <c>StateExited</c> event assets so input follows game state.
    /// </summary>
    public class GremlinRunner : MonoBehaviour {
        [SerializeField] private float steerSpeed = 8f;
        [SerializeField] private float halfWidth = 2.5f;
        [SerializeField] private float steerSmoothing = 12f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -25f;

        [Tooltip("Which layers to scan for socks/obstacles. Default = Everything (filtered to items " +
                 "that have a ScrollingItem). Put items on an 'Items' layer and set it here to tighten.")]
        [SerializeField] private LayerMask itemMask = ~0;

        [SerializeField] private GameEventString stateEntered;
        [SerializeField] private GameEventString stateExited;

        private CharacterController _controller;
        private readonly Collider[] _itemHits = new Collider[16];
        private InputAction _steerAction;
        private InputAction _jumpAction;

        private float _horizontalVelocity; // smoothed sideways velocity
        private float _verticalVelocity;   // gravity + jump
        private bool _inputActive;

        private void Awake() {
            // Controller lives on the moving root, which may be this object, a parent, or a child.
            _controller = GetComponent<CharacterController>();
            if (_controller == null) {
                // Search the whole gremlin: parent, a sibling 'Colliders' object, or children.
                _controller = transform.root.GetComponentInChildren<CharacterController>(true);
            }

            if (_controller == null) {
                Debug.LogError("[GremlinRunner] No CharacterController found on the gremlin or its " +
                               "parent/children. Add one to the gremlin root.", this);
            }
        }

        private void OnEnable() {
            // 1D axis so keyboard reads like a stick: -1 (left) .. +1 (right).
            _steerAction = new InputAction("Steer", expectedControlType: "Axis");
            _steerAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            _steerAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            _steerAction.AddBinding("<Gamepad>/leftStick/x");

            _jumpAction = new InputAction("Jump", InputActionType.Button);
            _jumpAction.AddBinding("<Keyboard>/space");
            _jumpAction.AddBinding("<Keyboard>/upArrow");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");

            if (stateEntered != null) {
                stateEntered.Event += HandleStateEntered;
            }

            if (stateExited != null) {
                stateExited.Event += HandleStateExited;
            }

            // Match whatever state is already active when this scene loads in (we arrive mid-Playing).
            SetInputActive(GameManager.Exists && GameManager.Instance.IsPlaying);
        }

        private void OnDisable() {
            if (stateEntered != null) {
                stateEntered.Event -= HandleStateEntered;
            }

            if (stateExited != null) {
                stateExited.Event -= HandleStateExited;
            }

            _steerAction?.Dispose();
            _jumpAction?.Dispose();
        }

        private void Update() {
            // Frozen in menus / pause. Time still flows, but the gremlin shouldn't.
            if (!_inputActive || _controller == null) {
                return;
            }

            float steerInput = _steerAction.ReadValue<float>();
            float targetVelocity = steerInput * steerSpeed;
            _horizontalVelocity = Mathf.Lerp(_horizontalVelocity, targetVelocity, steerSmoothing * Time.deltaTime);

            if (_controller.isGrounded) {
                // Small downward bias keeps isGrounded reliable between frames.
                if (_verticalVelocity < 0f) {
                    _verticalVelocity = -2f;
                }

                if (_jumpAction.WasPerformedThisFrame()) {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }

            _verticalVelocity += gravity * Time.deltaTime;

            // No forward (Z) component - the world scrolls toward us instead.
            Vector3 velocity = new Vector3(_horizontalVelocity, _verticalVelocity, 0f);
            _controller.Move(velocity * Time.deltaTime);

            ClampToDrum();
            ScanForItems();
        }

        /// <summary>
        ///     Sweeps the gremlin's own capsule for overlapping socks/obstacles and reports each hit.
        ///     Detection lives here (not on the items) so the items keep their script on `Code` and
        ///     their collider on `Colliders` with nothing extra - Unity routes trigger messages to the
        ///     collider's object, which we deliberately sidestep by scanning instead.
        /// </summary>
        private void ScanForItems() {
            Transform mover = _controller.transform;
            Vector3 scale = mover.lossyScale;
            float radius = _controller.radius * Mathf.Max(scale.x, scale.z);
            float height = Mathf.Max(_controller.height * scale.y, radius * 2f);
            Vector3 center = mover.TransformPoint(_controller.center);
            Vector3 axis = mover.up * (height * 0.5f - radius);

            int count = Physics.OverlapCapsuleNonAlloc(center - axis, center + axis, radius,
                _itemHits, itemMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++) {
                Collider hit = _itemHits[i];
                // Script is on `Code`; the collider we hit is on a `Colliders` sibling - so if the
                // direct parent lookup misses, search the item's whole hierarchy from its root.
                ScrollingItem item = hit.GetComponentInParent<ScrollingItem>();
                if (item == null) {
                    item = hit.transform.root.GetComponentInChildren<ScrollingItem>(true);
                }

                if (item != null) {
                    item.HitByPlayer(this);
                }
            }
        }

        /// <summary>Keeps the gremlin inside the drum and kills sideways momentum on contact with the wall.</summary>
        private void ClampToDrum() {
            // Clamp the object the controller moves (the root), not necessarily this script's object.
            Transform mover = _controller.transform;
            Vector3 position = mover.position;
            if (Mathf.Abs(position.x) > halfWidth) {
                position.x = Mathf.Clamp(position.x, -halfWidth, halfWidth);
                mover.position = position;
                _horizontalVelocity = 0f;
            }
        }

        private void HandleStateEntered(string stateName) {
            if (stateName == GameStateNames.Playing) {
                SetInputActive(true);
            }
        }

        private void HandleStateExited(string stateName) {
            if (stateName == GameStateNames.Playing) {
                SetInputActive(false);
            }
        }

        private void SetInputActive(bool active) {
            _inputActive = active;
            if (active) {
                _steerAction.Enable();
                _jumpAction.Enable();
            } else {
                _steerAction.Disable();
                _jumpAction.Disable();
                _horizontalVelocity = 0f;
            }
        }
    }
}
