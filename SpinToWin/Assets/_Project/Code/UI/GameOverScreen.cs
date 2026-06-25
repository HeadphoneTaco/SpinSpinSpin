using _Project.Code.Core;
using _Project.Code.Gameplay;
using CoreUtils.GameEvents;
using TMPro;
using UnityEngine;

namespace _Project.Code.UI {
    /// <summary>
    ///     Shows the end-of-run screen while the game is in the GameOver state and hides it otherwise.
    ///     Built like <see cref="SettingsOverlay" /> for consistency: this component lives on the
    ///     always-active GameOverOverlay root and toggles child panels, rather than switching itself
    ///     off. The difference is there are TWO panels - a Win panel and a Lose panel - and it picks
    ///     which to show from <see cref="RunDirector.Outcome" />, then fills that panel's run stats.
    ///
    ///     The Play Again / Quit buttons on each panel are plain <see cref="GameStateButton" />s
    ///     (Action.Play re-enters Playing and reloads Main; Action.MainMenu bails out), so no button
    ///     wiring lives here. Place this on the GameOverOverlay prefab/root and assign the two panels,
    ///     their optional stat labels, and the shared StateEntered / StateExited events.
    /// </summary>
    public class GameOverScreen : MonoBehaviour {
        [Header("Win panel")]
        [Tooltip("Shown when the run was WON. A child of this object - never this root itself.")]
        [SerializeField] private GameObject winPanel;
        [Tooltip("Final sock count on the win panel. In CollectSocks mode it also shows the target.")]
        [SerializeField] private TMP_Text winSocksText;
        [Tooltip("Distance travelled, on the win panel (metres).")]
        [SerializeField] private TMP_Text winDistanceText;
        [Tooltip("Time survived, on the win panel (m:ss).")]
        [SerializeField] private TMP_Text winTimeText;

        [Header("Lose panel")]
        [Tooltip("Shown when the run was LOST. A child of this object - never this root itself.")]
        [SerializeField] private GameObject losePanel;
        [Tooltip("Final sock count on the lose panel. In CollectSocks mode it also shows the target.")]
        [SerializeField] private TMP_Text loseSocksText;
        [Tooltip("Distance travelled, on the lose panel (metres).")]
        [SerializeField] private TMP_Text loseDistanceText;
        [Tooltip("Time survived, on the lose panel (m:ss).")]
        [SerializeField] private TMP_Text loseTimeText;

        [Header("Events")]
        [SerializeField] private GameEventString stateEntered;
        [SerializeField] private GameEventString stateExited;

        private void OnEnable() {
            // Footgun guard (same as SettingsOverlay): the panels must be SEPARATE objects from the one
            // this component lives on. OnEnable only runs while this object is active, so if we ever hid
            // ourselves we'd stop hearing the next "GameOver entered" and the screen would never return.
            if (winPanel == gameObject || losePanel == gameObject) {
                Debug.LogError("[GameOverScreen] A panel is set to this same object. Point Win/Lose " +
                               "Panel at child objects instead, or the overlay disables itself and " +
                               "never reopens.", this);
            }

            if (stateEntered != null) {
                stateEntered.Event += HandleStateEntered;
            }

            if (stateExited != null) {
                stateExited.Event += HandleStateExited;
            }

            // Match whatever the current state already is (e.g. if enabled mid-GameOver).
            if (GameManager.Exists && GameManager.Instance.IsState(GameStateNames.GameOver)) {
                Show();
            } else {
                HideBoth();
            }
        }

        private void OnDisable() {
            if (stateEntered != null) {
                stateEntered.Event -= HandleStateEntered;
            }

            if (stateExited != null) {
                stateExited.Event -= HandleStateExited;
            }
        }

        private void HandleStateEntered(string stateName) {
            if (stateName == GameStateNames.GameOver) {
                Show();
            }
        }

        private void HandleStateExited(string stateName) {
            if (stateName == GameStateNames.GameOver) {
                HideBoth();
            }
        }

        /// <summary>Reveals the panel that matches the outcome and fills it from the run that ended.</summary>
        private void Show() {
            RunDirector run = RunDirector.Instance;
            bool won = run != null && run.Outcome == RunOutcome.Won;

            if (winPanel != null) {
                winPanel.SetActive(won);
            }

            if (losePanel != null) {
                losePanel.SetActive(!won);
            }

            if (run == null) {
                return;
            }

            if (won) {
                FillStats(run, winSocksText, winDistanceText, winTimeText);
            } else {
                FillStats(run, loseSocksText, loseDistanceText, loseTimeText);
            }
        }

        private void HideBoth() {
            if (winPanel != null) {
                winPanel.SetActive(false);
            }

            if (losePanel != null) {
                losePanel.SetActive(false);
            }
        }

        private static void FillStats(RunDirector run, TMP_Text socks, TMP_Text distance, TMP_Text time) {
            if (socks != null) {
                socks.text = run.Mode == WinMode.CollectSocks
                    ? $"Socks {run.SockCount}/{run.TargetSocks}"
                    : $"Socks {run.SockCount}";
            }

            if (distance != null) {
                distance.text = $"{run.Distance} m";
            }

            if (time != null) {
                time.text = FormatTime(run.ElapsedTime);
            }
        }

        private static string FormatTime(float seconds) {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{total / 60:0}:{total % 60:00}";
        }
    }
}
