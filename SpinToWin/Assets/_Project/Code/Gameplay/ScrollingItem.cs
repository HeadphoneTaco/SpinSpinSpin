using _Project.Code.Core;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     A pool that can reclaim a spent <see cref="ScrollingItem" />. Implemented by
    ///     <see cref="TrackSpawner" /> so items can return themselves once they scroll off-screen
    ///     or get collected, instead of being destroyed every time.
    /// </summary>
    public interface IScrollingItemPool {
        void Release(ScrollingItem item);
    }

    /// <summary>
    ///     Base class for anything that rides the drum toward the gremlin — socks to grab, hazards
    ///     to dodge. Think of it as the conveyor belt plus the chute at the end: this class handles
    ///     the moving and the disappearing; each subclass only says what happens when the gremlin
    ///     reaches it (<see cref="OnHitPlayer" />).
    ///
    ///     Movement uses <see cref="RunDirector.Speed" /> so everything on the belt speeds up together
    ///     as the run heats up. With no director present (e.g. testing in the Playground scene) it
    ///     falls back to <see cref="fallbackSpeed" />. Items freeze whenever the game isn't Playing.
    ///
    ///     Trigger setup: each item needs a trigger <see cref="Collider" /> and a <b>kinematic
    ///     Rigidbody</b> (it's moved by transform, and triggers need a body on one side). The gremlin
    ///     supplies the other collider.
    /// </summary>
    public abstract class ScrollingItem : MonoBehaviour {
        [SerializeField] private float fallbackSpeed = 10f;

        private IScrollingItemPool _pool;
        private float _despawnZ = -10f;

        /// <summary>
        ///     Wires the item to its pool and tells it how far back to ride before recycling.
        ///     Called by the spawner right after positioning, before the item is shown.
        /// </summary>
        public void Configure(IScrollingItemPool pool, float despawnZ) {
            _pool = pool;
            _despawnZ = despawnZ;
        }

        protected virtual void Update() {
            // Hold position unless we're actively playing (lets Pause freeze the whole belt).
            bool playing = !GameManager.Exists || GameManager.Instance.IsPlaying;
            if (!playing) {
                return;
            }

            float speed = RunDirector.Instance != null ? RunDirector.Instance.Speed : fallbackSpeed;
            transform.position += Vector3.back * (speed * Time.deltaTime);

            if (transform.position.z <= _despawnZ) {
                Despawn();
            }
        }

        private void OnTriggerEnter(Collider other) {
            GremlinRunner runner = other.GetComponentInParent<GremlinRunner>();
            if (runner != null) {
                OnHitPlayer(runner);
            }
        }

        /// <summary>What this item does the moment the gremlin overlaps it.</summary>
        protected abstract void OnHitPlayer(GremlinRunner runner);

        /// <summary>Returns the item to its pool, or destroys it if it was spawned poolless.</summary>
        protected void Despawn() {
            if (_pool != null) {
                _pool.Release(this);
            } else {
                Destroy(gameObject);
            }
        }
    }
}
