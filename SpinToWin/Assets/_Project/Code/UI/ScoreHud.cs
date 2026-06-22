using _Project.Code.Gameplay;
using TMPro;
using UnityEngine;

namespace _Project.Code.UI {
    /// <summary>
    ///     The in-run HUD. Reads <see cref="RunDirector.Instance" /> each frame and shows the sock
    ///     count, the timer (only in SurviveTime mode), and a win/lose banner when the run ends.
    ///     Polling keeps it dead simple - no event wiring needed. Put it on a HUD Canvas in <c>Main</c>
    ///     and assign the three TMP texts. The timer text hides itself in CollectSocks mode.
    /// </summary>
    public class ScoreHud : MonoBehaviour {
        [Tooltip("Shows the sock count (and the target in CollectSocks mode).")]
        [SerializeField] private TMP_Text socksText;
        [Tooltip("Shows the remaining time. Auto-hidden in CollectSocks mode (its timer is secret).")]
        [SerializeField] private TMP_Text timeText;
        [Tooltip("Win/lose banner. Blank while the run is in progress.")]
        [SerializeField] private TMP_Text outcomeText;

        private void Update() {
            RunDirector run = RunDirector.Instance;
            if (run == null) {
                return;
            }

            if (socksText != null) {
                socksText.text = run.Mode == WinMode.CollectSocks
                    ? $"Socks {run.SockCount}/{run.TargetSocks}"
                    : $"Socks {run.SockCount}";
            }

            if (timeText != null) {
                timeText.gameObject.SetActive(run.TimerVisible);
                if (run.TimerVisible) {
                    float remaining = Mathf.Max(0f, run.TargetTime - run.ElapsedTime);
                    timeText.text = FormatTime(remaining);
                }
            }

            if (outcomeText != null) {
                switch (run.Outcome) {
                    case RunOutcome.Won:
                        outcomeText.text = "YOU WIN!";
                        break;
                    case RunOutcome.Lost:
                        outcomeText.text = "CRASHED!";
                        break;
                    default:
                        outcomeText.text = string.Empty;
                        break;
                }
            }
        }

        private static string FormatTime(float seconds) {
            int total = Mathf.CeilToInt(seconds);
            return $"{total / 60:0}:{total % 60:00}";
        }
    }
}
