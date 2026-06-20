using _Project.Code.Core;
using CoreUtils.GameEvents;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _Project.Code.UI {
    /// <summary>
    ///     Shows the pause panel while the game is in the Paused state and wires its buttons.
    ///     Reacts to the StateEntered / StateExited GameEvents, and toggles pause from a
    ///     code-defined input (Esc / gamepad Start). Place this in the gameplay scene with the
    ///     pause panel as <see cref="pausePanel" />, and assign the two state events.
    /// </summary>
    public class PauseMenuController : MonoBehaviour {
        [Header("Panel")]
        [SerializeField] private GameObject pausePanel;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;

        [Tooltip("Settings panel opened from the pause menu. Optional.")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Events (GameEventString, payload = state name)")]
        [SerializeField] private GameEventString stateEntered;
        [SerializeField] private GameEventString stateExited;

        private InputAction _pauseAction;

        private void OnEnable() {
            // Esc / Start toggles pause. Always enabled — TogglePause itself only acts while
            // Playing or Paused, so it's a no-op in menus.
            _pauseAction = new InputAction("Pause", InputActionType.Button);
            _pauseAction.AddBinding("<Keyboard>/escape");
            _pauseAction.AddBinding("<Gamepad>/start");
            _pauseAction.performed += OnPausePressed;
            _pauseAction.Enable();

            if (stateEntered != null) {
                stateEntered.Event += HandleStateEntered;
            }

            if (stateExited != null) {
                stateExited.Event += HandleStateExited;
            }

            if (resumeButton != null) {
                resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (settingsButton != null) {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (mainMenuButton != null) {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }

            // Start hidden unless we somehow enable mid-pause.
            SetPanelVisible(GameManager.Exists && GameManager.Instance.IsPaused);
        }

        private void OnDisable() {
            _pauseAction.performed -= OnPausePressed;
            _pauseAction.Dispose();

            if (stateEntered != null) {
                stateEntered.Event -= HandleStateEntered;
            }

            if (stateExited != null) {
                stateExited.Event -= HandleStateExited;
            }

            if (resumeButton != null) {
                resumeButton.onClick.RemoveListener(OnResumeClicked);
            }

            if (settingsButton != null) {
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (mainMenuButton != null) {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }
        }

        private void OnPausePressed(InputAction.CallbackContext context) {
            if (GameManager.Exists) {
                GameManager.Instance.TogglePause();
            }
        }

        private void HandleStateEntered(string stateName) {
            if (stateName == GameStateNames.Paused) {
                SetPanelVisible(true);
            }
        }

        private void HandleStateExited(string stateName) {
            if (stateName == GameStateNames.Paused) {
                SetPanelVisible(false);
            }
        }

        private void OnResumeClicked() {
            GameManager.Instance.Resume();
        }

        private void OnSettingsClicked() {
            if (settingsPanel != null) {
                settingsPanel.SetActive(true);
            }
        }

        private void OnMainMenuClicked() {
            GameManager.Instance.ReturnToMenu();
        }

        private void SetPanelVisible(bool visible) {
            if (pausePanel != null) {
                pausePanel.SetActive(visible);
            }
        }
    }
}
