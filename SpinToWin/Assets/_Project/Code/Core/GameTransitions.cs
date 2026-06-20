using System.Collections;
using _Project.Code.UI;
using CoreUtils.GameEvents;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Code.Core {
    /// <summary>
    ///     Sequences the scene change between the laundromat (menu) scene and the
    ///     inside-the-machine (gameplay) scene with a camera dolly + fade:
    ///     entering Playing dollies toward the machine, fades to black, loads the game scene,
    ///     and fades in; entering MainMenu fades back to the laundromat.
    ///     Put this on the persistent [Managers] object with the (also persistent)
    ///     <see cref="ScreenFader" /> assigned.
    ///
    ///     The camera dolly itself is just the menu scene's CameraDirector reacting to
    ///     Playing (its "Playing" viewpoint = the close-up approach), so there is no
    ///     cross-scene reference to manage here — we only wait for that blend.
    /// </summary>
    public class GameTransitions : MonoBehaviour {
        [SerializeField] private string menuScene = "Start";
        [SerializeField] private string gameScene = "Main";

        [Header("Timing")]
        [SerializeField] private float approachTime = 1.0f;

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

        private void HandleStateEntered(string stateName) {
            string activeScene = SceneManager.GetActiveScene().name;

            if (stateName == GameStateNames.Playing && activeScene != gameScene) {
                Run(EnterGame());
            } else if (stateName == GameStateNames.MainMenu && activeScene != menuScene) {
                Run(ReturnToMenu());
            }
        }

        private void Run(IEnumerator routine) {
            if (_running != null) {
                StopCoroutine(_running);
            }

            _running = StartCoroutine(routine);
        }

        private IEnumerator EnterGame() {
            // 1. Let the menu camera dolly toward the washing machine.
            yield return new WaitForSecondsRealtime(approachTime);

            // 2. Fade to black, 3. load the inside-the-machine scene, 4. fade back in.
            yield return FadeOut();
            yield return SceneManager.LoadSceneAsync(gameScene);
            yield return FadeIn();

            _running = null;
        }

        private IEnumerator ReturnToMenu() {
            yield return FadeOut();
            yield return SceneManager.LoadSceneAsync(menuScene);
            yield return FadeIn();

            _running = null;
        }

        private static IEnumerator FadeOut() {
            if (ScreenFader.Instance != null) {
                yield return ScreenFader.Instance.FadeOut();
            }
        }

        private static IEnumerator FadeIn() {
            if (ScreenFader.Instance != null) {
                yield return ScreenFader.Instance.FadeIn();
            }
        }
    }
}
