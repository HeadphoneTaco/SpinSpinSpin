using System.Collections.Generic;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>What a single grid cell tells the spawner to drop.</summary>
    public enum WaveCellKind {
        Empty,
        CommonSock,
        UncommonSock,
        RareSock,
        Obstacle,
        Paddle
    }

    /// <summary>One parsed cell: what to spawn, plus which paddle variant (when it's a paddle).</summary>
    public struct WaveCell {
        public WaveCellKind Kind;
        public int PaddleIndex; // -1 = auto (intro order then random); 0-3 = paddle bucket index

        public static readonly WaveCell Empty = new WaveCell { Kind = WaveCellKind.Empty, PaddleIndex = -1 };
    }

    /// <summary>
    ///     A hand-authored "wave": a little grid of what flows down the drum, painted as text so anyone
    ///     can make one with no code. Each LINE is one row; the TOP line is farthest away and arrives
    ///     first, the bottom line arrives last. Each CHARACTER in a line is one lane, left to right -
    ///     use one char per lane (the track has 7), and the spawner places each char in that lane.
    ///
    ///     Legend (case-insensitive):
    ///       .  or space  empty (nothing in that lane)
    ///       o            common sock   (Sock Bucket)
    ///       u            uncommon sock (Uncommon Sock Bucket)
    ///       r            rare sock     (Rare Sock Bucket)
    ///       x  or #      obstacle      (Obstacle Bucket)
    ///       p            paddle        (auto: introduces each variant in bucket order, then random)
    ///       0-3          a SPECIFIC paddle by bucket index (0 = first paddle in the bucket)
    ///     Any other character is treated as empty. A blank line is a full row of empty - handy as a
    ///     breather between clusters.
    ///
    ///     Make these via Create > SpinToWin > Spawn Wave, then drop them in a folder a
    ///     <see cref="SpawnWaveBucket" /> watches (intro pool or random pool on the TrackSpawner).
    /// </summary>
    [CreateAssetMenu(menuName = "SpinToWin/Spawn Wave", fileName = "Wave")]
    public class SpawnWave : ScriptableObject {
        /// <summary>Highest paddle index the grid understands ('0'..'3' = four variants).</summary>
        public const int MaxPaddleIndex = 3;

        [Tooltip("Optional designer note - what this wave is for. Not used at runtime.")]
        [SerializeField] private string notes = "";

        [Tooltip("Paint the wave. One line = one row (top arrives first); one char = one lane. " +
                 "Legend: . empty, o/u/r sock tiers, x obstacle, p paddle, 0-3 specific paddle.")]
        [TextArea(4, 16)]
        [SerializeField] private string grid = "";

        [System.NonSerialized] private List<WaveCell[]> _rows;

        /// <summary>Number of rows in the wave.</summary>
        public int RowCount {
            get {
                EnsureParsed();
                return _rows.Count;
            }
        }

        /// <summary>The parsed cells of one row (empty array if the index is out of range).</summary>
        public WaveCell[] Row(int index) {
            EnsureParsed();
            return index >= 0 && index < _rows.Count ? _rows[index] : System.Array.Empty<WaveCell>();
        }

        private void OnValidate() {
            _rows = null; // re-parse after an edit in the inspector
        }

        private void EnsureParsed() {
            if (_rows == null) {
                _rows = Parse(grid);
            }
        }

        /// <summary>Turns the painted text into rows of cells. Public + static so tools/tests can use it.</summary>
        public static List<WaveCell[]> Parse(string grid) {
            List<WaveCell[]> rows = new List<WaveCell[]>();
            if (string.IsNullOrEmpty(grid)) {
                return rows;
            }

            string[] lines = grid.Replace("\r", "").Split('\n');
            foreach (string line in lines) {
                WaveCell[] row = new WaveCell[line.Length];
                for (int i = 0; i < line.Length; i++) {
                    row[i] = CellFor(line[i]);
                }

                rows.Add(row);
            }

            return rows;
        }

        private static WaveCell CellFor(char c) {
            switch (c) {
                case 'o':
                case 'O':
                    return Of(WaveCellKind.CommonSock);
                case 'u':
                case 'U':
                    return Of(WaveCellKind.UncommonSock);
                case 'r':
                case 'R':
                    return Of(WaveCellKind.RareSock);
                case 'x':
                case 'X':
                case '#':
                    return Of(WaveCellKind.Obstacle);
                case 'p':
                case 'P':
                    return Paddle(-1);
                default:
                    if (c >= '0' && c <= '0' + MaxPaddleIndex) {
                        return Paddle(c - '0');
                    }

                    return WaveCell.Empty; // '.', ' ', '_', digits above 3, or anything unrecognised
            }
        }

        private static WaveCell Of(WaveCellKind kind) {
            return new WaveCell { Kind = kind, PaddleIndex = -1 };
        }

        private static WaveCell Paddle(int index) {
            return new WaveCell { Kind = WaveCellKind.Paddle, PaddleIndex = index };
        }
    }
}
