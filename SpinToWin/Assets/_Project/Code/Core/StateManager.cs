using System;
using _Project.Code.Core.Enums;
using UnityEngine;

namespace _Project.Code.Core {
    /// <summary>
    ///     Owns the high-level <see cref="GameState" /> machine and nothing else.
    ///     Not a singleton: it is created and owned by <see cref="GameManager" /> and
    ///     accessed via <c>GameManager.Instance.State</c>. Other systems (time control,
    ///     UI, audio) react to <see cref="OnStateChanged" />.
    /// </summary>
    public class StateManager : MonoBehaviour {
        /// <summary>Fired whenever the state changes. Arg order is (previous, current).</summary>
        public event Action<GameState, GameState> OnStateChanged;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public GameState PreviousState { get; private set; } = GameState.MainMenu;

        public bool IsPlaying => CurrentState == GameState.Playing;
        public bool IsPaused => CurrentState == GameState.Paused;

        // --- Convenience transitions ---------------------------------------------

        public void StartGame() {
            SetState(GameState.Playing);
        }

        public void Pause() {
            SetState(GameState.Paused);
        }

        public void Resume() {
            SetState(GameState.Playing);
        }

        public void TogglePause() {
            if (CurrentState == GameState.Playing) {
                Pause();
            } else if (CurrentState == GameState.Paused) {
                Resume();
            }
        }

        public void EndGame() {
            SetState(GameState.GameOver);
        }

        public void ReturnToMenu() {
            SetState(GameState.MainMenu);
        }

        // --- Core state machine --------------------------------------------------

        /// <summary>
        ///     Attempts to move to <paramref name="newState" />. Ignores no-op transitions
        ///     and rejects invalid ones (logged as a warning). Raises
        ///     <see cref="OnStateChanged" /> on success.
        /// </summary>
        /// <returns>True if the state actually changed.</returns>
        public bool SetState(GameState newState) {
            if (newState == CurrentState) {
                return false;
            }

            if (!CanTransition(CurrentState, newState)) {
                Debug.LogWarning($"[StateManager] Invalid transition: {CurrentState} -> {newState}");
                return false;
            }

            PreviousState = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(PreviousState, CurrentState);
            return true;
        }

        /// <summary>Defines which state transitions are allowed.</summary>
        private static bool CanTransition(GameState from, GameState to) {
            switch (from) {
                case GameState.MainMenu:
                    return to == GameState.Playing;
                case GameState.Playing:
                    return to == GameState.Paused || to == GameState.GameOver;
                case GameState.Paused:
                    // Resume, quit to menu, or game ends while paused.
                    return to == GameState.Playing || to == GameState.MainMenu || to == GameState.GameOver;
                case GameState.GameOver:
                    // Restart or return to menu.
                    return to == GameState.Playing || to == GameState.MainMenu;
                default:
                    return false;
            }
        }
    }
}
