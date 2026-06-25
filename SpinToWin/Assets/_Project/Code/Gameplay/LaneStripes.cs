using System.Collections.Generic;
using _Project.Code.Core;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     A rough-in for the playtest note "dash lines for lanes": a scrolling dashed guide running
    ///     down each of the drum's lanes so players can read where the lanes are - and so the floor
    ///     visibly rushes toward them, selling the speed. It mirrors <see cref="TrackSpawner" />'s lane
    ///     layout (match halfWidth / laneCount / groundY to it) and builds a pool of thin quad "dashes"
    ///     per lane, sliding them toward the gremlin at the current run Speed and wrapping them back to
    ///     the far end. Placeholder visuals only: drop a striped/worn material on the Material field (or
    ///     let the team re-skin the generated quads) to make it art-final.
    ///
    ///     This is a scene helper (like TrackSpawner), not a Code/Mesh-split prefab - put it on its own
    ///     object in the gameplay scene. The generated dashes have their colliders stripped so they
    ///     never interfere with the gremlin's item scan or movement.
    /// </summary>
    public class LaneStripes : MonoBehaviour {
        [Header("Lane layout (match TrackSpawner)")]
        [Tooltip("Half the drum width; outermost lanes sit here. Match TrackSpawner.halfWidth.")]
        [SerializeField] private float halfWidth = 2.5f;
        [Tooltip("Number of lanes across the drum. Match TrackSpawner.laneCount.")]
        [SerializeField] private int laneCount = 6;
        [Tooltip("Floor height the dashes sit on. Match TrackSpawner.groundY (a hair is added so they " +
                 "don't z-fight the floor).")]
        [SerializeField] private float groundY = 0f;

        [Header("Run of the stripes (Z)")]
        [Tooltip("Far end the dashes stream in from - put it at/above where items first appear.")]
        [SerializeField] private float farZ = 30f;
        [Tooltip("Near end the dashes recycle at - put it behind the gremlin (TrackSpawner.despawnZ).")]
        [SerializeField] private float nearZ = -10f;

        [Header("Dash look")]
        [Tooltip("Length of one dash along Z.")]
        [SerializeField] private float dashLength = 1.2f;
        [Tooltip("Gap between dashes along Z.")]
        [SerializeField] private float gapLength = 1.4f;
        [Tooltip("Width of the dash across X.")]
        [SerializeField] private float lineWidth = 0.12f;
        [Tooltip("Optional material for the dashes. Leave empty for a flat unlit colour the team can " +
                 "swap for striped/worn art later.")]
        [SerializeField] private Material material;
        [Tooltip("Tint used when no Material is assigned.")]
        [SerializeField] private Color color = new Color(1f, 1f, 1f, 0.5f);

        [Header("Motion")]
        [Tooltip("Multiplies the run Speed for the scroll. 1 = matches the world; >1 reads faster.")]
        [SerializeField] private float speedScale = 1f;
        [Tooltip("Scroll at this speed when not in a run (so the menu/idle still looks alive). " +
                 "0 = stand still until Playing.")]
        [SerializeField] private float idleSpeed = 0f;

        private readonly List<Transform> _dashes = new();
        private float _span; // dash + gap, the wrap distance
        private Material _runtimeMaterial;

        private void Start() {
            Build();
        }

        private void OnDestroy() {
            if (_runtimeMaterial != null) {
                Destroy(_runtimeMaterial);
            }
        }

        private void Build() {
            _span = Mathf.Max(0.05f, dashLength + gapLength);

            Material mat = material;
            if (mat == null) {
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
                _runtimeMaterial = new Material(shader) { color = color };
                mat = _runtimeMaterial;
            }

            int lanes = Mathf.Max(1, laneCount);
            int perLane = Mathf.CeilToInt((farZ - nearZ) / _span) + 1;

            for (int lane = 0; lane < lanes; lane++) {
                float x = LaneX(lane);
                for (int d = 0; d < perLane; d++) {
                    float z = farZ - d * _span;
                    _dashes.Add(MakeDash(mat, x, z));
                }
            }
        }

        private Transform MakeDash(Material mat, float x, float z) {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "LaneDash";

            // Strip the collider so dashes never touch the gremlin's scan or movement.
            Collider col = quad.GetComponent<Collider>();
            if (col != null) {
                Destroy(col);
            }

            Renderer r = quad.GetComponent<Renderer>();
            if (r != null) {
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }

            Transform t = quad.transform;
            t.SetParent(transform, false);
            // A Quad faces +Z and is 1x1; lay it flat so its local Y maps to world Z.
            t.localRotation = Quaternion.Euler(90f, 0f, 0f);
            t.localScale = new Vector3(lineWidth, dashLength, 1f);
            t.position = new Vector3(x, groundY + 0.01f, z);
            return t;
        }

        private void Update() {
            float speed = idleSpeed;
            if (GameManager.Exists && GameManager.Instance.IsPlaying && RunDirector.Instance != null) {
                speed = RunDirector.Instance.Speed * speedScale;
            }

            if (speed == 0f) {
                return;
            }

            float step = speed * Time.deltaTime;
            float low = nearZ;
            float high = nearZ + _span * Mathf.Ceil((farZ - nearZ) / _span);

            for (int i = 0; i < _dashes.Count; i++) {
                Transform t = _dashes[i];
                if (t == null) {
                    continue;
                }

                Vector3 p = t.position;
                p.z -= step;
                if (p.z < low) {
                    p.z += high - low; // wrap back to the far end, keeping the cadence
                }

                t.position = p;
            }
        }

        private float LaneX(int lane) {
            int lanes = Mathf.Max(1, laneCount);
            if (lanes == 1) {
                return 0f;
            }

            return Mathf.Lerp(-halfWidth, halfWidth, lane / (float)(lanes - 1));
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            int lanes = Mathf.Max(1, laneCount);
            for (int i = 0; i < lanes; i++) {
                float x = LaneX(i);
                Gizmos.DrawLine(new Vector3(x, groundY, nearZ), new Vector3(x, groundY, farZ));
            }
        }
    }
}
