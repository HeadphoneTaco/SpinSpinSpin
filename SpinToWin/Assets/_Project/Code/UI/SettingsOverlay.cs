using _Project.Code.Core;
using CoreUtils.GameEvents;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Code.UI {
    /// <summary>
    ///     Shows the settings panel while the game is in the Settings state and hides it otherwise.
    ///     Settings is an overlay now (see <see cref="GameManager.OpenSettings" />): it no longer
    ///     loads its own scene, so this same prefab is dropped into every scene that needs settings
    ///     (the title screen / Start scene and the gameplay / Main scene). Each instance just listens
    ///     to the shared StateEntered / StateExited events and reacts when Settings is the state —
    ///     instances in inactive scenes simply never see their state become active.
    ///
    ///     Opening is done by a <see cref="GameStateButton" /> set to Action.Settings; closing by one
    ///     set to Action.CloseSettings (or the Esc key here). Closing returns to whatever state
    ///     settings was opened from — MainMenu from the title screen, Paused from the pause menu.
    ///     Place this on the SettingsOverlay prefab root and assign the panel + the two events.
    /// </summary>
    public class SettingsOverlay : MonoBehaviour {
        [Header("Panel")]
        [Tooltip("The settings panel root to show while in the Settings state. Usually a child of this object.")]
        [SerializeField] private GameObject panel;

        [Header("Input")]
        [Tooltip("Let Esc / gamepad B close the overlay while it's open.")]
        [SerializeField] private bool closeOnCancel = true;

        [Header("Events")]
        [SerializeField] private GameEventString stateEntered;
        [SerializeField] private GameEventString stateExited;

        private InputAction _cancelAction;

        private void OnEnable() {
            // Footgun guard: the panel must be a SEPARATE object from the one this component lives on.
            // OnEnable only runs while this GameObject is active, so if we hid ourselves we could never
            // hear the next "Settings entered" to come back — the overlay would be stuck invisible and
            // un-closeable. Assign a child Panel, not this root.
            if (panel == gameObject) {
                Debug.LogError("[SettingsOverlay] Panel is set to this same object. Point it at a " +
                               "child panel instead, or the overlay will disable itself and never " +
                               "reopen.", this);
            }

            if (stateEntered != null) {
                stateEntered.Event += HandleStateEntered;
            }

            if (stateExited != null) {
                stateExited.Event += HandleStateExited;
            }

            if (closeOnCancel) {
                _cancelAction = new InputAction("CloseSettings", InputActionType.Button);
                _cancelAction.AddBinding("<Keyboard>/escape");
                _cancelAction.AddBinding("<Gamepad>/buttonEast"); // B / circle
                _cancelAction.performed += OnCancelPressed;
                _cancelAction.Enable();
            }

            // Match whatever the current state already is (e.g. if enabled mid-Settings).
            SetPanelVisible(GameManager.Exists && GameManager.Instance.IsState(GameStateNames.Settings));
        }

        private void OnDisable() {
            if (stateEntered != null) {
                stateEntered.Event -= HandleStateEntered;
            }

            if (stateExited != null) {
                stateExited.Event -= HandleStateExited;
            }

            if (_cancelAction != null) {
                _cancelAction.performed -= OnCancelPressed;
                _cancelAction.Dispose();
                _cancelAction = null;
            }
        }

        private void HandleStateEntered(string stateName) {
            if (stateName == GameStateNames.Settings) {
                SetPanelVisible(true);
            }
        }

        private void HandleStateExited(string stateName) {
            if (stateName == GameStateNames.Settings) {
                SetPanelVisible(false);
            }
        }

        private void OnCancelPressed(InputAction.CallbackContext context) {
            // Only act while we're actually the open overlay; otherwise leave Esc to the pause menu.
            if (GameManager.Exists && GameManager.Instance.IsState(GameStateNames.Settings)) {
                GameManager.Instance.CloseSettings();
            }
        }

        private void SetPanelVisible(bool visible) {
            if (panel != null) {
                panel.SetActive(visible);
            }
        }
    }
}
