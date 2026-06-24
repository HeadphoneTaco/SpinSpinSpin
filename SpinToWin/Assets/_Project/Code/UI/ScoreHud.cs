using System.Text;
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

        [Header("Lives")]
        [Tooltip("Shows remaining hits as hearts. Leave empty to skip the lives display.")]
        [SerializeField] private TMP_Text livesText;
        [Tooltip("Glyph (or TMP <sprite> tag) used for every heart. Lost hearts reuse this same glyph, dimmed.")]
        [SerializeField] private string fullHeart = "♥"; // ♥
        [Tooltip("Inserted between hearts.")]
        [SerializeField] private string heartSeparator = " ";
        [Tooltip("Opacity of a lost heart (0 = invisible, 1 = looks the same as a full heart).")]
        [Range(0f, 1f)] [SerializeField] private float lostHeartFade = 0.28f;

        private void OnEnable() {
            // Lost hearts are dimmed with a rich-text <alpha> tag, so rich text must be on.
            if (livesText != null) {
                livesText.richText = true;
            }
        }

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

            if (livesText != null) {
                livesText.text = BuildHearts(run.HitsRemaining, run.MaxHits);
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

        /// <summary>
        ///     Builds the hearts string, e.g. three full then two dimmed. Every heart uses the SAME
        ///     glyph — lost ones are just faded with an &lt;alpha&gt; tag. Using one glyph means we
        ///     never ask TMP for a character that might be missing from the font atlas (the hollow ♡
        ///     outline usually is, which is what threw the warning).
        /// </summary>
        private string BuildHearts(int remaining, int max) {
            if (max <= 0) {
                return string.Empty;
            }

            remaining = Mathf.Clamp(remaining, 0, max);
            string lostAlphaTag = $"<alpha=#{Mathf.RoundToInt(Mathf.Clamp01(lostHeartFade) * 255f):X2}>";
            const string fullAlphaTag = "<alpha=#FF>";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < max; i++) {
                if (i > 0) {
                    sb.Append(heartSeparator);
                }

                sb.Append(i < remaining ? fullAlphaTag : lostAlphaTag).Append(fullHeart);
            }

            sb.Append(fullAlphaTag); // reset so nothing after inherits the dim
            return sb.ToString();
        }
    }
}
