using System;
using System.Collections;
using UnityEngine;

namespace _Project.Code.UI.Transitions {
    /// <summary>
    ///     The persistent full-screen overlay that plays a screen transition. It owns the
    ///     <em>timing</em> and the moment-of-swap; the <em>look</em> lives in a
    ///     <see cref="TransitionEffect" /> (fade now, Mina's bubbles later). Replaces the old
    ///     ScreenFader: same self-persisting singleton shape (detaches to the root, survives scene
    ///     loads, duplicates self-destruct), so author it under a scene's [UI] for tidiness.
    ///
    ///     Two ways to drive it:
    ///     <list type="bullet">
    ///         <item><see cref="Play" /> — cover, run a swap callback while hidden, reveal. For
    ///         in-scene screen switches (e.g. opening Settings).</item>
    ///         <item><see cref="Cover" /> / <see cref="Reveal" /> — the two halves on their own, for
    ///         a scene load that has to happen between them (see GameTransitions).</item>
    ///     </list>
    /// </summary>
    public class ScreenTransition : MonoBehaviour {
        public static ScreenTransition Instance { get; private set; }

        [Tooltip("The visual effect. Leave a CanvasGroupFadeEffect here until the bubble animation lands.")]
        [SerializeField] private TransitionEffect effect;

        private bool _playing;

        /// <summary>True while a transition is mid-play (input should already be blocked by the effect).</summary>
        public bool IsPlaying => _playing;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (effect == null) {
                effect = GetComponentInChildren<TransitionEffect>(true);
            }

            // DontDestroyOnLoad needs a root object; detach if authored under [UI].
            if (transform.parent != null) {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy() {
            if (Instance == this) {
                Instance = null;
            }
        }

        /// <summary>Cover the screen, run <paramref name="onCovered" /> while hidden, then reveal.</summary>
        public void Play(Action onCovered) {
            // If a transition is already running, just do the swap so the action isn't lost.
            if (_playing || effect == null) {
                onCovered?.Invoke();
                return;
            }

            StartCoroutine(PlayRoutine(onCovered));
        }

        /// <summary>Bring the overlay fully on. Yields until covered.</summary>
        public IEnumerator Cover() {
            if (effect != null) {
                yield return effect.Cover();
            }
        }

        /// <summary>Clear the overlay. Yields until revealed.</summary>
        public IEnumerator Reveal() {
            if (effect != null) {
                yield return effect.Reveal();
            }
        }

        private IEnumerator PlayRoutine(Action onCovered) {
            _playing = true;
            yield return effect.Cover();
            onCovered?.Invoke();
            yield return effect.Reveal();
            _playing = false;
        }
    }
}
