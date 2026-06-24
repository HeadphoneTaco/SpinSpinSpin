using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     The rectangle the gremlin is allowed to move within — the single source of truth for how
    ///     wide the lane is and how far forward/back the player may drift. <see cref="GremlinRunner" />
    ///     clamps to it, so changing the size here moves the walls and the on-screen boundary together.
    ///
    ///     The boundary is drawn two ways: a runtime <see cref="LineRenderer" /> outline the player can
    ///     see, and an editor gizmo (when selected) so you can size the area without entering play mode.
    ///     Place this on an empty object at the centre of the play area, on the floor, roughly where the
    ///     gremlin starts. +Z is "forward" (toward the oncoming items); -Z is "back".
    ///
    ///     Note for URP: the fallback line shader is "Sprites/Default", which usually renders fine for
    ///     a line. If the outline shows up magenta, assign a small URP unlit material to
    ///     <see cref="outlineMaterial" />.
    /// </summary>
    public class PlayArea : MonoBehaviour {
        [Header("Size (from this object's centre)")]
        [Min(0f)] [SerializeField] private float halfWidth = 4f;
        [Tooltip("How far the gremlin can push toward the oncoming items (+Z).")]
        [Min(0f)] [SerializeField] private float forwardExtent = 3f;
        [Tooltip("How far the gremlin can retreat from the oncoming items (-Z).")]
        [Min(0f)] [SerializeField] private float backExtent = 2f;

        [Header("Outline")]
        [SerializeField] private bool showOutline = true;
        [SerializeField] private Color outlineColor = new Color(0.3f, 0.9f, 1f, 0.9f);
        [Min(0f)] [SerializeField] private float outlineWidth = 0.08f;
        [Tooltip("Lifts the outline a hair off the floor so it doesn't z-fight with it.")]
        [SerializeField] private float outlineYOffset = 0.02f;
        [Tooltip("Optional. Material for the outline line. Leave empty to use a default unlit line.")]
        [SerializeField] private Material outlineMaterial;

        private LineRenderer _line;

        public float HalfWidth => halfWidth;
        public float XMin => transform.position.x - halfWidth;
        public float XMax => transform.position.x + halfWidth;
        public float ZMin => transform.position.z - backExtent;
        public float ZMax => transform.position.z + forwardExtent;
        public float FloorY => transform.position.y;

        /// <summary>Clamps a world position into the area on X and Z; Y is left untouched.</summary>
        public Vector3 Clamp(Vector3 worldPos) {
            worldPos.x = Mathf.Clamp(worldPos.x, XMin, XMax);
            worldPos.z = Mathf.Clamp(worldPos.z, ZMin, ZMax);
            return worldPos;
        }

        private void OnEnable() {
            BuildLine();
            RefreshOutline();
        }

        private void Update() {
            // Cheap, and keeps the outline correct if the area is moved/resized live while tuning.
            RefreshOutline();
        }

        private void BuildLine() {
            _line = GetComponent<LineRenderer>();
            if (_line == null) {
                _line = gameObject.AddComponent<LineRenderer>();
            }

            if (_line.sharedMaterial == null) {
                _line.sharedMaterial = outlineMaterial != null
                    ? outlineMaterial
                    : new Material(Shader.Find("Sprites/Default"));
            }

            _line.useWorldSpace = true;
            _line.loop = true;
            _line.positionCount = 4;
            _line.numCornerVertices = 2;
            _line.alignment = LineAlignment.View;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
        }

        private void RefreshOutline() {
            if (_line == null) {
                BuildLine();
            }

            _line.enabled = showOutline;
            if (!showOutline) {
                return;
            }

            _line.startColor = _line.endColor = outlineColor;
            _line.startWidth = _line.endWidth = outlineWidth;

            float y = FloorY + outlineYOffset;
            _line.SetPosition(0, new Vector3(XMin, y, ZMin));
            _line.SetPosition(1, new Vector3(XMin, y, ZMax));
            _line.SetPosition(2, new Vector3(XMax, y, ZMax));
            _line.SetPosition(3, new Vector3(XMax, y, ZMin));
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = outlineColor;
            float y = FloorY + outlineYOffset;
            Vector3 a = new Vector3(XMin, y, ZMin);
            Vector3 b = new Vector3(XMin, y, ZMax);
            Vector3 c = new Vector3(XMax, y, ZMax);
            Vector3 d = new Vector3(XMax, y, ZMin);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }
    }
}
