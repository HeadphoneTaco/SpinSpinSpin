using _Project.Code.Core;
using CoreUtils.GameEvents;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Code.Utilities {
    /// <summary>
    ///     Dev-only helper for driving the game state from the keyboard so the flow can be
    ///     tested without building out the menus. Also logs each state entered.
    ///     Remove (or disable) before shipping.
    ///     Keys: 1 = Start, 2 = Toggle Pause, 3 = End Game, 0 = Main Menu.
    /// </summary>
    public class DebugStateControls : MonoBehaviour {
        [SerializeField] private GameEventString stateEntered;

        private void OnEnable() {
            if (stateEntered != null) {
                stateEntered.Event += LogStateEntered;
            }
        }

        private void OnDisable() {
            if (stateEntered != null) {
                stateEntered.Event -= LogStateEntered;
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

            GameManager game = GameManager.Instance;

            if (keyboard.digit1Key.wasPressedThisFrame) {
                game.StartGame();
            } else if (keyboard.digit2Key.wasPressedThisFrame) {
                game.TogglePause();
            } else if (keyboard.digit3Key.wasPressedThisFrame) {
                game.EndGame();
            } else if (keyboard.digit0Key.wasPressedThisFrame) {
                game.ReturnToMenu();
            }
        }

        private static void LogStateEntered(string stateName) {
            Debug.Log($"[State] entered {stateName}");
        }
    }
}
