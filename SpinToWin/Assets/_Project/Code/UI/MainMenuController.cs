using _Project.Code.Core;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Code.UI {
    /// <summary>
    ///     Drives the main menu: Play, Settings and Quit. Pure UI wiring — game logic
    ///     lives behind <see cref="GameManager" />. Assign the button references in the
    ///     inspector (see the menu setup guide).
    /// </summary>
    public class MainMenuController : MonoBehaviour {
        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Panels")]
        [Tooltip("Settings panel toggled open by the Settings button. Optional.")]
        [SerializeField] private GameObject settingsPanel;

        private void OnEnable() {
            if (playButton != null) {
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (settingsButton != null) {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (quitButton != null) {
                quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        private void OnDisable() {
            if (playButton != null) {
                playButton.onClick.RemoveListener(OnPlayClicked);
            }

            if (settingsButton != null) {
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (quitButton != null) {
                quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }

        private void OnPlayClicked() {
            GameManager.Instance.State.StartGame();
        }

        private void OnSettingsClicked() {
            if (settingsPanel != null) {
                settingsPanel.SetActive(true);
            }
        }

        private void OnQuitClicked() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
