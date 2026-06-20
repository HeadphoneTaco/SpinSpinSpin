using System.Collections.Generic;
using _Project.Code.Core;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     Feeds the belt. Spawns socks and obstacles out ahead of the gremlin at a steady spacing in
    ///     world units (distance-based, not time-based — so density stays honest as the run speeds up),
    ///     scatters them across the drum width, and lets <see cref="ScrollingItem" /> carry them home.
    ///
    ///     Spent items are recycled through a small per-prefab pool (this class is the
    ///     <see cref="IScrollingItemPool" />), so a long run doesn't churn the garbage collector. Only
    ///     runs while the game is Playing. Place one in <c>Main</c>; assign the sock + obstacle prefabs.
    /// </summary>
    public class TrackSpawner : MonoBehaviour, IScrollingItemPool {
        [SerializeField] private Collectible sockPrefab;
        [SerializeField] private Obstacle[] obstaclePrefabs;
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
        private readonly Dictionary<ScrollingItem, Stack<ScrollingItem>> _poolFor = new();
        private readonly Dictionary<ScrollingItem, ScrollingItem> _prefabOf = new();

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
                Obstacle prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
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

        private void SpawnItem(ScrollingItem prefab, float x, float zOffset) {
            // Move/show the whole prefab via Body — the script may sit on a child object.
            ScrollingItem item = GetFromPool(prefab);
            item.Configure(this, despawnZ);
            item.Body.position = new Vector3(x, groundY, spawnZ + zOffset);
            item.Body.gameObject.SetActive(true);
        }

        // --- Pooling -------------------------------------------------------------

        private ScrollingItem GetFromPool(ScrollingItem prefab) {
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

            ScrollingItem fresh = Instantiate(prefab);
            _prefabOf[fresh] = prefab;
            fresh.Body.gameObject.SetActive(false); // Configure + position it before it's shown.
            return fresh;
        }

        public void Release(ScrollingItem item) {
            if (item == null) {
                return;
            }

            item.Body.gameObject.SetActive(false);
            if (_prefabOf.TryGetValue(item, out ScrollingItem prefab) && _poolFor.TryGetValue(prefab, out Stack<ScrollingItem> stack)) {
                stack.Push(item);
            } else {
                Destroy(item.Body.gameObject);
            }
        }
    }
}
