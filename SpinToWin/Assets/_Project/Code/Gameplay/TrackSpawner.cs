using System.Collections.Generic;
using _Project.Code.Core;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     Feeds the belt. Spawns socks and obstacles out ahead of the gremlin at a steady spacing in
    ///     world units (distance-based, not time-based, so density stays honest as the run speeds up),
    ///     scatters them across the drum width, and lets <see cref="ScrollingItem" /> carry them home.
    ///
    ///     Prefabs are referenced as plain <see cref="GameObject" />s (the prefab root), so the
    ///     Code / Colliders / Mesh split is fine: the <see cref="Collectible" /> / <see cref="Obstacle" />
    ///     script can sit on a `Code` child. The spawner resolves the <see cref="ScrollingItem" /> from
    ///     the instance's children at spawn time.
    ///
    ///     Spent items are recycled through a small per-prefab pool (this class is the
    ///     <see cref="IScrollingItemPool" />), so a long run doesn't churn the garbage collector. Only
    ///     runs while the game is Playing. Place one in <c>Main</c>; assign the sock + obstacle prefabs.
    /// </summary>
    public class TrackSpawner : MonoBehaviour, IScrollingItemPool {
        [Tooltip("The sock prefab (drag the prefab root; its Collectible can be on a Code child).")]
        [SerializeField] private GameObject sockPrefab;
        [Tooltip("Obstacle prefab roots (each needs an Obstacle somewhere in its hierarchy).")]
        [SerializeField] private GameObject[] obstaclePrefabs;

        [SerializeField] private float spawnZ = 40f;
        [SerializeField] private float despawnZ = -10f;
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
            if (obstaclePrefabs != null && obstaclePrefabs.Length > 0 && Random.value < obstacleChance) {
                GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                SpawnItem(prefab, RandomX(), 0f);
            }

            if (sockPrefab != null && Random.value < sockChance) {
                float x = RandomX();
                int count = Random.Range(sockClusterMin, sockClusterMax + 1);
                for (int i = 0; i < count; i++) {
                    // Stagger socks further ahead so they arrive as a collectible line.
                    SpawnItem(sockPrefab, x, i * sockSpacing);
                }
            }
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
