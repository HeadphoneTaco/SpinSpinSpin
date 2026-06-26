using _Project.Code.Gameplay;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _Project.Code.Editor {
    /// <summary>
    ///     A live grid preview for <see cref="SpawnWave" /> assets, oriented to match the GAME VIEW: the
    ///     player sits at the bottom (a blue play-area bar) and rows scroll down toward them, so the
    ///     bottom row is the one that arrives first. (You still paint top-down in the text - the first
    ///     line arrives first - the preview just flips it so it reads like the screen.)
    ///
    ///     Cells are coloured by kind - sock tiers, obstacles, empties. A paddle is drawn as a bar
    ///     across the row: lanes the player can HOP show a cyan jump line; lanes blocked by the paddle's
    ///     raised wall are shaded dark. Which lanes are which comes from the variant
    ///     (<see cref="JumpableLanesByPaddle" />), so you can see the route a paddle forces. Uses the
    ///     same <see cref="SpawnWave.Parse" /> the spawner uses, so the preview can't drift from spawns.
    /// </summary>
    [CustomEditor(typeof(SpawnWave))]
    public class SpawnWaveInspector : UnityEditor.Editor {
        private const int LaneCount = 7;   // the track's lane count, for alignment + "ignored" marking
        private const float CellSize = 22f;
        private const float Gap = 2f;
        private const float PlayAreaHeight = 12f;

        private static readonly Color JumpLine = new Color(0.25f, 0.95f, 0.95f, 1f);
        private static readonly Color WallShade = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color PlayArea = new Color(0.20f, 0.45f, 0.85f, 1f);
        private static readonly Color GapBg = new Color(0.12f, 0.12f, 0.12f);

        private GUIStyle _glyphStyle;

        /// <summary>
        ///     Which lanes (0-6, left to right) a paddle variant can be JUMPED in - the low sections.
        ///     The lanes not listed are the raised block (a wall you have to route around). Indexed by
        ///     paddle bucket index, matching the prefab names: 0 Basic / 1 Center / 2 Left / 3 Right.
        ///     EDIT THESE to match the meshes - they're the single source the preview reads.
        /// </summary>
        private static readonly int[][] JumpableLanesByPaddle = {
            new[] { 0, 1, 2, 3, 4, 5, 6 }, // 0 Basic  - low everywhere: hop any lane
            new[] { 0, 1, 5, 6 },          // 1 Center - raised middle (2-4): hop the sides
            new[] { 3, 4, 5, 6 },          // 2 Left   - raised left (0-2): hop the right
            new[] { 0, 1, 2, 3 },          // 3 Right  - raised right (4-6): hop the left
        };

        private static bool IsJumpable(int paddleIndex, int lane) {
            if (paddleIndex < 0 || paddleIndex >= JumpableLanesByPaddle.Length) {
                return true; // auto 'p' or unknown index: assume the whole width is jumpable
            }

            foreach (int l in JumpableLanesByPaddle[paddleIndex]) {
                if (l == lane) {
                    return true;
                }
            }

            return false;
        }

        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            SerializedProperty gridProp = serializedObject.FindProperty("grid");
            List<WaveCell[]> rows = SpawnWave.Parse(gridProp != null ? gridProp.stringValue : string.Empty);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            if (rows.Count == 0) {
                EditorGUILayout.HelpBox(
                    "Paint a grid in the field above to preview it.\n" +
                    "One line = one row (the FIRST line arrives first); one character = one lane.",
                    MessageType.Info);
                DrawLegend();
                return;
            }

            int cols = LaneCount;
            foreach (WaveCell[] r in rows) {
                cols = Mathf.Max(cols, r.Length);
            }

            EditorGUILayout.LabelField("Game view: rows scroll down to the player ▾ (bottom = arrives first)",
                EditorStyles.miniLabel);
            DrawColumnHeader(cols);
            DrawGrid(rows, cols);
            if (cols > LaneCount) {
                EditorGUILayout.HelpBox($"Rows are wider than {LaneCount} lanes; the greyed columns past " +
                                        $"lane {LaneCount - 1} are ignored by the spawner.", MessageType.Warning);
            }

            DrawLegend();
        }

        private void DrawColumnHeader(int cols) {
            Rect area = GUILayoutUtility.GetRect(cols * (CellSize + Gap) + Gap, 16f, GUILayout.ExpandWidth(false));
            for (int c = 0; c < cols; c++) {
                Rect cell = new Rect(area.x + Gap + c * (CellSize + Gap), area.y, CellSize, 16f);
                bool middle = c == LaneCount / 2;
                GUIStyle style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
                if (middle) {
                    style.normal.textColor = new Color(0.95f, 0.55f, 0.2f);
                    style.fontStyle = FontStyle.Bold;
                }

                GUI.Label(cell, c.ToString(), style);
            }
        }

        private void DrawGrid(List<WaveCell[]> rows, int cols) {
            EnsureStyle();

            float w = cols * (CellSize + Gap) + Gap;
            float h = rows.Count * (CellSize + Gap) + Gap + PlayAreaHeight + Gap;
            Rect area = GUILayoutUtility.GetRect(w, h, GUILayout.ExpandWidth(false));

            EditorGUI.DrawRect(area, GapBg);

            float laneSpan = LaneCount * CellSize + (LaneCount - 1) * Gap;

            // Rows are drawn FLIPPED: the first wave row (top of the text) is drawn at the BOTTOM, next
            // to the play area, so the preview reads the way the game does (player at the bottom).
            for (int di = 0; di < rows.Count; di++) {
                int ri = rows.Count - 1 - di;
                WaveCell[] row = rows[ri];
                float rowY = area.y + Gap + di * (CellSize + Gap);
                bool isPaddleRow = TryFirstPaddle(row, out WaveCell paddle);

                if (isPaddleRow) {
                    Rect bar = new Rect(area.x + Gap, rowY, laneSpan, CellSize);
                    EditorGUI.DrawRect(bar, ColorFor(WaveCellKind.Paddle));
                    DrawPaddleLanes(paddle.PaddleIndex, area.x, rowY);
                }

                for (int ci = 0; ci < cols; ci++) {
                    WaveCell cell = ci < row.Length ? row[ci] : WaveCell.Empty;
                    bool ignored = ci >= LaneCount;

                    // Paddle row: the bar covers the lane; only layer non-empty, non-paddle items on top.
                    // (Paddle cells are shown by the bar + jump lines.) Ignored columns still draw.
                    if (isPaddleRow && !ignored &&
                        (cell.Kind == WaveCellKind.Paddle || cell.Kind == WaveCellKind.Empty)) {
                        continue;
                    }

                    Rect rect = new Rect(area.x + Gap + ci * (CellSize + Gap), rowY, CellSize, CellSize);
                    Color col = ColorFor(cell.Kind);
                    if (ignored) {
                        col = Color.Lerp(col, GapBg, 0.6f);
                    }

                    EditorGUI.DrawRect(rect, col);

                    string glyph = GlyphFor(cell);
                    if (!string.IsNullOrEmpty(glyph)) {
                        _glyphStyle.normal.textColor = TextColorFor(cell.Kind);
                        GUI.Label(rect, glyph, _glyphStyle);
                    }
                }
            }

            // The play area: a blue bar along the bottom marking where the gremlin runs.
            float playY = area.y + Gap + rows.Count * (CellSize + Gap);
            Rect play = new Rect(area.x + Gap, playY, laneSpan, PlayAreaHeight);
            EditorGUI.DrawRect(play, PlayArea);
            GUIStyle playStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
            playStyle.normal.textColor = Color.white;
            GUI.Label(play, "play area", playStyle);
        }

        /// <summary>
        ///     Marks each lane of a paddle bar: a cyan jump line where the player can hop it, or a dark
        ///     shade where the raised block walls the lane off. The variant number (0-3) or "P" is
        ///     centred on the bar. This is the routing the paddle forces.
        /// </summary>
        private void DrawPaddleLanes(int paddleIndex, float areaX, float rowY) {
            for (int lane = 0; lane < LaneCount; lane++) {
                Rect laneRect = new Rect(areaX + Gap + lane * (CellSize + Gap), rowY, CellSize, CellSize);
                if (IsJumpable(paddleIndex, lane)) {
                    float cx = laneRect.x + CellSize * 0.5f;
                    EditorGUI.DrawRect(new Rect(cx - 1.5f, rowY, 3f, CellSize), JumpLine);
                } else {
                    EditorGUI.DrawRect(laneRect, WallShade); // raised block: can't jump this lane
                }
            }

            int centre = LaneCount / 2;
            Rect label = new Rect(areaX + Gap + centre * (CellSize + Gap), rowY, CellSize, CellSize);
            _glyphStyle.normal.textColor = Color.white;
            GUI.Label(label, paddleIndex >= 0 ? paddleIndex.ToString() : "P", _glyphStyle);
        }

        private static bool TryFirstPaddle(WaveCell[] row, out WaveCell paddle) {
            for (int i = 0; i < row.Length && i < LaneCount; i++) {
                if (row[i].Kind == WaveCellKind.Paddle) {
                    paddle = row[i];
                    return true;
                }
            }

            paddle = WaveCell.Empty;
            return false;
        }

        private void DrawLegend() {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Legend", EditorStyles.miniBoldLabel);
            LegendRow(WaveCellKind.CommonSock, "o", "common sock");
            LegendRow(WaveCellKind.UncommonSock, "u", "uncommon sock");
            LegendRow(WaveCellKind.RareSock, "r", "rare sock");
            LegendRow(WaveCellKind.Obstacle, "x", "obstacle (also #)");
            LegendRow(WaveCellKind.Paddle, "P", "paddle - cyan = jumpable lane, dark = wall (0-3 = variant)");
            LegendRow(WaveCellKind.Empty, "", ".  or space = empty");
        }

        private void LegendRow(WaveCellKind kind, string glyph, string label) {
            EnsureStyle();
            Rect line = GUILayoutUtility.GetRect(0f, CellSize, GUILayout.ExpandWidth(true));
            Rect swatch = new Rect(line.x, line.y, CellSize, CellSize);
            EditorGUI.DrawRect(swatch, ColorFor(kind));
            if (!string.IsNullOrEmpty(glyph)) {
                _glyphStyle.normal.textColor = TextColorFor(kind);
                GUI.Label(swatch, glyph, _glyphStyle);
            }

            Rect text = new Rect(line.x + CellSize + 6f, line.y, line.width - CellSize - 6f, CellSize);
            GUI.Label(text, label, EditorStyles.label);
        }

        private void EnsureStyle() {
            if (_glyphStyle == null) {
                _glyphStyle = new GUIStyle(EditorStyles.boldLabel) {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11
                };
            }
        }

        private static Color ColorFor(WaveCellKind kind) {
            switch (kind) {
                case WaveCellKind.CommonSock: return new Color(0.85f, 0.85f, 0.85f);
                case WaveCellKind.UncommonSock: return new Color(0.30f, 0.55f, 0.95f);
                case WaveCellKind.RareSock: return new Color(0.96f, 0.78f, 0.20f);
                case WaveCellKind.Obstacle: return new Color(0.85f, 0.30f, 0.28f);
                case WaveCellKind.Paddle: return new Color(0.95f, 0.55f, 0.20f);
                default: return new Color(0.22f, 0.22f, 0.22f); // Empty
            }
        }

        private static Color TextColorFor(WaveCellKind kind) {
            switch (kind) {
                case WaveCellKind.UncommonSock:
                case WaveCellKind.Obstacle:
                case WaveCellKind.Paddle:
                    return Color.white;
                default:
                    return Color.black;
            }
        }

        private static string GlyphFor(WaveCell cell) {
            switch (cell.Kind) {
                case WaveCellKind.CommonSock: return "o";
                case WaveCellKind.UncommonSock: return "u";
                case WaveCellKind.RareSock: return "r";
                case WaveCellKind.Obstacle: return "x";
                case WaveCellKind.Paddle: return cell.PaddleIndex >= 0 ? cell.PaddleIndex.ToString() : "P";
                default: return string.Empty;
            }
        }
    }
}
