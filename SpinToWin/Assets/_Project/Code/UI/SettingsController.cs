using _Project.Code.Core;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Code.UI {
    /// <summary>
    ///     Settings panel: music and SFX volume sliders wired to the
    ///     <see cref="AudioManager" /> behind <see cref="GameManager" />.
    ///     Sliders should be configured 0..1 in the inspector (see the menu setup guide).
    /// </summary>
    public class SettingsController : MonoBehaviour {
        [Header("Sliders")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Buttons")]
        [Tooltip("Closes/hides this panel. Optional.")]
        [SerializeField] private Button closeButton;

        private void OnEnable() {
            AudioManager audioManager = GameManager.Instance.Audio;

            // Reflect current volumes without firing the change callbacks.
            if (musicSlider != null) {
                musicSlider.SetValueWithoutNotify(audioManager.MusicVolume);
                musicSlider.onValueChanged.AddListener(OnMusicChanged);
            }

            if (sfxSlider != null) {
                sfxSlider.SetValueWithoutNotify(audioManager.SfxVolume);
                sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            }

            if (closeButton != null) {
                closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnDisable() {
            if (musicSlider != null) {
                musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            }

            if (sfxSlider != null) {
                sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            }

            if (closeButton != null) {
                closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void OnMusicChanged(float value) {
            GameManager.Instance.Audio.SetMusicVolume(value);
        }

        private void OnSfxChanged(float value) {
            GameManager.Instance.Audio.SetSfxVolume(value);
        }

        private void OnCloseClicked() {
            gameObject.SetActive(false);
        }
    }
}
