using System.Collections;
using UnityEngine;

namespace _Project.Code.UI.Transitions {
    /// <summary>
    ///     The <em>visual</em> half of a screen transition: how the screen gets covered and then
    ///     revealed. <see cref="ScreenTransition" /> owns the timing and the swap; this owns the
    ///     looks. Swapping the bubble animation for a fade (or anything else) is just assigning a
    ///     different subclass — no other code changes (Open/Closed + Dependency Inversion).
    /// </summary>
    public abstract class TransitionEffect : MonoBehaviour {
        /// <summary>Bring the overlay fully on, hiding the screen. Yields until fully covered.</summary>
        public abstract IEnumerator Cover();

        /// <summary>Clear the overlay, revealing the screen. Yields until fully clear.</summary>
        public abstract IEnumerator Reveal();

        /// <summary>
        ///     Stretches this object's RectTransform to fill its parent so the overlay always
        ///     covers the whole screen — no matter what size the Image was created at. Subclasses
        ///     call this from Awake (and Reset) so a half-set-up overlay can't leak the swap.
        /// </summary>
        protected void StretchToFill() {
            if (transform is RectTransform rect) {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }
    }
}
