using _Project.Code.Core;
using CoreUtils.GameEvents;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     The sock-gremlin the player drives down the drum. This is a "treadmill" runner: the
    ///     gremlin holds a fixed forward position (its Z never changes) while the world scrolls
    ///     toward it (see <see cref="ScrollingItem" />), which is exactly what the fixed drum
    ///     camera needs. So the gremlin only does two things — steer left/right across the drum
    ///     and hop. Steering is analog (Temple Run feel): the further you push, the faster it
    ///     slides, smoothed and clamped to the drum's half-width.
    ///
    ///     Input is code-defined (no Input Action asset to wire) and live only while the game is
    ///     in the <see cref="GameStateNames.Playing" /> state, mirroring <c>Spinner</c> and
    ///     <c>PauseMenuController</c>. While Paused / in menus the gremlin freezes.
    ///
    ///     Setup: drop this on the gremlin root with a <see cref="CharacterController" />. For the
    ///     trigger pickups/hazards to register, the gremlin also needs a <b>kinematic Rigidbody</b>
    ///     (the items carry the trigger colliders). Assign the <c>StateEntered</c> / <c>StateExited</c>
    ///     event assets so input follows game state.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class GremlinRunner : MonoBehaviour {
        [SerializeField] private float steerSpeed = 8f;
        [SerializeField] private float halfWidth = 2.5f;
        [SerializeField] private float steerSmoothing = 12f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private GameEventString stateEntered;
        [SerializeField] private GameEventString stateExited;

        private CharacterController _controller;
        private InputAction _steerAction;
        private InputAction _jumpAction;

        private float _horizontalVelocity; // smoothed sideways velocity
        private float _verticalVelocity;   // gravity + jump
        private bool _inputActive;

        private void Awake() {
            _controller = GetComponent<CharacterController>();
        }

        private void OnEnable() {
            // 1D axis so keyboard reads like a stick: -1 (left) - +1 (right).
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
            if (!_inputActive) {
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

            // No forward (Z) component — the world scrolls toward us instead.
            Vector3 velocity = new Vector3(_horizontalVelocity, _verticalVelocity, 0f);
            _controller.Move(velocity * Time.deltaTime);

            ClampToDrum();
        }

        /// <summary>Keeps the gremlin inside the drum and kills sideways momentum on contact with the wall.</summary>
        private void ClampToDrum() {
            Vector3 position = transform.position;
            if (Mathf.Abs(position.x) > halfWidth) {
                position.x = Mathf.Clamp(position.x, -halfWidth, halfWidth);
                transform.position = position;
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
