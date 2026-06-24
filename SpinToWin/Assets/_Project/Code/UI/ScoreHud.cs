using System.Collections.Generic;
using System.Text;
using _Project.Code.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Code.UI {
    /// <summary>
    ///     The in-run HUD. Reads <see cref="RunDirector.Instance" /> each frame and shows the sock
    ///     count, the timer (only in SurviveTime mode), a win/lose banner, the lives (hearts), and
    ///     the sock fill bar. Polling keeps it dead simple - no event wiring needed. Put it on a HUD
    ///     Canvas in <c>Main</c> and assign the references below.
    /// </summary>
    public class ScoreHud : MonoBehaviour {
        [Header("Text")]
        [Tooltip("Shows the sock count (and the target in CollectSocks mode).")]
        [SerializeField] private TMP_Text socksText;
        [Tooltip("Shows the remaining time. Auto-hidden in CollectSocks mode (its timer is secret).")]
        [SerializeField] private TMP_Text timeText;
        [Tooltip("Win/lose banner. Blank while the run is in progress.")]
        [SerializeField] private TMP_Text outcomeText;

        [Header("Sock fill - stripes (preferred)")]
        [Tooltip("The fill slots inside the sock, ordered BOTTOM to TOP - one per sock needed to win. " +
                 "Each collected sock lights the next slot, tinted with that sock's colour, so the bar " +
                 "fills in collection order. Use plain white Image stripes for now; per-section strip " +
                 "art can drop straight onto these later. When this list has entries it is used " +
                 "instead of the single continuous Sock Fill image below.")]
        [SerializeField] private Image[] sockSegments;
        [Tooltip("ON: empty slots are hidden so the bare sock outline shows. OFF: empty slots stay " +
                 "visible but faded to Empty Segment Fade.")]
        [SerializeField] private bool hideEmptySegments = true;
        [Tooltip("Opacity of an empty slot when not hidden.")]
        [Range(0f, 1f)] [SerializeField] private float emptySegmentFade = 0.12f;

        [Header("Sock fill - continuous (fallback)")]
        [Tooltip("Single fill Image used only when no Sock Segments are assigned. Set Image Type=Filled, " +
                 "Fill Method=Vertical, Fill Origin=Bottom. Fills bottom-up to SockCount / TargetSocks.")]
        [SerializeField] private Image sockFill;
        [Tooltip("Optional sprite stamped onto the continuous fill Image on start.")]
        [SerializeField] private Sprite sockFillSprite;
        [Tooltip("How quickly the continuous fill eases toward the new level. Higher = snappier; " +
                 "~8 feels juicy. 0 = snap instantly.")]
        [SerializeField] private float fillLerpSpeed = 8f;

        [Header("Lives (heart sprites)")]
        [Tooltip("The heart Images, left to right (one per life). When assigned, these are used " +
                 "instead of the legacy text hearts. Lost hearts fade or hide per Dim Lost Hearts.")]
        [SerializeField] private Image[] heartImages;
        [Tooltip("Optional. The heart sprite (HeartSprite) applied to every Heart Image on start. " +
                 "Leave empty to keep whatever sprite is already set on each Image.")]
        [SerializeField] private Sprite heartSprite;
        [Tooltip("ON: a lost heart stays in place but fades to Lost Heart Fade. OFF: it is hidden.")]
        [SerializeField] private bool dimLostHearts = true;
        [Tooltip("Opacity of a lost heart (0 = invisible, 1 = looks the same as a full heart). " +
                 "Used by the sprite hearts (when dimming) and the legacy text hearts.")]
        [Range(0f, 1f)] [SerializeField] private float lostHeartFade = 0.28f;

        [Header("Lives (legacy text fallback)")]
        [Tooltip("Shows remaining hits as text hearts. Only used when no heart sprites are assigned.")]
        [SerializeField] private TMP_Text livesText;
        [Tooltip("Glyph (or TMP <sprite> tag) used for every text heart. Lost hearts reuse this glyph, dimmed.")]
        [SerializeField] private string fullHeart = "♥"; // heart
        [Tooltip("Inserted between text hearts.")]
        [SerializeField] private string heartSeparator = " ";

        private void OnEnable() {
            // Lost text hearts are dimmed with a rich-text <alpha> tag, so rich text must be on.
            if (livesText != null) {
                livesText.richText = true;
            }

            ApplySprites();
        }

        /// <summary>
        ///     Pushes the assigned sprites onto their Images, so the art can be wired from one field
        ///     each instead of set on every Image by hand. Null sprites are skipped, leaving whatever
        ///     the Image already shows.
        /// </summary>
        private void ApplySprites() {
            if (sockFill != null && sockFillSprite != null) {
                sockFill.sprite = sockFillSprite;
            }

            if (heartSprite != null && heartImages != null) {
                foreach (Image heart in heartImages) {
                    if (heart != null) {
                        heart.sprite = heartSprite;
                    }
                }
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

            UpdateSockBar(run);
            UpdateHearts(run);

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

        /// <summary>
        ///     Drives the sock fill. Preferred path: a stack of stripe slots, bottom to top, each lit
        ///     and tinted by the matching collected sock so the bar fills in collection order. Falls
        ///     back to a single continuous Filled Image (eased to SockCount / TargetSocks) when no
        ///     slots are assigned.
        /// </summary>
        private void UpdateSockBar(RunDirector run) {
            if (sockSegments != null && sockSegments.Length > 0) {
                IReadOnlyList<Color> colors = run.CollectedSockColors;
                int filled = colors != null ? colors.Count : 0;

                for (int i = 0; i < sockSegments.Length; i++) {
                    Image seg = sockSegments[i];
                    if (seg == null) {
                        continue;
                    }

                    if (i < filled) {
                        seg.enabled = true;
                        seg.color = colors[i];
                    } else if (hideEmptySegments) {
                        seg.enabled = false;
                    } else {
                        seg.enabled = true;
                        Color c = seg.color;
                        c.a = Mathf.Clamp01(emptySegmentFade);
                        seg.color = c;
                    }
                }

                return;
            }

            if (sockFill == null) {
                return;
            }

            int target = run.TargetSocks;
            float level = target > 0
                ? Mathf.Clamp01((float)run.SockCount / target)
                : (run.SockCount > 0 ? 1f : 0f);

            sockFill.fillAmount = fillLerpSpeed > 0f
                ? Mathf.Lerp(sockFill.fillAmount, level, 1f - Mathf.Exp(-fillLerpSpeed * Time.deltaTime))
                : level;
        }

        /// <summary>
        ///     Updates the lives display. Prefers the heart sprites when assigned (fading or hiding
        ///     each lost heart), and falls back to the legacy text hearts otherwise.
        /// </summary>
        private void UpdateHearts(RunDirector run) {
            if (heartImages != null && heartImages.Length > 0) {
                for (int i = 0; i < heartImages.Length; i++) {
                    Image heart = heartImages[i];
                    if (heart == null) {
                        continue;
                    }

                    bool alive = i < run.HitsRemaining;
                    if (dimLostHearts) {
                        heart.enabled = true;
                        Color c = heart.color;
                        c.a = alive ? 1f : Mathf.Clamp01(lostHeartFade);
                        heart.color = c;
                    } else {
                        heart.enabled = alive;
                    }
                }

                return;
            }

            if (livesText != null) {
                livesText.text = BuildHearts(run.HitsRemaining, run.MaxHits);
            }
        }

        private static string FormatTime(float seconds) {
            int total = Mathf.CeilToInt(seconds);
            return $"{total / 60:0}:{total % 60:00}";
        }

        /// <summary>
        ///     Builds the text-hearts string, e.g. three full then two dimmed. Every heart uses the
        ///     SAME glyph - lost ones are just faded with an &lt;alpha&gt; tag. Using one glyph means
        ///     we never ask TMP for a character that might be missing from the font atlas (the hollow
        ///     outline usually is, which is what threw the warning). Only used when no heart sprites
        ///     are assigned.
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
