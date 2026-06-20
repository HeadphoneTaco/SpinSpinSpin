using System.Collections;
using UnityEngine;

namespace _Project.Code.UI {
    /// <summary>
    ///     Full-screen fade to/from black via a <see cref="CanvasGroup" />.
    ///     Self-persisting: it detaches to the root and survives scene loads (so it can cover a
    ///     load), and a duplicate in a later scene destroys itself. That means you can author it
    ///     under the menu scene's [UI] for tidiness — at runtime it pops to the root and lives on.
    ///     Reached by <see cref="GameTransitions" /> via <see cref="Instance" />.
    ///     Uses unscaled time so it still runs while paused (Time.timeScale = 0).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour {
        public static ScreenFader Instance { get; private set; }

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float defaultDuration = 0.4f;

        private void Reset() {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake() {
            // Only one fader survives across scenes.
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (canvasGroup == null) {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            // DontDestroyOnLoad needs a root object; detach if authored under [UI].
            if (transform.parent != null) {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);

            // Start clear and click-through.
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnDestroy() {
            if (Instance == this) {
                Instance = null;
            }
        }

        public IEnumerator FadeOut(float duration = -1f) {
            return Fade(1f, duration);
        }

        public IEnumerator FadeIn(float duration = -1f) {
            return Fade(0f, duration);
        }

        private IEnumerator Fade(float targetAlpha, float duration) {
            if (duration < 0f) {
                duration = defaultDuration;
            }

            float startAlpha = canvasGroup.alpha;
            // Block input while the screen is (partly) covered.
            canvasGroup.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < duration) {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            canvasGroup.blocksRaycasts = targetAlpha > 0.001f;
        }
    }
}
