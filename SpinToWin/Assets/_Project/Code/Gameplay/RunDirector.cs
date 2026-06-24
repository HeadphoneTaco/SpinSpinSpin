using _Project.Code.Core;
using CoreUtils.GameEvents;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>How a run is won. Two prototype playstyles, switchable in the Inspector.</summary>
    public enum WinMode {
        /// <summary>Survive until the visible timer reaches Target Time. Socks are score only.</summary>
        SurviveTime,

        /// <summary>Collect Target Socks. A hidden timer tightens spawn spacing as you go.</summary>
        CollectSocks
    }

    /// <summary>The result of the run once it ends.</summary>
    public enum RunOutcome { None, Won, Lost }

    /// <summary>
    ///     The scoreboard-and-throttle for a single run. Scene-scoped (lives in <c>Main</c>): it owns
    ///     the scroll <see cref="Speed" />, the sock count, the elapsed time, the win check, and the
    ///     one place a run ends - as a win or a loss. Both outcomes route through the existing GameOver
    ///     state; <see cref="Outcome" /> tells the HUD which banner to show.
    ///
    ///     Two playstyles via <see cref="WinMode" />:
    ///       SurviveTime - visible countdown; reach Target Time to win; socks are score only.
    ///       CollectSocks - timer hidden; collect Target Socks to win; the timer ramps spawn frequency.
    ///     In both, hitting an obstacle is a loss. Because <c>Main</c> reloads per Play, a run starts
    ///     clean in <see cref="OnEnable" />. Reachable from items via <see cref="Instance" />.
    /// </summary>
    public class RunDirector : MonoBehaviour {
        public static RunDirector Instance { get; private set; }
        [SerializeField] private WinMode winMode = WinMode.SurviveTime;
        [SerializeField] private float targetTime = 60f;
        [SerializeField] private int targetSocks = 25;
        [SerializeField] private float baseSpeed = 10f;
        [SerializeField] private float maxSpeed = 30f;
        [SerializeField] private float acceleration = 0.4f;
        [SerializeField] private float minSpawnSpacingScale = 0.4f;
        [SerializeField] private float spawnRampTime = 60f;

        [Header("Hits / lives")]
        [Tooltip("How many obstacle hits the gremlin can take before the run is lost.")]
        [SerializeField] private int maxHits = 3;
        [Tooltip("Seconds of invincibility after a hit, so one obstacle (or a cluster) can't drain " +
                 "several hits at once. 0 = no grace.")]
        [SerializeField] private float hitGrace = 0.75f;

        [SerializeField] private GameEventInt sockCountChanged;
        [SerializeField] private GameEventInt distanceChanged;
        [Tooltip("Raised with the remaining hit count whenever it changes (for the HUD hearts).")]
        [SerializeField] private GameEventInt hitsChanged;
        private float _distance;
        private int _lastReportedDistance;
        private float _graceUntil;
        public float Speed { get; private set; }
        public int SockCount { get; private set; }
        public int MaxHits => maxHits;
        public int HitsRemaining { get; private set; }
        /// <summary>True during the post-hit grace window (handy for flashing the gremlin).</summary>
        public bool Invincible => Time.time < _graceUntil;
        public int Distance => Mathf.FloorToInt(_distance);
        public float ElapsedTime { get; private set; }
        public RunOutcome Outcome { get; private set; }
        public float SpawnSpacingScale { get; private set; } = 1f;
        public WinMode Mode => winMode;
        public float TargetTime => targetTime;
        public int TargetSocks => targetSocks;
        public bool TimerVisible => winMode == WinMode.SurviveTime;

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
            if (Outcome != RunOutcome.None || !GameManager.Exists || !GameManager.Instance.IsPlaying) {
                return;
            }

            ElapsedTime += Time.deltaTime;
            Speed = Mathf.Min(Speed + acceleration * Time.deltaTime, maxSpeed);
            _distance += Speed * Time.deltaTime;

            SpawnSpacingScale = winMode == WinMode.CollectSocks && spawnRampTime > 0f
                ? Mathf.Lerp(1f, minSpawnSpacingScale, ElapsedTime / spawnRampTime)
                : 1f;

            int metres = Mathf.FloorToInt(_distance);
            if (metres != _lastReportedDistance) {
                _lastReportedDistance = metres;
                if (distanceChanged != null) {
                    distanceChanged.Raise(metres);
                }
            }

            if (winMode == WinMode.SurviveTime && ElapsedTime >= targetTime) {
                EndRun(RunOutcome.Won);
            }
        }

        /// <summary>Wipes the run back to the starting line. Called automatically when the scene loads.</summary>
        public void ResetRun() {
            Speed = baseSpeed;
            SockCount = 0;
            _distance = 0f;
            _lastReportedDistance = 0;
            ElapsedTime = 0f;
            Outcome = RunOutcome.None;
            SpawnSpacingScale = 1f;
            HitsRemaining = maxHits;
            _graceUntil = 0f;

            if (sockCountChanged != null) {
                sockCountChanged.Raise(0);
            }

            if (distanceChanged != null) {
                distanceChanged.Raise(0);
            }

            if (hitsChanged != null) {
                hitsChanged.Raise(HitsRemaining);
            }
        }

        /// <summary>Banks one (or more) socks. In CollectSocks mode, hitting the target wins the run.</summary>
        public void CollectSock(int amount) {
            if (amount <= 0 || Outcome != RunOutcome.None) {
                return;
            }

            SockCount += amount;
            if (sockCountChanged != null) {
                sockCountChanged.Raise(SockCount);
            }

            if (winMode == WinMode.CollectSocks && SockCount >= targetSocks) {
                EndRun(RunOutcome.Won);
            }
        }

        /// <summary>
        ///     The gremlin hit a hazard. Costs one hit; the run is lost only when hits run out.
        ///     Ignored during the post-hit grace window and once the run has already ended.
        /// </summary>
        public void Crash() {
            if (Outcome != RunOutcome.None || Invincible) {
                return;
            }

            HitsRemaining = Mathf.Max(0, HitsRemaining - 1);
            _graceUntil = Time.time + hitGrace;

            if (hitsChanged != null) {
                hitsChanged.Raise(HitsRemaining);
            }

            if (HitsRemaining <= 0) {
                EndRun(RunOutcome.Lost);
            }
        }

        /// <summary>Ends the run once, recording the outcome and dropping into the GameOver state.</summary>
        private void EndRun(RunOutcome outcome) {
            if (Outcome != RunOutcome.None) {
                return;
            }

            Outcome = outcome;
            if (GameManager.Exists) {
                GameManager.Instance.EndGame();
            }
        }
    }
}
