using _Project.Code.Core;
using CoreUtils.GameEvents;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     The scoreboard-and-throttle for a single run. Scene-scoped (lives in <c>Main</c>, not a
    ///     persistent manager): it owns the current scroll <see cref="Speed" />, the sock count, and
    ///     the distance travelled, and it is the one place a crash turns into a game over.
    ///
    ///     Speed ramps from <see cref="baseSpeed" /> toward <see cref="maxSpeed" /> the longer you
    ///     survive, but only while the game is actually <see cref="GameManager.IsPlaying" /> — so it
    ///     naturally stalls during Pause without any time-scale trickery. The drum's spin is purely
    ///     cosmetic for now; this director is where a "spin = speed" link would later hook in.
    ///
    ///     Because <c>Main</c> reloads on every fresh Play, a new run starts clean in
    ///     <see cref="OnEnable" />. Reachable from items via <see cref="Instance" />.
    /// </summary>
    public class RunDirector : MonoBehaviour {
        /// <summary>The active director in the loaded gameplay scene, or null outside <c>Main</c>.</summary>
        public static RunDirector Instance { get; private set; }
        
        [SerializeField] private float baseSpeed = 10f;
        [SerializeField] private float maxSpeed = 30f;
        [SerializeField] private float acceleration = 0.4f;
        [SerializeField] private GameEventInt sockCountChanged;
        [SerializeField] private GameEventInt distanceChanged;

        private float _distance;
        private int _lastReportedDistance;

        /// <summary>Current scroll speed every <see cref="ScrollingItem" /> reads to move toward the gremlin.</summary>
        public float Speed { get; private set; }

        /// <summary>Socks banked this run.</summary>
        public int SockCount { get; private set; }

        /// <summary>Whole world-units travelled this run (rounded down).</summary>
        public int Distance => Mathf.FloorToInt(_distance);

        private void OnEnable() {
            Instance = this;
            ResetRun();
        }

        private void OnDisable() {
            if (Instance == this) {
                Instance = null;
            }
        }

        private void Update() {
            // Only advance while truly playing; Paused / GameOver hold everything in place.
            if (!GameManager.Exists || !GameManager.Instance.IsPlaying) {
                return;
            }

            Speed = Mathf.Min(Speed + acceleration * Time.deltaTime, maxSpeed);
            _distance += Speed * Time.deltaTime;

            int metres = Mathf.FloorToInt(_distance);
            if (metres != _lastReportedDistance) {
                _lastReportedDistance = metres;
                if (distanceChanged != null) {
                    distanceChanged.Raise(metres);
                }
            }
        }

        /// <summary>Wipes the run back to the starting line. Called automatically when the scene loads.</summary>
        public void ResetRun() {
            Speed = baseSpeed;
            SockCount = 0;
            _distance = 0f;
            _lastReportedDistance = 0;

            if (sockCountChanged != null) {
                sockCountChanged.Raise(0);
            }

            if (distanceChanged != null) {
                distanceChanged.Raise(0);
            }
        }

        /// <summary>Banks one (or more) socks and broadcasts the new total.</summary>
        public void CollectSock(int amount) {
            if (amount <= 0) {
                return;
            }

            SockCount += amount;
            if (sockCountChanged != null) {
                sockCountChanged.Raise(SockCount);
            }
        }

        /// <summary>The gremlin hit a hazard: end the run through the shared game-over transition.</summary>
        public void Crash() {
            if (GameManager.Exists) {
                GameManager.Instance.EndGame();
            }
        }
    }
}
