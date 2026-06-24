using System.Collections;
using _Project.Code.Core;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     The "getting hit feels bad" layer: a camera shake and a hit sound whenever the gremlin
    ///     loses a heart. It simply watches <see cref="RunDirector.HitsRemaining" /> each frame and
    ///     fires the moment it drops — the same dead-simple polling the HUD uses, so there's no event
    ///     to wire. Because it keys off the hit count (not the obstacle), every current and future
    ///     hazard gets the same juice for free, and the run's grace window naturally collapses a
    ///     cluster of hits into one shake.
    ///
    ///     Lives on the persistent <c>[Managers]</c> object — the camera stays a dumb camera. Since
    ///     it can't hold a cross-scene reference to the gameplay camera, it resolves
    ///     <see cref="Camera.main" /> when a shake fires (override with <see cref="shakeTarget" /> if
    ///     you ever want to shake something specific). The sound plays through the shared
    ///     <see cref="AudioManager" /> so it respects the SFX volume setting.
    /// </summary>
    public class HitFeedback : MonoBehaviour {
        [Header("Shake")]
        [Tooltip("Optional. What moves when shaking. Leave empty to shake the current Main Camera.")]
        [SerializeField] private Transform shakeTarget;
        [Tooltip("How long one shake lasts, in seconds.")]
        [SerializeField] private float shakeDuration = 0.25f;
        [Tooltip("How far the shake throws the target (world units) at full strength.")]
        [SerializeField] private float shakeMagnitude = 0.25f;

        [Header("Audio")]
        [Tooltip("Played once per hit. Leave empty for shake-only feedback.")]
        [SerializeField] private AudioClip hitSfx;

        private int _lastHits = -1;
        private Coroutine _shaking;
        private Transform _activeShake;
        private Vector3 _restLocalPos;

        private void Update() {
            RunDirector run = RunDirector.Instance;
            if (run == null) {
                // No active run (e.g. between scenes): forget the count so re-entering doesn't
                // read the change as a "hit".
                _lastHits = -1;
                return;
            }

            if (_lastHits < 0) {
                _lastHits = run.HitsRemaining; // first sighting of this run — sync, don't fire
                return;
            }

            if (run.HitsRemaining < _lastHits) {
                PlayHitFeedback();
            }

            _lastHits = run.HitsRemaining;
        }

        private void PlayHitFeedback() {
            if (hitSfx != null && GameManager.Exists) {
                GameManager.Instance.Audio.PlaySfx(hitSfx);
            }

            // The managers object isn't on screen, so resolve the camera to shake now (override wins).
            Transform target = shakeTarget != null ? shakeTarget : ResolveCamera();
            if (target == null || shakeDuration <= 0f || shakeMagnitude <= 0f) {
                return;
            }

            // Restart cleanly so back-to-back hits don't stack offsets or strand the camera.
            if (_shaking != null && _activeShake != null) {
                _activeShake.localPosition = _restLocalPos;
                StopCoroutine(_shaking);
            }

            _activeShake = target;
            _shaking = StartCoroutine(Shake(target));
        }

        private static Transform ResolveCamera() {
            Camera cam = Camera.main; // the camera tagged "MainCamera" in the active scene
            return cam != null ? cam.transform : null;
        }

        private IEnumerator Shake(Transform target) {
            _restLocalPos = target.localPosition;
            float elapsed = 0f;

            while (elapsed < shakeDuration) {
                // Ease the strength down so the shake settles instead of cutting off.
                float strength = shakeMagnitude * (1f - elapsed / shakeDuration);
                Vector2 offset = Random.insideUnitCircle * strength;
                target.localPosition = _restLocalPos + new Vector3(offset.x, offset.y, 0f);

                // Unscaled so the shake still animates even if a timeScale freeze is added later.
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            target.localPosition = _restLocalPos;
            _shaking = null;
            _activeShake = null;
        }
    }
}
