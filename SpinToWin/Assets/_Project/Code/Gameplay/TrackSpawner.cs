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
        [Header("Prefabs")]
        [SerializeField] private Collectible sockPrefab;
        [SerializeField] private Obstacle[] obstaclePrefabs;

        [Header("Where things appear")]
        [Tooltip("Z the gremlin sits at + how far ahead to spawn. Items ride from here toward 0.")]
        [SerializeField] private float spawnZ = 40f;
        [Tooltip("Z behind the gremlin where items are recycled.")]
        [SerializeField] private float despawnZ = -10f;
        [Tooltip("Ground height items spawn at.")]
        [SerializeField] private float groundY = 0f;
        [Tooltip("Half the drum width. Items spawn within ±90% of this.")]
        [SerializeField] private float halfWidth = 2.5f;

        [Header("Pacing")]
        [Tooltip("World-units between spawn rows. Smaller = denser track.")]
        [SerializeField] private float spawnSpacing = 8f;
        [Tooltip("Chance a row drops a hazard.")]
        [Range(0f, 1f)] [SerializeField] private float obstacleChance = 0.55f;
        [Tooltip("Chance a row drops a sock cluster.")]
        [Range(0f, 1f)] [SerializeField] private float sockChance = 0.7f;

        [Header("Sock clusters")]
        [SerializeField] private int sockClusterMin = 2;
        [SerializeField] private int sockClusterMax = 4;
        [Tooltip("Z-gap between socks in a cluster so they form a line to scoop up.")]
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
            ScrollingItem item = GetFromPool(prefab);
            item.transform.SetParent(transform, false);
            item.transform.position = new Vector3(x, groundY, spawnZ + zOffset);
            item.Configure(this, despawnZ);
            item.gameObject.SetActive(true);
        }

        // --- Pooling -------------------------------------------------------------

        private ScrollingItem GetFromPool(ScrollingItem prefab) {
            if (!_poolFor.TryGetValue(prefab, out Stack<ScrollingItem> stack)) {
                stack = new Stack<ScrollingItem>();
                _poolFor[prefab] = stack;
            }

            // Pop any still-valid recycled instance.
            while (stack.Count > 0) {
                ScrollingItem recycled = stack.Pop();
                if (recycled != null) {
                    return recycled;
                }
            }

            ScrollingItem fresh = Instantiate(prefab);
            _prefabOf[fresh] = prefab;
            return fresh;
        }

        public void Release(ScrollingItem item) {
            if (item == null) {
                return;
            }

            item.gameObject.SetActive(false);
            if (_prefabOf.TryGetValue(item, out ScrollingItem prefab) && _poolFor.TryGetValue(prefab, out Stack<ScrollingItem> stack)) {
                stack.Push(item);
            } else {
                Destroy(item.gameObject);
            }
        }
    }
}
