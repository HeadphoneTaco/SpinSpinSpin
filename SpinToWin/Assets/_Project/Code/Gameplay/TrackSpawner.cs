using System.Collections.Generic;
using _Project.Code.Core;
using CoreUtils.AssetBuckets;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     Feeds the belt. Two ways to decide what goes in each row, chosen automatically:
    ///
    ///     1. WAVE MODE (if any waves are assigned) - authored routing. Plays the Intro Waves once in
    ///        bucket order to teach mechanics, then pulls Pool Waves at random. Each wave is a painted
    ///        grid (see <see cref="SpawnWave" />); one wave row is spawned per row tick.
    ///     2. RANDOM MODE (no waves assigned) - the original dice-roll fallback: a random obstacle and
    ///        sock in two different lanes, with an occasional full-width paddle.
    ///
    ///     Either way THIS class owns placement: it maps a lane to its X, drops the item at the top of
    ///     the C it rides down (<see cref="ApproachCurve" />), and recycles spent items through a small
    ///     per-prefab pool. Row cadence is distance-based (so density stays honest as the run speeds up)
    ///     and scaled by <see cref="RunDirector.SpawnSpacingScale" />. Only runs while Playing. The
    ///     Scene-view gizmo draws each lane's C so you can tune the track without pressing Play.
    /// </summary>
    public class TrackSpawner : MonoBehaviour, IScrollingItemPool {
        [Header("Prefabs (PrefabBuckets)")]
        [Tooltip("Common-tier socks ('o' cells, and every sock in random mode).")]
        [SerializeField] private PrefabBucket sockBucket;
        [Tooltip("Uncommon-tier socks ('u' cells). Used in wave mode.")]
        [SerializeField] private PrefabBucket uncommonSockBucket;
        [Tooltip("Rare-tier socks ('r' cells). Used in wave mode - keep this bucket small/special.")]
        [SerializeField] private PrefabBucket rareSockBucket;
        [SerializeField] private PrefabBucket obstacleBucket;
        [Tooltip("Full-width 'paddle' fins. In random mode a paddle blocks every lane and must be " +
                 "jumped; in wave mode a paddle cell places one in its lane (full-width prefabs go in " +
                 "the middle column). Leave empty to disable paddles.")]
        [SerializeField] private PrefabBucket paddleBucket;

        [Header("Waves (optional - authored routing)")]
        [Tooltip("Waves played ONCE, in bucket order, at the start of a run to introduce mechanics. " +
                 "Sort the bucket to control the teaching order. Leave empty to skip the intro.")]
        [SerializeField] private SpawnWaveBucket introWaves;
        [Tooltip("Waves pulled at RANDOM after the intro. If BOTH wave buckets are empty, the spawner " +
                 "falls back to the random per-row logic (the Pacing section below).")]
        [SerializeField] private SpawnWaveBucket poolWaves;

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

        [Header("Pacing (random mode)")]
        [Tooltip("World-units between spawn rows. Smaller = denser track. Used in both modes.")]
        [SerializeField] private float spawnSpacing = 8f;
        [Range(0f, 1f)] [SerializeField] private float obstacleChance = 0.55f;
        [Range(0f, 1f)] [SerializeField] private float sockChance = 0.7f;
        [Tooltip("Random mode only: chance per row to throw a full-width middle-lane paddle.")]
        [Range(0f, 1f)] [SerializeField] private float paddleChance = 0.12f;

        private readonly Dictionary<GameObject, Stack<ScrollingItem>> _poolFor = new();
        private readonly Dictionary<ScrollingItem, GameObject> _prefabOf = new();

        private float _distanceSinceSpawn;

        // Random-mode paddle intro counter: hand out paddles in bucket order until each has appeared,
        // then go random. Resets each run with the scene. (Wave mode does its own teaching via waves.)
        private int _paddlesSpawned;

        // Wave-mode playback state. _wave = the wave currently streaming out, _waveRow = the next of its
        // rows to spawn, _introCursor = how many intro waves we've used. All reset each run with the scene.
        private SpawnWave _wave;
        private int _waveRow;
        private int _introCursor;

        private bool WavesAssigned =>
            (introWaves != null && introWaves.Items.Length > 0) ||
            (poolWaves != null && poolWaves.Items.Length > 0);

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
            if (WavesAssigned) {
                SpawnWaveRow();
            } else {
                SpawnRandomRow();
            }
        }

        // --- Wave mode -----------------------------------------------------------

        /// <summary>Spawns the next row of the current wave, picking a new wave when one runs out.</summary>
        private void SpawnWaveRow() {
            if (_wave == null || _waveRow >= _wave.RowCount) {
                _wave = NextWave();
                _waveRow = 0;
                if (_wave == null || _wave.RowCount == 0) {
                    _wave = null; // empty/blank wave - try again next tick
                    return;
                }
            }

            WaveCell[] row = _wave.Row(_waveRow);
            int lanes = Mathf.Max(1, laneCount);
            for (int lane = 0; lane < row.Length && lane < lanes; lane++) {
                SpawnCell(row[lane], lane);
            }

            _waveRow++;
        }

        /// <summary>Intro waves first (in bucket order, once each), then a random pool wave.</summary>
        private SpawnWave NextWave() {
            if (introWaves != null && _introCursor < introWaves.Items.Length) {
                return introWaves.Items[_introCursor++];
            }

            if (poolWaves != null && poolWaves.Items.Length > 0) {
                return poolWaves.Items[Random.Range(0, poolWaves.Items.Length)];
            }

            return null;
        }

        private void SpawnCell(WaveCell cell, int lane) {
            switch (cell.Kind) {
                case WaveCellKind.CommonSock:
                    SpawnItem(RandomPrefab(sockBucket), lane);
                    break;
                case WaveCellKind.UncommonSock:
                    SpawnItem(RandomPrefab(uncommonSockBucket), lane);
                    break;
                case WaveCellKind.RareSock:
                    SpawnItem(RandomPrefab(rareSockBucket), lane);
                    break;
                case WaveCellKind.Obstacle:
                    SpawnItem(RandomPrefab(obstacleBucket), lane);
                    break;
                case WaveCellKind.Paddle:
                    SpawnItem(PaddleAt(cell.PaddleIndex), lane);
                    break;
                // Empty: nothing to do.
            }
        }

        /// <summary>A specific paddle by bucket index, or the auto intro/random paddle when index &lt; 0 or out of range.</summary>
        private GameObject PaddleAt(int index) {
            if (paddleBucket == null) {
                return null;
            }

            GameObject[] items = paddleBucket.Items;
            if (items.Length == 0) {
                return null;
            }

            return index >= 0 && index < items.Length ? items[index] : NextPaddle();
        }

        // --- Random mode (fallback) ---------------------------------------------

        private void SpawnRandomRow() {
            int lanes = Mathf.Max(1, laneCount);

            // Paddle row first: a full-width paddle blocks every lane (middle-lane prefab), so the
            // player must jump. First paddles of a run come out in bucket order (NextPaddle) to
            // introduce each variant; then random. A sock may ride a lane on top as a reward.
            if (Random.value < paddleChance) {
                GameObject paddle = NextPaddle();
                if (paddle != null) {
                    SpawnItem(paddle, MiddleLane);
                    GameObject reward = RandomPrefab(sockBucket);
                    if (reward != null && Random.value < sockChance) {
                        SpawnItem(reward, Random.Range(0, lanes));
                    }

                    return;
                }
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
        ///     Picks the next paddle prefab. For the first paddles of a run it walks the bucket IN ORDER
        ///     (one of each) so the player is introduced to every variant; once each has appeared it
        ///     falls back to a random pick. Returns null if the paddle bucket is empty.
        /// </summary>
        private GameObject NextPaddle() {
            if (paddleBucket == null) {
                return null;
            }

            GameObject[] items = paddleBucket.Items;
            if (items.Length == 0) {
                return null;
            }

            GameObject chosen = _paddlesSpawned < items.Length
                ? items[_paddlesSpawned] // intro: next in bucket order
                : items[Random.Range(0, items.Length)]; // then random
            _paddlesSpawned++;
            return chosen;
        }

        // --- Placement + helpers -------------------------------------------------

        /// <summary>
        ///     The centre lane index. For an odd lane count it's the exact middle (X = 0); for an even
        ///     count it's the lane just past centre. Random-mode paddles spawn here so a full-width
        ///     paddle sits dead-centre regardless of how many lanes there are.
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
            if (prefab == null) {
                return;
            }

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
