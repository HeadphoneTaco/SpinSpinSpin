using System.Collections;
using UnityEngine;

namespace _Project.Code.UI.Transitions {
    /// <summary>
    ///     Plays Mina's bubble transition through an <see cref="Animator" />. Drop her animation
    ///     into two states reached by the <c>Cover</c> and <c>Reveal</c> triggers; this fires the
    ///     trigger and waits <see cref="coverDuration" /> / <see cref="revealDuration" /> (set them
    ///     to match the clip lengths — e.g. 3–5s for the bubble sweep). The <see cref="CanvasGroup" />
    ///     blocks clicks while bubbles are on screen. Uses unscaled time so it runs while paused.
    ///
    ///     Different asset format later (a particle prefab, a shader wipe)? Write another
    ///     <see cref="TransitionEffect" /> subclass — nothing else changes.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class AnimatorTransitionEffect : TransitionEffect {
        [SerializeField] private Animator animator;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Trigger names on the Animator")]
        [SerializeField] private string coverTrigger = "Cover";
        [SerializeField] private string revealTrigger = "Reveal";

        [Header("Clip lengths (seconds) — match Mina's animation")]
        [SerializeField] private float coverDuration = 1.5f;
        [SerializeField] private float revealDuration = 1.5f;

        private void Reset() {
            animator = GetComponent<Animator>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake() {
            if (canvasGroup == null) {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = false;
        }

        public override IEnumerator Cover() {
            canvasGroup.blocksRaycasts = true;
            if (animator != null) {
                animator.SetTrigger(coverTrigger);
            }

            yield return new WaitForSecondsRealtime(coverDuration);
        }

        public override IEnumerator Reveal() {
            if (animator != null) {
                animator.SetTrigger(revealTrigger);
            }

            yield return new WaitForSecondsRealtime(revealDuration);
            canvasGroup.blocksRaycasts = false;
        }
    }
}
