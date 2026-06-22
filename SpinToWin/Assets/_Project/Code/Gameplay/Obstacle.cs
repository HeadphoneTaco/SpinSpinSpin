using _Project.Code.Core;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     A hazard on the belt — a lint trap, a rogue detergent pod, whatever the art team cooks up.
    ///     The moment the gremlin touches it the run is over: it plays a crash sound and asks the
    ///     <see cref="RunDirector" /> to end the game (which routes through the shared GameOver
    ///     transition). Put this on the obstacle prefab with a trigger collider + kinematic Rigidbody.
    /// </summary>
    public class Obstacle : ScrollingItem {
        [SerializeField] private AudioClip crashSfx;

        protected override void OnHitPlayer(GremlinRunner runner) {
            if (crashSfx != null && GameManager.Exists) {
                GameManager.Instance.Audio.PlaySfx(crashSfx);
            }

            if (RunDirector.Instance != null) {
                RunDirector.Instance.Crash();
            }
            // The obstacle stays put — the run is ending, no need to recycle it.
        }
    }
}
