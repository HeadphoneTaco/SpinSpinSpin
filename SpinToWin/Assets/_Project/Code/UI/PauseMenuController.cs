using System.Collections;
using _Project.Code.Core;
using CoreUtils.GameEvents;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _Project.Code.UI {
    /// <summary>
    ///     Shows the pause menu during the Paused state, built like <see cref="SettingsOverlay" /> and
    ///     <see cref="GameOverScreen" /> for consistency: this component lives on the always-active
    ///     PauseOverlay root and toggles a child Pause Panel (plus the countdown root), rather than
    ///     switching itself off. It also owns the bits unique to pause: Esc / gamepad Start toggles
    ///     pause, and resuming plays a short 3-2-1 countdown before gameplay un-freezes (the game stays
    ///     frozen - still Paused, since every gameplay system gates on IsPlaying - while the count runs,
    ///     then this calls GameManager.Resume() to enter Playing).
    ///
    ///     Hierarchy (matches the other overlays): PauseOverlay (this script, always active) > Pause
    ///     Panel (the painted menu + buttons) and Countdown Root as children. Assign the pause panel,
    ///     the Resume button, the countdown UI, and the StateEntered / StateExited events.
    /// </summary>
    public class PauseMenuController : MonoBehaviour {
        [Header("Panel")]
        [SerializeField] private GameObject pausePanel;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;

        // Settings is opened by a GameStateButton (Action.Settings) on the pause panel and shown by
        // the state-driven SettingsOverlay — this controller no longer owns that show/hide logic.

        [Header("Resume countdown")]
        [Tooltip("Shown during the 3-2-1 count, hidden otherwise. Usually the countdown number's parent object.")]
        [SerializeField] private GameObject countdownRoot;
        [SerializeField] private TMP_Text countdownText;
        [Tooltip("Seconds to count down before gameplay resumes. 0 = resume instantly.")]
        [SerializeField] private float countdownSeconds = 3f;

        [Header("Events")]
        [SerializeField] private GameEventString stateEntered;
        [SerializeField] private GameEventString stateExited;

        private InputAction _pauseAction;
        private Coroutine _resuming;

        private void OnEnable() {
            // Footgun guard (same as SettingsOverlay / GameOverScreen): the Pause Panel must be a
            // SEPARATE object from the one this component lives on. OnEnable only runs while this object
            // is active, so if we hid ourselves we'd stop hearing the next "Paused entered" and the menu
            // would be stuck. Put this on the always-active PauseOverlay root and point Pause Panel at a child.
            if (pausePanel == gameObject) {
                Debug.LogError("[PauseMenuController] Pause Panel is set to this same object. Point it " +
                               "at a child panel instead, or the overlay disables itself and never reopens.", this);
            }

            // Esc / Start toggles pause. Always enabled; the handler itself only acts while
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
                resumeButton.onClick.AddListener(BeginResume);
            }

            if (mainMenuButton != null) {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }

            // Start hidden unless we somehow enable mid-pause.
            SetPanelVisible(GameManager.Exists && GameManager.Instance.IsPaused);
            ShowCountdown(false);
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
                resumeButton.onClick.RemoveListener(BeginResume);
            }

            if (mainMenuButton != null) {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }

            _resuming = null;
        }

        private void OnPausePressed(InputAction.CallbackContext context) {
            if (!GameManager.Exists) {
                return;
            }

            if (GameManager.Instance.IsPlaying) {
                GameManager.Instance.Pause();
            } else if (GameManager.Instance.IsPaused) {
                BeginResume();
            }
        }

        private void HandleStateEntered(string stateName) {
            if (stateName == GameStateNames.Paused) {
                // Fresh pause: drop any stale countdown, show the menu, hide the count.
                _resuming = null;
                ShowCountdown(false);
                SetPanelVisible(true);
            }
        }

        private void HandleStateExited(string stateName) {
            if (stateName == GameStateNames.Paused) {
                SetPanelVisible(false);
            }
        }

        /// <summary>Hide the menu, play the 3-2-1 count, then un-freeze gameplay.</summary>
        private void BeginResume() {
            if (!GameManager.Exists || !GameManager.Instance.IsPaused || _resuming != null) {
                return;
            }

            if (countdownSeconds <= 0f) {
                GameManager.Instance.Resume();
                return;
            }

            _resuming = StartCoroutine(ResumeCountdown());
        }

        private IEnumerator ResumeCountdown() {
            SetPanelVisible(false);
            ShowCountdown(true);

            int n = Mathf.CeilToInt(countdownSeconds);
            while (n > 0) {
                if (countdownText != null) {
                    countdownText.text = n.ToString();
                }

                // Realtime so the count stays honest even if a timeScale freeze is added later.
                yield return new WaitForSecondsRealtime(1f);
                n--;
            }

            ShowCountdown(false);
            _resuming = null;
            GameManager.Instance.Resume();
        }

        private void OnMainMenuClicked() {
            GameManager.Instance.ReturnToMenu();
        }

        private void SetPanelVisible(bool visible) {
            if (pausePanel != null) {
                pausePanel.SetActive(visible);
            }
        }

        private void ShowCountdown(bool visible) {
            if (countdownRoot != null) {
                countdownRoot.SetActive(visible);
            }
        }
    }
}
