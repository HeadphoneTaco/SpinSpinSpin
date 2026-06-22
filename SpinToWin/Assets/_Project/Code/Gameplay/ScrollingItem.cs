using _Project.Code.Core;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     A pool that can reclaim a spent <see cref="ScrollingItem" />. Implemented by
    ///     <see cref="TrackSpawner" /> so items can return themselves once they leave the track or get
    ///     collected, instead of being destroyed every time.
    /// </summary>
    public interface IScrollingItemPool {
        void Release(ScrollingItem item);
    }

    /// <summary>
    ///     Base class for anything that rides the drum toward the gremlin - socks to grab, hazards to
    ///     dodge. This class moves the item and recycles it; each subclass only says what happens on
    ///     contact (<see cref="OnHitPlayer" />).
    ///
    ///     When configured with an <see cref="ApproachCurve" />, the item traces a "C" down the drum
    ///     wall: it advances along the arc by angle at <see cref="RunDirector.Speed" /> until it reaches
    ///     the gremlin, then slides straight out the back to despawn. With no director it falls back to
    ///     <see cref="fallbackSpeed" />. Items freeze whenever the game isn't Playing.
    ///
    ///     Code / Colliders / Mesh split: this script lives on the `Code` child; the trigger collider +
    ///     kinematic Rigidbody live on a `Colliders` sibling (no script there). It moves
    ///     <see cref="Body" /> (the prefab root) so the whole prefab rides along, and the gremlin scans
    ///     for overlaps and calls <see cref="HitByPlayer" /> - so no trigger script is needed on the item.
    /// </summary>
    public abstract class ScrollingItem : MonoBehaviour {
        [SerializeField] private float fallbackSpeed = 10f;
        [SerializeField] private Transform body;

        private IScrollingItemPool _pool;
        private float _despawnZ = -10f;
        private bool _consumed;
        private ApproachCurve _curve;
        private bool _hasCurve;
        private float _phi; // current angle along the C (StartAngle -> 0)

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
        ///     Wires the item to its pool, its recycle distance, and the C-arc it rides. Resets the item
        ///     to the start of the arc. Called by the spawner right after positioning, before it shows.
        /// </summary>
        public void Configure(IScrollingItemPool pool, float despawnZ, ApproachCurve curve) {
            _pool = pool;
            _despawnZ = despawnZ;
            _curve = curve;
            _hasCurve = true;
            _phi = curve.StartAngle;
        }

        protected virtual void Update() {
            // Hold position unless we're actively playing (lets Pause freeze the whole belt).
            bool playing = !GameManager.Exists || GameManager.Instance.IsPlaying;
            if (!playing) {
                return;
            }

            float speed = RunDirector.Instance != null ? RunDirector.Instance.Speed : fallbackSpeed;
            Vector3 p = Body.position;

            if (_hasCurve && _curve.radius > 0f && _phi > 0f) {
                // Sweep down the C by angle; keep the lane (x) we spawned in.
                _phi -= _curve.AngularStep(speed, Time.deltaTime);
                Vector2 zy = _curve.PointAt(Mathf.Max(_phi, 0f));
                p.z = zy.x;
                p.y = zy.y;
            } else {
                // Off the arc (reached the player, or no curve): slide straight out the back.
                p.z -= speed * Time.deltaTime;
                if (_hasCurve) {
                    p.y = _curve.groundY;
                }
            }

            Body.position = p;

            if (p.z <= _despawnZ) {
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
