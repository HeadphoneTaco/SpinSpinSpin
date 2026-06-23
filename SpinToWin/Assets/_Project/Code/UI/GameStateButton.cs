using _Project.Code.Core;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Code.UI {
    /// <summary>
    ///     A <see cref="Button" /> that asks <see cref="GameManager" /> for one transition. One
    ///     button = one component = one action, so menu/nav buttons across every scene (Play,
    ///     Settings, Back-to-menu, Pause, Resume, Quit) all use this — no per-scene controller
    ///     classes. Adding a new action is a new enum case, not a new script (Open/Closed).
    ///
    ///     Scene changes happen because the requested state maps to a scene in
    ///     <see cref="GameTransitions" />, which plays the bubble transition. This button doesn't
    ///     touch scenes itself — it only changes state.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class GameStateButton : MonoBehaviour {
        public enum Action {
            Play,
            Settings,
            MainMenu,
            Pause,
            Resume,
            Restart,
            Quit
        }

        [SerializeField] private Button button;
        [SerializeField] private Action action = Action.Play;

        protected virtual void Reset() {
            button = GetComponent<Button>();
        }

        private void OnEnable() {
            if (button == null) {
                button = GetComponent<Button>();
            }

            button.onClick.AddListener(Invoke);
        }

        private void OnDisable() {
            if (button != null) {
                button.onClick.RemoveListener(Invoke);
            }
        }

        private void Invoke() {
            switch (action) {
                case Action.Play:
                    GameManager.Instance.StartGame();
                    break;
                case Action.Settings:
                    GameManager.Instance.OpenSettings();
                    break;
                case Action.MainMenu:
                    GameManager.Instance.ReturnToMenu();
                    break;
                case Action.Pause:
                    GameManager.Instance.Pause();
                    break;
                case Action.Resume:
                    GameManager.Instance.Resume();
                    break;
                case Action.Restart:
                    GameManager.Instance.RestartGame();
                    break;
                case Action.Quit:
                    Quit();
                    break;
            }
        }

        private static void Quit() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
