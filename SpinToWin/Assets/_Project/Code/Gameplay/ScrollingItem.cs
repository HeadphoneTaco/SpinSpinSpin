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
    ///     Base class for anything that rides the drum toward the gremlin - socks to grab, hazards
    ///     to dodge. Think of it as the conveyor belt plus the chute at the end: this class handles
    ///     the moving and the disappearing; each subclass only says what happens when the gremlin
    ///     reaches it (<see cref="OnHitPlayer" />).
    ///
    ///     Movement uses <see cref="RunDirector.Speed" /> so everything on the belt speeds up together
    ///     as the run heats up. With no director present (e.g. testing in the Playground scene) it
    ///     falls back to <see cref="fallbackSpeed" />. Items freeze whenever the game isn't Playing.
    ///
    ///     Code / Colliders / Mesh split: this script lives on the `Code` child; the trigger collider
    ///     and kinematic Rigidbody live on a `Colliders` sibling (no script there). Two things make
    ///     that work: it moves <see cref="Body" /> (the prefab root), not just its own transform, so
    ///     the whole prefab rides the belt; and the gremlin itself scans for overlapping items and
    ///     calls <see cref="HitByPlayer" />, so no trigger script is needed on the collider object.
    /// </summary>
    public abstract class ScrollingItem : MonoBehaviour {
        [SerializeField] private float fallbackSpeed = 10f;
        [SerializeField] private Transform body;
        private IScrollingItemPool _pool;
        private float _despawnZ = -10f;
        private bool _consumed;

        /// <summary>The transform that rides the belt - the prefab root, even when this script is on a child.</summary>
        public Transform Body { get; private set; }

        protected virtual void Awake() {
            // Cached at Instantiate, while the clone is still unparented, so root is its own root.
            Body = body != null ? body : transform.root;
        }

        protected virtual void OnEnable() {
            // Fresh off the spawner (or reused from the pool): this item hasn't been grabbed yet.
            _consumed = false;
        }

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
            Body.position += Vector3.back * (speed * Time.deltaTime);

            if (Body.position.z <= _despawnZ) {
                Despawn();
            }
        }

        /// <summary>
        ///     Called by the gremlin when it scans and finds this item overlapping it. Guarded so a
        ///     multi-frame overlap only counts once (the gremlin polls every frame, unlike a one-shot
        ///     OnTriggerEnter).
        /// </summary>
        public void HitByPlayer(GremlinRunner runner) {
            if (_consumed) {
                return;
            }

            _consumed = true;
            OnHitPlayer(runner);
        }

        /// <summary>What this item does the moment the gremlin overlaps it.</summary>
        protected abstract void OnHitPlayer(GremlinRunner runner);

        /// <summary>Returns the whole prefab to its pool, or destroys it if it was spawned poolless.</summary>
        protected void Despawn() {
            if (_pool != null) {
                _pool.Release(this);
            } else {
                Destroy(Body.gameObject);
            }
        }
    }
}
