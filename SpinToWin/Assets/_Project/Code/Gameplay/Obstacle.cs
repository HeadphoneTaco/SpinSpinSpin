using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     A hazard on the belt — a lint trap, a rogue detergent pod, whatever the art team cooks up.
    ///     Touching it costs the gremlin one hit; the run only ends once the <see cref="RunDirector" />
    ///     runs out of hits, which routes through the shared GameOver transition. Either way the
    ///     obstacle recycles itself so it doesn't linger on the belt. The "ow" — sound + screen shake —
    ///     is handled centrally by <see cref="HitFeedback" /> (it watches the hit count), so every
    ///     hazard feels the same and there's one place to tune the juice. Put this on the obstacle
    ///     prefab with a trigger collider + kinematic Rigidbody.
    /// </summary>
    public class Obstacle : ScrollingItem {
        protected override void OnHitPlayer(GremlinRunner runner) {
            // Grace window already spent on a recent hit? Then this touch is free — don't punish it,
            // just clear the obstacle off the belt.
            RunDirector run = RunDirector.Instance;
            if (run != null && run.Invincible) {
                Despawn();
                return;
            }

            if (run != null) {
                run.Crash();
            }

            Despawn();
        }
    }
}
