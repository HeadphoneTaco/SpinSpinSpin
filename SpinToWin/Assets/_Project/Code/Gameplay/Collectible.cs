using _Project.Code.Core;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     A sock on the belt. When the gremlin grabs it, it banks its <see cref="value" /> with the
    ///     <see cref="RunDirector" />, plays a pickup blip, and recycles itself. Put this on the sock
    ///     prefab with a trigger collider + kinematic Rigidbody (see <see cref="ScrollingItem" />).
    /// </summary>
    public class Collectible : ScrollingItem {

        [SerializeField] private int value = 1;
        [SerializeField] private AudioClip collectSfx;

        protected override void OnHitPlayer(GremlinRunner runner) {
            if (RunDirector.Instance != null) {
                RunDirector.Instance.CollectSock(value);
            }

            if (collectSfx != null && GameManager.Exists) {
                GameManager.Instance.Audio.PlaySfx(collectSfx);
            }

            Despawn();
        }
    }
}
