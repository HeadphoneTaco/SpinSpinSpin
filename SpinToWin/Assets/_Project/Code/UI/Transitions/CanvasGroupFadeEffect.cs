using System.Collections;
using UnityEngine;

namespace _Project.Code.UI.Transitions {
    /// <summary>
    ///     Placeholder transition: a plain fade to/from black via a <see cref="CanvasGroup" />.
    ///     Works today so screen switches function before Mina's bubble animation arrives — then
    ///     swap this component out for <see cref="AnimatorTransitionEffect" /> on the same object.
    ///     Uses unscaled time so it still runs while the game is paused (Time.timeScale = 0).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class CanvasGroupFadeEffect : TransitionEffect {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float duration = 0.4f;

        private void Reset() {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake() {
            if (canvasGroup == null) {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        public override IEnumerator Cover() => FadeTo(1f);

        public override IEnumerator Reveal() => FadeTo(0f);

        private IEnumerator FadeTo(float targetAlpha) {
            // Block clicks while the screen is (partly) covered.
            canvasGroup.blocksRaycasts = true;

            float startAlpha = canvasGroup.alpha;
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
