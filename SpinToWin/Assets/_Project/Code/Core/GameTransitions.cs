using System.Collections;
using _Project.Code.UI.Transitions;
using CoreUtils.GameEvents;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Code.Core {
    /// <summary>
    ///     Drives scene changes off the game state: each state that lives in its own scene maps to
    ///     a scene name, and entering that state covers the screen with the bubble transition,
    ///     loads the scene while hidden, then reveals. MainMenu → Start, Settings → Settings,
    ///     Playing → Main. States that are just in-scene overlays (Paused, GameOver) map to nothing
    ///     and don't trigger a load. Put this on the persistent [Managers] object; the (also
    ///     persistent) <see cref="ScreenTransition" /> is found via its singleton.
    ///
    ///     The camera no longer moves between framings — each scene is a single fixed shot and
    ///     the bubbles cover the swap, so there's nothing here to wait on but the transition.
    /// </summary>
    public class GameTransitions : MonoBehaviour {
        [Header("State → scene names")]
        [SerializeField] private string menuScene = "Start";
        [SerializeField] private string settingsScene = "Settings";
        [SerializeField] private string gameScene = "Main";

        [Header("Events")]
        [SerializeField] private GameEventString stateEntered;

        private Coroutine _running;

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

        /// <summary>The scene a state lives in, or null if the state is just an in-scene overlay.</summary>
        private string SceneForState(string stateName) {
            return stateName switch {
                GameStateNames.MainMenu => menuScene,
                GameStateNames.Settings => settingsScene,
                GameStateNames.Playing => gameScene,
                _ => null
            };
        }

        private void HandleStateEntered(string stateName) {
            string targetScene = SceneForState(stateName);
            if (string.IsNullOrEmpty(targetScene)) {
                return;
            }

            if (SceneManager.GetActiveScene().name == targetScene) {
                return;
            }

            Run(LoadScene(targetScene));
        }

        private void Run(IEnumerator routine) {
            if (_running != null) {
                StopCoroutine(_running);
            }

            _running = StartCoroutine(routine);
        }

        private IEnumerator LoadScene(string sceneName) {
            // Cover with bubbles, load the target scene while hidden, then reveal.
            yield return Cover();
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return Reveal();

            _running = null;
        }

        private static IEnumerator Cover() {
            if (ScreenTransition.Instance != null) {
                yield return ScreenTransition.Instance.Cover();
            }
        }

        private static IEnumerator Reveal() {
            if (ScreenTransition.Instance != null) {
                yield return ScreenTransition.Instance.Reveal();
            }
        }
    }
}
