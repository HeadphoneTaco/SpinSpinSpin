using _Project.Code.Core;
using _Project.Code.Core.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     Minimal "spin to win" test piece: press the spin input to give the target a
    ///     burst of angular velocity, which decays back to rest. Plays a spin SFX on input
    ///     and a land SFX when it stops. Input is only active while the game is in the
    ///     Playing state. Built to exercise the Input System + AudioManager wiring.
    /// </summary>
    public class Spinner : MonoBehaviour {
        
        [SerializeField] private Transform spinTarget;
        [Header("Feel")]
        [SerializeField] private float spinImpulse = 720f;
        [SerializeField] private float maxSpeed = 1440f;
        [SerializeField] private float decay = 540f;
        [Header("Audio")]
        [SerializeField] private AudioClip spinSfx;
        [SerializeField] private AudioClip landSfx;

        private InputAction _spinAction;
        private float _angularVelocity;
        private bool _isSpinning;

        private void OnEnable() {
            // Code-defined action so no Input Action asset wiring is needed to test.
            _spinAction = new InputAction("Spin", InputActionType.Button);
            _spinAction.AddBinding("<Keyboard>/space");
            _spinAction.AddBinding("<Gamepad>/buttonSouth");
            _spinAction.AddBinding("<Mouse>/leftButton");
            _spinAction.performed += OnSpinPerformed;

            StateManager state = GameManager.Instance.State;
            state.OnStateChanged += HandleStateChanged;
            SetInputActive(state.IsPlaying);
        }

        private void OnDisable() {
            if (GameManager.Exists) {
                GameManager.Instance.State.OnStateChanged -= HandleStateChanged;
            }

            _spinAction.performed -= OnSpinPerformed;
            _spinAction.Dispose();
        }

        private void Update() {
            if (_angularVelocity <= 0f) {
                return;
            }

            Transform target = spinTarget != null ? spinTarget : transform;
            target.Rotate(0f, 0f, -_angularVelocity * Time.deltaTime);

            _angularVelocity -= decay * Time.deltaTime;
            if (_angularVelocity <= 0f) {
                _angularVelocity = 0f;
                if (_isSpinning) {
                    _isSpinning = false;
                    OnLanded();
                }
            }
        }

        private void OnSpinPerformed(InputAction.CallbackContext context) {
            _angularVelocity = Mathf.Min(_angularVelocity + spinImpulse, maxSpeed);
            _isSpinning = true;
            GameManager.Instance.Audio.PlaySfx(spinSfx);
        }

        private void OnLanded() {
            GameManager.Instance.Audio.PlaySfx(landSfx);
            float facing = (spinTarget != null ? spinTarget : transform).eulerAngles.z;
            Debug.Log($"[Spinner] Landed at {facing:0}°.");
        }

        private void HandleStateChanged(GameState previous, GameState current) {
            SetInputActive(current == GameState.Playing);
        }

        private void SetInputActive(bool active) {
            if (active) {
                _spinAction.Enable();
            } else {
                _spinAction.Disable();
            }
        }
    }
}
