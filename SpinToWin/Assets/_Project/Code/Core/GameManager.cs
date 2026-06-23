using CoreUtils;
using UnityEngine;

namespace _Project.Code.Core {
    /// <summary>
    ///     Single front door for SpinToWin's core systems. Plain MonoBehaviour singleton
    ///     (see <see cref="Singleton{T}" />) that drives a CoreUtils <see cref="StateMachine" />.
    ///     Place this on the persistent managers root object in the boot scene, with the
    ///     StateMachine as a child (states: MainMenu / Playing / Paused / GameOver).
    ///     Transition helpers below wrap <see cref="StateMachine.ChangeState(string)" />.
    /// </summary>
    public class GameManager : Singleton<GameManager> {
        [SerializeField] private StateMachine stateMachine;

        private AudioManager _audio;
        private AccessibilityManager _accessibility;

        // The state we were in when settings opened, so closing the overlay returns there
        // (MainMenu from the title screen, Paused from the pause menu) instead of always MainMenu.
        private string _stateBeforeSettings;

        /// <summary>The CoreUtils state machine driving the game.</summary>
        public StateMachine StateMachine => stateMachine;

        /// <summary>Handles music and sound-effect playback. Created on first access.</summary>
        public AudioManager Audio => GetOrAdd(ref _audio);

        /// <summary>
        ///     Accessibility preferences (high-contrast, etc.). Created on first access; place
        ///     it on this object and wire its event to enable the broadcast signal.
        /// </summary>
        public AccessibilityManager Accessibility => GetOrAdd(ref _accessibility);

        /// <summary>Name of the currently active state GameObject, or null if none.</summary>
        public string CurrentStateName => stateMachine != null && stateMachine.CurrentState != null
            ? stateMachine.CurrentState.name
            : null;

        public bool IsState(string stateName) => CurrentStateName == stateName;
        public bool IsPlaying => IsState(GameStateNames.Playing);
        public bool IsPaused => IsState(GameStateNames.Paused);

        protected override void Awake() {
            base.Awake();

            // A duplicate destroys itself in base.Awake; only the survivor sets up.
            if (Instance != this) {
                return;
            }

            if (stateMachine == null) {
                stateMachine = GetComponentInChildren<StateMachine>(true);
            }

            if (stateMachine == null) {
                Debug.LogError("[GameManager] No StateMachine found. Add one as a child with " +
                               "MainMenu/Playing/Paused/GameOver state objects.", this);
            }
        }

        // --- Transition helpers (lightly guarded) --------------------------------

        public void StartGame() {
            ChangeState(GameStateNames.Playing);
        }

        /// <summary>
        ///     Opens the settings overlay. Settings is still a state (gameplay systems gate on
        ///     IsPlaying, so the game stays frozen), but it no longer loads its own scene — the
        ///     overlay shows on top of whatever is active. Close with <see cref="CloseSettings" />.
        /// </summary>
        public void OpenSettings() {
            if (IsState(GameStateNames.Settings)) {
                return;
            }

            _stateBeforeSettings = CurrentStateName;
            ChangeState(GameStateNames.Settings);
        }

        /// <summary>
        ///     Closes the settings overlay, returning to the state it was opened from (Paused or
        ///     MainMenu). Falls back to MainMenu if that state is unknown.
        /// </summary>
        public void CloseSettings() {
            if (!IsState(GameStateNames.Settings)) {
                return;
            }

            string back = string.IsNullOrEmpty(_stateBeforeSettings)
                ? GameStateNames.MainMenu
                : _stateBeforeSettings;
            _stateBeforeSettings = null;
            ChangeState(back);
        }

        public void Pause() {
            if (IsPlaying) {
                ChangeState(GameStateNames.Paused);
            }
        }

        public void Resume() {
            if (IsPaused) {
                ChangeState(GameStateNames.Playing);
            }
        }

        public void TogglePause() {
            if (IsPlaying) {
                Pause();
            } else if (IsPaused) {
                Resume();
            }
        }

        public void EndGame() {
            ChangeState(GameStateNames.GameOver);
        }

        public void ReturnToMenu() {
            ChangeState(GameStateNames.MainMenu);
        }

        private void ChangeState(string stateName) {
            if (stateMachine == null) {
                Debug.LogError($"[GameManager] Can't change to '{stateName}' — no StateMachine.", this);
                return;
            }

            stateMachine.ChangeState(stateName);
        }

        /// <summary>Returns the cached sub-manager, fetching or adding the component on demand.</summary>
        private TManager GetOrAdd<TManager>(ref TManager field) where TManager : Component {
            if (field == null) {
                field = GetComponent<TManager>();
                if (field == null) {
                    field = gameObject.AddComponent<TManager>();
                }
            }

            return field;
        }
    }
}
