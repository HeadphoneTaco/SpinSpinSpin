using _Project.Code.Core;
using CoreUtils.GameEvents;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     The sock-gremlin the player drives down the drum. A "treadmill" runner: the gremlin holds a
    ///     fixed Z while the world scrolls toward it (see <see cref="ScrollingItem" />), so it only
    ///     steers left/right across a flat lane and hops. Steering is analog (Temple Run feel): the
    ///     further you push, the faster it slides, smoothed and clamped to the drum's half-width.
    ///
    ///     Input is code-defined and live only in the <see cref="GameStateNames.Playing" /> state,
    ///     mirroring <c>PauseMenuController</c>. The gremlin scans for overlapping socks/obstacles each
    ///     frame (see <see cref="ScanForItems" />), so items need no trigger script of their own.
    ///
    ///     Setup: a <see cref="CharacterController" /> on the moving root (it's both collider and mover,
    ///     so it can't be split). The script finds it on itself or anywhere in the gremlin. Needs a
    ///     floor collider under it to stay grounded. Assign the StateEntered / StateExited events.
    /// </summary>
    public class GremlinRunner : MonoBehaviour {
        [Header("Steering")]
        [SerializeField] private float steerSpeed = 8f;
        [SerializeField] private float halfWidth = 2.5f;
        [SerializeField] private float steerSmoothing = 12f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -25f;

        [Header("Item scan")]
        [Tooltip("Which layers to scan for socks/obstacles. Default = Everything (filtered to ScrollingItem).")]
        [SerializeField] private LayerMask itemMask = ~0;

        [Header("State events")]
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
            _controller = GetComponent<CharacterController>();
            if (_controller == null) {
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
            if (!_inputActive || _controller == null) {
                return;
            }

            float steerInput = _steerAction.ReadValue<float>();
            float targetVelocity = steerInput * steerSpeed;
            _horizontalVelocity = Mathf.Lerp(_horizontalVelocity, targetVelocity, steerSmoothing * Time.deltaTime);

            if (_controller.isGrounded) {
                if (_verticalVelocity < 0f) {
                    _verticalVelocity = -2f; // small downward bias keeps isGrounded reliable
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

        /// <summary>Keeps the gremlin inside the drum and kills sideways momentum on contact with the wall.</summary>
        private void ClampToDrum() {
            Transform mover = _controller.transform;
            Vector3 position = mover.position;
            if (Mathf.Abs(position.x) > halfWidth) {
                position.x = Mathf.Clamp(position.x, -halfWidth, halfWidth);
                mover.position = position;
                _horizontalVelocity = 0f;
            }
        }

        /// <summary>
        ///     Sweeps the gremlin's own capsule for overlapping socks/obstacles and reports each hit.
        ///     Detection lives here (not on the items) so the items keep their script on `Code` and
        ///     their collider on `Colliders` with nothing extra.
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
                ScrollingItem item = hit.GetComponentInParent<ScrollingItem>();
                if (item == null) {
                    item = hit.transform.root.GetComponentInChildren<ScrollingItem>(true);
                }

                if (item != null) {
                    item.HitByPlayer(this);
                }
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
