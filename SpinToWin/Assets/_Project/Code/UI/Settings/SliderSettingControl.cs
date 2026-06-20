using _Project.Code.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Code.UI.Settings {
    /// <summary>
    ///     Binds the <see cref="Slider" /> on this object (and an optional % label) to a
    ///     float <see cref="ISetting{T}" />. This base class owns the slider plumbing; a
    ///     subclass only says <em>which</em> setting by overriding <see cref="ResolveSetting" />.
    ///     Adding a new slider-backed preference is therefore a new ~3-line subclass, not an
    ///     edit here (Open/Closed). The control depends on <see cref="ISetting{T}" />, never on
    ///     a concrete manager (Dependency Inversion).
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public abstract class SliderSettingControl : MonoBehaviour {
        [SerializeField] private Slider slider;

        [Tooltip("Optional label that shows the value as a percentage (e.g. 80%).")]
        [SerializeField] private TMP_Text valueLabel;

        private ISetting<float> _setting;

        /// <summary>Return the setting this slider edits (e.g. the music or SFX volume).</summary>
        protected abstract ISetting<float> ResolveSetting();

        // Auto-fills the slider reference when the component is first added in the editor.
        protected virtual void Reset() {
            slider = GetComponent<Slider>();
        }

        private void OnEnable() {
            if (slider == null) {
                slider = GetComponent<Slider>();
            }

            _setting = ResolveSetting();
            if (_setting == null) {
                return;
            }

            // Reflect the current value without firing the change callback.
            slider.SetValueWithoutNotify(_setting.Value);
            UpdateLabel(_setting.Value);
            slider.onValueChanged.AddListener(HandleChanged);
        }

        private void OnDisable() {
            if (slider != null) {
                slider.onValueChanged.RemoveListener(HandleChanged);
            }
        }

        private void HandleChanged(float value) {
            _setting.SetValue(value);
            UpdateLabel(value);
        }

        private void UpdateLabel(float normalizedValue) {
            if (valueLabel != null) {
                valueLabel.text = Mathf.RoundToInt(normalizedValue * 100f) + "%";
            }
        }
    }
}
