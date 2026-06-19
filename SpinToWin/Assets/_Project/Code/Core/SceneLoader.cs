using CoreUtils.GameEvents;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Code.Core {
    /// <summary>
    ///     Loads the right scene when a state is entered: <see cref="GameStateNames.MainMenu" />
    ///     → the menu scene, <see cref="GameStateNames.Playing" /> → the gameplay scene.
    ///     Reacts to the StateEntered GameEvent, so it has no direct reference to the state
    ///     machine. Put this on the persistent managers object so it survives the loads it
    ///     triggers, and assign the StateEntered event.
    /// </summary>
    public class SceneLoader : MonoBehaviour {
        // Must match the scene file names AND be added to File > Build Settings.
        public const string MenuScene = "Start";
        public const string GameScene = "Main";

        [SerializeField] private GameEventString stateEntered;

        private void OnEnable() {
            if (stateEntered != null) {
                stateEntered.Event += HandleStateEntered;
            }
        }

        private void OnDisable() {
            if (stateEntered != null) {
                stateEntered.Event -= HandleStateEntered;
            }
        }

        private void HandleStateEntered(string stateName) {
            switch (stateName) {
                case GameStateNames.MainMenu:
                    Load(MenuScene);
                    break;
                case GameStateNames.Playing:
                    // Entering Playing from Paused (resume) is a no-op here because we're
                    // already in the game scene and Load() guards against reloading it.
                    Load(GameScene);
                    break;
            }
        }

        private static void Load(string sceneName) {
            if (SceneManager.GetActiveScene().name != sceneName) {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
