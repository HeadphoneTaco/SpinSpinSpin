using System.Collections.Generic;
using _Project.Code.Core;
using CoreUtils.AssetBuckets;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     Feeds the belt. Drops socks and obstacles into <see cref="laneCount" /> fixed lanes across the
    ///     drum at a steady spacing in world units (distance-based, so density stays honest as the run
    ///     speeds up). Each item enters high and traces a "C" down the drum wall to the gremlin via
    ///     <see cref="ApproachCurve" />; side-to-side lane (X) is separate so the player can steer.
    ///
    ///     Variety comes from CoreUtils <see cref="PrefabBucket" />s (one for socks, one for obstacles).
    ///     Spacing is scaled by <see cref="RunDirector.SpawnSpacingScale" /> so CollectSocks mode ramps
    ///     up frequency over time. Spent items recycle through a small per-prefab pool. Only runs while
    ///     Playing. The Scene-view gizmo draws each lane's C so you can tune it without pressing Play.
    /// </summary>
    public class TrackSpawner : MonoBehaviour, IScrollingItemPool {
        [Header("Prefabs (PrefabBuckets)")]
        [SerializeField] private PrefabBucket sockBucket;
        [SerializeField] private PrefabBucket obstacleBucket;
        [Tooltip("Optional full-width 'paddle' fins. When one spawns it spans the WHOLE drum and " +
                 "must be jumped - that row has no clear lane. Leave empty to disable paddles.")]
        [SerializeField] private PrefabBucket paddleBucket;

        [Header("Track shape")]
        [Tooltip("Z the gremlin runs at; the bottom of every C sits here.")]
        [SerializeField] private float playerZ = 0f;
        [Tooltip("Height items arrive at - the gremlin's run height.")]
        [SerializeField] private float groundY = 0f;
        [Tooltip("Z behind the gremlin where missed items are recycled.")]
        [SerializeField] private float despawnZ = -10f;
        [Tooltip("Half the drum width; the outermost lanes sit here.")]
        [SerializeField] private float halfWidth = 2.5f;
        [Tooltip("Number of lanes across the drum.")]
        [SerializeField] private int laneCount = 6;

        [Header("The C-curve (items ride this down to the player)")]
        [Tooltip("Radius of the C. Bigger = wider sweep and more reaction time.")]
        [SerializeField] private float radius = 10f;
        [Tooltip("How much of the circle to trace. 180 = a full C; less = a gentler hook.")]
        [SerializeField] private float sweepDegrees = 180f;
        [Tooltip("Vertical squish of the C: 1 = round, below 1 = flatter, above 1 = taller / exaggerated.")]
        [SerializeField] private float squish = 1f;

        [Header("Pacing")]
        [Tooltip("World-units between spawn rows. Smaller = denser track.")]
        [SerializeField] private float spawnSpacing = 8f;
        [Range(0f, 1f)] [SerializeField] private float obstacleChance = 0.55f;
        [Range(0f, 1f)] [SerializeField] private float sockChance = 0.7f;
        [Tooltip("Chance per row to throw a full-width paddle instead of the normal obstacle row. " +
                 "The paddle blocks every lane, so the player has to jump it. A sock can still ride " +
                 "a lane that row as a reward for clearing it.")]
        [Range(0f, 1f)] [SerializeField] private float paddleChance = 0.12f;

        private readonly Dictionary<GameObject, Stack<ScrollingItem>> _poolFor = new();
        private readonly Dictionary<ScrollingItem, GameObject> _prefabOf = new();

        private float _distanceSinceSpawn;

        private ApproachCurve Curve => new ApproachCurve {
            groundY = groundY,
            playerZ = playerZ,
            radius = radius,
            sweepDegrees = sweepDegrees,
            squish = squish
        };

        private void Update() {
            if (!GameManager.Exists || !GameManager.Instance.IsPlaying || RunDirector.Instance == null) {
                return;
            }

            float spacing = Mathf.Max(0.5f, spawnSpacing * RunDirector.Instance.SpawnSpacingScale);
            _distanceSinceSpawn += RunDirector.Instance.Speed * Time.deltaTime;
            while (_distanceSinceSpawn >= spacing) {
                _distanceSinceSpawn -= spacing;
                SpawnRow();
            }
        }

        private void SpawnRow() {
            int lanes = Mathf.Max(1, laneCount);

            // Full-width paddle first: it blocks every lane, so it replaces the normal obstacle row
            // and the player must jump. The paddle prefab is as wide as the whole play area, so it
            // always spawns in the MIDDLE lane (X = 0) - dead-centre, grid-aligned. A sock may still
            // ride a lane on top as a reward for clearing it.
            GameObject paddle = RandomPrefab(paddleBucket);
            if (paddle != null && Random.value < paddleChance) {
                SpawnItem(paddle, MiddleLane);
                GameObject reward = RandomPrefab(sockBucket);
                if (reward != null && Random.value < sockChance) {
                    SpawnItem(reward, Random.Range(0, lanes));
                }

                return;
            }

            // An obstacle and a sock land in two different lanes; either may sit the row out.
            int obstacleLane = Random.Range(0, lanes);
            int sockLane = lanes > 1
                ? (obstacleLane + 1 + Random.Range(0, lanes - 1)) % lanes
                : 0;

            GameObject obstacle = RandomPrefab(obstacleBucket);
            if (obstacle != null && Random.value < obstacleChance) {
                SpawnItem(obstacle, obstacleLane);
            }

            GameObject sock = RandomPrefab(sockBucket);
            if (sock != null && Random.value < sockChance) {
                SpawnItem(sock, sockLane);
            }
        }

        /// <summary>
        ///     The centre lane index. For an odd lane count it's the exact middle (X = 0); for an even
        ///     count it's the lane just past centre. Paddles spawn here so a full-width paddle sits
        ///     dead-centre regardless of how many lanes there are.
        /// </summary>
        private int MiddleLane => Mathf.Clamp(Mathf.Max(1, laneCount) / 2, 0, Mathf.Max(1, laneCount) - 1);

        /// <summary>World X of a lane centre, spread evenly across the drum width.</summary>
        private float LaneX(int lane) {
            int lanes = Mathf.Max(1, laneCount);
            if (lanes == 1) {
                return 0f;
            }

            return Mathf.Lerp(-halfWidth, halfWidth, lane / (float)(lanes - 1));
        }

        private static GameObject RandomPrefab(PrefabBucket bucket) {
            if (bucket == null) {
                return null;
            }

            GameObject[] items = bucket.Items;
            return items.Length > 0 ? items[Random.Range(0, items.Length)] : null;
        }

        private void SpawnItem(GameObject prefab, int lane) {
            ScrollingItem item = GetFromPool(prefab);
            if (item == null) {
                return;
            }

            ApproachCurve curve = Curve;
            Vector2 start = curve.PointAt(curve.StartAngle); // (z, y) at the top of the C
            float x = LaneX(lane);

            item.Configure(this, despawnZ, curve);
            item.Body.position = new Vector3(x, start.y, start.x);
            item.Body.gameObject.SetActive(true);
        }

        // --- Pooling -------------------------------------------------------------

        private ScrollingItem GetFromPool(GameObject prefab) {
            if (!_poolFor.TryGetValue(prefab, out Stack<ScrollingItem> stack)) {
                stack = new Stack<ScrollingItem>();
                _poolFor[prefab] = stack;
            }

            while (stack.Count > 0) {
                ScrollingItem recycled = stack.Pop();
                if (recycled != null) {
                    return recycled;
                }
            }

            GameObject instance = Instantiate(prefab);
            ScrollingItem fresh = instance.GetComponentInChildren<ScrollingItem>(true);
            if (fresh == null) {
                Debug.LogError($"[TrackSpawner] Prefab '{prefab.name}' has no Collectible/Obstacle " +
                               "(ScrollingItem) anywhere in it. Skipping.", prefab);
                Destroy(instance);
                return null;
            }

            _prefabOf[fresh] = prefab;
            fresh.Body.gameObject.SetActive(false);
            return fresh;
        }

        public void Release(ScrollingItem item) {
            if (item == null) {
                return;
            }

            item.Body.gameObject.SetActive(false);
            if (_prefabOf.TryGetValue(item, out GameObject prefab) && _poolFor.TryGetValue(prefab, out Stack<ScrollingItem> stack)) {
                stack.Push(item);
            } else {
                Destroy(item.Body.gameObject);
            }
        }

        // --- Scene-view visualisation -------------------------------------------

        // Draws each lane's C-curve so the track is tunable without pressing Play.
        // Yellow = the C each item rides; cyan = the arrival line at the gremlin.
        private void OnDrawGizmos() {
            int lanes = Mathf.Max(1, laneCount);
            ApproachCurve curve = Curve;

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            for (int i = 0; i < lanes; i++) {
                float x = LaneX(i);
                DrawC(x, curve);
                Vector2 start = curve.PointAt(curve.StartAngle);
                Gizmos.DrawSphere(new Vector3(x, start.y, start.x), 0.18f);
            }

            // Green "up" ticks fanning around the C: how each item is oriented as it rotates with the
            // drum. This previews the runtime tilt (ScrollingItem.alignToCurve) without pressing Play.
            Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.9f);
            for (int i = 0; i < lanes; i++) {
                float x = LaneX(i);
                const int ticks = 8;
                for (int t = 0; t <= ticks; t++) {
                    float phi = Mathf.Lerp(curve.StartAngle, 0f, t / (float)ticks);
                    Vector2 zy = curve.PointAt(phi);
                    Vector3 p = new Vector3(x, zy.y, zy.x);
                    Gizmos.DrawLine(p, p + curve.UpAt(phi) * 0.6f);
                }
            }

            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            Gizmos.DrawLine(new Vector3(-halfWidth, groundY, playerZ), new Vector3(halfWidth, groundY, playerZ));
        }

        private void DrawC(float x, ApproachCurve curve) {
            const int steps = 28;
            Vector3 prev = Vector3.zero;
            for (int s = 0; s <= steps; s++) {
                float phi = Mathf.Lerp(curve.StartAngle, 0f, s / (float)steps);
                Vector2 zy = curve.PointAt(phi);
                Vector3 p = new Vector3(x, zy.y, zy.x);
                if (s > 0) {
                    Gizmos.DrawLine(prev, p);
                }

                prev = p;
            }
        }
    }
}
