using _Project.Code.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Code.UI {
    /// <summary>
    ///     Settings panel: music and SFX volume sliders wired to the
    ///     <see cref="AudioManager" />, optional live percentage labels, and a high-contrast
    ///     accessibility toggle wired to the <see cref="AccessibilityManager" />.
    ///     Sliders should be configured 0..1 in the inspector (see the menu setup guide).
    /// </summary>
    public class SettingsController : MonoBehaviour {
        [Header("Sliders")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Value labels (optional, show %)")]
        [SerializeField] private TMP_Text musicValueLabel;
        [SerializeField] private TMP_Text sfxValueLabel;

        [Header("Accessibility")]
        [Tooltip("Toggles high-contrast mode. Optional.")]
        [SerializeField] private Toggle highContrastToggle;

        [Header("Buttons")]
        [Tooltip("Closes/hides this panel. Optional.")]
        [SerializeField] private Button closeButton;

        private void OnEnable() {
            AudioManager audioManager = GameManager.Instance.Audio;

            // Reflect current volumes without firing the change callbacks.
            if (musicSlider != null) {
                musicSlider.SetValueWithoutNotify(audioManager.MusicVolume);
                musicSlider.onValueChanged.AddListener(OnMusicChanged);
                UpdateLabel(musicValueLabel, audioManager.MusicVolume);
            }

            if (sfxSlider != null) {
                sfxSlider.SetValueWithoutNotify(audioManager.SfxVolume);
                sfxSlider.onValueChanged.AddListener(OnSfxChanged);
                UpdateLabel(sfxValueLabel, audioManager.SfxVolume);
            }

            if (highContrastToggle != null) {
                highContrastToggle.SetIsOnWithoutNotify(GameManager.Instance.Accessibility.HighContrast);
                highContrastToggle.onValueChanged.AddListener(OnHighContrastChanged);
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

            if (highContrastToggle != null) {
                highContrastToggle.onValueChanged.RemoveListener(OnHighContrastChanged);
            }

            if (closeButton != null) {
                closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void OnMusicChanged(float value) {
            GameManager.Instance.Audio.SetMusicVolume(value);
            UpdateLabel(musicValueLabel, value);
        }

        private void OnSfxChanged(float value) {
            GameManager.Instance.Audio.SetSfxVolume(value);
            UpdateLabel(sfxValueLabel, value);
        }

        private void OnHighContrastChanged(bool isOn) {
            GameManager.Instance.Accessibility.SetHighContrast(isOn);
        }

        private void OnCloseClicked() {
            gameObject.SetActive(false);
        }

        private static void UpdateLabel(TMP_Text label, float normalizedValue) {
            if (label != null) {
                label.text = Mathf.RoundToInt(normalizedValue * 100f) + "%";
            }
        }
    }
}
