using _Project.Code.Core;
using _Project.Code.Core.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Code.Utilities {
    /// <summary>
    ///     Dev-only helper for driving the game state from the keyboard so the flow can be
    ///     tested without building out the menus. Also logs every state transition.
    ///     Remove (or disable) before shipping.
    ///     Keys: 1 = Start, 2 = Toggle Pause, 3 = End Game, 0 = Main Menu.
    /// </summary>
    public class DebugStateControls : MonoBehaviour {
        private void OnEnable() {
            GameManager.Instance.State.OnStateChanged += LogStateChange;
        }

        private void OnDisable() {
            if (GameManager.Exists) {
                GameManager.Instance.State.OnStateChanged -= LogStateChange;
            }
        }

        private void Update() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) {
                return;
            }

            // Bail out during play-mode teardown, when the singleton is already gone.
            if (!GameManager.Exists) {
                return;
            }

            StateManager state = GameManager.Instance.State;

            if (keyboard.digit1Key.wasPressedThisFrame) {
                state.StartGame();
            } else if (keyboard.digit2Key.wasPressedThisFrame) {
                state.TogglePause();
            } else if (keyboard.digit3Key.wasPressedThisFrame) {
                state.EndGame();
            } else if (keyboard.digit0Key.wasPressedThisFrame) {
                state.ReturnToMenu();
            }
        }

        private static void LogStateChange(GameState previous, GameState current) {
            Debug.Log($"[State] {previous} -> {current}");
        }
    }
}
