using System.Collections.Generic;
using _Project.Code.Core;
using CoreUtils.AssetBuckets;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     Feeds the belt. Spawns socks and obstacles out ahead of the gremlin at a steady spacing in
    ///     world units (distance-based, not time-based, so density stays honest as the run speeds up),
    ///     scatters them across the drum width, and lets <see cref="ScrollingItem" /> carry them home.
    ///
    ///     Sock/obstacle variety comes from CoreUtils <see cref="PrefabBucket" />s: point a bucket at a
    ///     folder of sock prefabs and every type in it is picked from at random. Prefabs are referenced
    ///     as plain GameObjects (the prefab root), so the Code / Colliders / Mesh split is fine - the
    ///     <see cref="Collectible" /> / <see cref="Obstacle" /> script can sit on a `Code` child, and the
    ///     spawner resolves the <see cref="ScrollingItem" /> from the instance's children at spawn time.
    ///
    ///     Spent items are recycled through a small per-prefab pool (this class is the
    ///     <see cref="IScrollingItemPool" />), so a long run doesn't churn the garbage collector. Only
    ///     runs while the game is Playing. Place one in <c>Main</c>; assign the sock + obstacle buckets.
    /// </summary>
    public class TrackSpawner : MonoBehaviour, IScrollingItemPool {
        [Tooltip("PrefabBucket of sock prefabs (CoreUtils/Bucket/Prefab Bucket). One is picked at random per cluster.")]
        [SerializeField] private PrefabBucket sockBucket;
        [Tooltip("PrefabBucket of obstacle prefabs. One is picked at random per row.")]
        [SerializeField] private PrefabBucket obstacleBucket;

        [SerializeField] private float spawnZ = 40f;
        [SerializeField] private float despawnZ = -10f;
        [Tooltip("World Y the items ride at. Lift this to the gremlin's body height so they sit on the " +
                 "floor / float at collection height rather than sinking below it.")]
        [SerializeField] private float groundY = 0f;
        [SerializeField] private float halfWidth = 2.5f;
        [SerializeField] private float spawnSpacing = 8f;
        [Range(0f, 1f)] [SerializeField] private float obstacleChance = 0.55f;
        [Range(0f, 1f)] [SerializeField] private float sockChance = 0.7f;
        [SerializeField] private int sockClusterMin = 2;
        [SerializeField] private int sockClusterMax = 4;
        [SerializeField] private float sockSpacing = 2.5f;

        // Recycled-instance stacks, one per prefab, plus a lookup from instance back to its prefab.
        private readonly Dictionary<GameObject, Stack<ScrollingItem>> _poolFor = new();
        private readonly Dictionary<ScrollingItem, GameObject> _prefabOf = new();

        private float _distanceSinceSpawn;

        private void Update() {
            if (!GameManager.Exists || !GameManager.Instance.IsPlaying || RunDirector.Instance == null) {
                return;
            }

            // March forward by however far the belt moved this frame, dropping a row each spacing.
            _distanceSinceSpawn += RunDirector.Instance.Speed * Time.deltaTime;
            while (_distanceSinceSpawn >= spawnSpacing) {
                _distanceSinceSpawn -= spawnSpacing;
                SpawnRow();
            }
        }

        private void SpawnRow() {
            GameObject obstacle = RandomPrefab(obstacleBucket);
            if (obstacle != null && Random.value < obstacleChance) {
                SpawnItem(obstacle, RandomX(), 0f);
            }

            GameObject sock = RandomPrefab(sockBucket);
            if (sock != null && Random.value < sockChance) {
                float x = RandomX();
                int count = Random.Range(sockClusterMin, sockClusterMax + 1);
                for (int i = 0; i < count; i++) {
                    // Stagger socks further ahead so they arrive as a collectible line of one type.
                    SpawnItem(sock, x, i * sockSpacing);
                }
            }
        }

        private static GameObject RandomPrefab(PrefabBucket bucket) {
            if (bucket == null) {
                return null;
            }

            GameObject[] items = bucket.Items;
            return items.Length > 0 ? items[Random.Range(0, items.Length)] : null;
        }

        private float RandomX() {
            float limit = halfWidth * 0.9f;
            return Random.Range(-limit, limit);
        }

        private void SpawnItem(GameObject prefab, float x, float zOffset) {
            ScrollingItem item = GetFromPool(prefab);
            if (item == null) {
                return;
            }

            item.Configure(this, despawnZ);
            item.Body.position = new Vector3(x, groundY, spawnZ + zOffset);
            item.Body.gameObject.SetActive(true);
        }

        // --- Pooling -------------------------------------------------------------

        private ScrollingItem GetFromPool(GameObject prefab) {
            if (!_poolFor.TryGetValue(prefab, out Stack<ScrollingItem> stack)) {
                stack = new Stack<ScrollingItem>();
                _poolFor[prefab] = stack;
            }

            // Pop any still-valid recycled instance (already inactive from Release).
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
            fresh.Body.gameObject.SetActive(false); // Configure + position it before it's shown.
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
    }
}
