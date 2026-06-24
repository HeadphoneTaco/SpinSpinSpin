using _Project.Code.Core;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     A sock on the belt. When the gremlin grabs it, it banks its <see cref="value" /> with the
    ///     <see cref="RunDirector" />, hands over its <see cref="fillColor" /> for the next stripe on
    ///     the sock bar, plays a pickup blip, and recycles itself. Put this on the sock prefab with a
    ///     trigger collider + kinematic Rigidbody (see <see cref="ScrollingItem" />).
    /// </summary>
    public class Collectible : ScrollingItem {

        [SerializeField] private int value = 1;
        [Tooltip("The colour this sock paints onto its stripe in the sock bar. Give each sock prefab " +
                 "its own colour; since socks spawn at random, the bar fills with a random colour " +
                 "stack in the order you collect them. (Placeholder until per-section strip art lands.)")]
        [SerializeField] private Color fillColor = Color.white;
        [SerializeField] private AudioClip collectSfx;

        protected override void OnHitPlayer(GremlinRunner runner) {
            if (RunDirector.Instance != null) {
                RunDirector.Instance.CollectSock(value, fillColor);
            }

            if (collectSfx != null && GameManager.Exists) {
                GameManager.Instance.Audio.PlaySfx(collectSfx);
            }

            Despawn();
        }
    }
}
