using _Project.Code.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Code.UI.Settings {
    /// <summary>
    ///     Binds the <see cref="Toggle" /> on this object to a bool <see cref="ISetting{T}" />.
    ///     Same shape as <see cref="SliderSettingControl" />: this base owns the toggle
    ///     plumbing, subclasses only name the setting (Open/Closed + Dependency Inversion).
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public abstract class ToggleSettingControl : MonoBehaviour {
        [SerializeField] private Toggle toggle;

        private ISetting<bool> _setting;

        /// <summary>Return the setting this toggle edits (e.g. high-contrast mode).</summary>
        protected abstract ISetting<bool> ResolveSetting();

        protected virtual void Reset() {
            toggle = GetComponent<Toggle>();
        }

        private void OnEnable() {
            if (toggle == null) {
                toggle = GetComponent<Toggle>();
            }

            _setting = ResolveSetting();
            if (_setting == null) {
                return;
            }

            toggle.SetIsOnWithoutNotify(_setting.Value);
            toggle.onValueChanged.AddListener(HandleChanged);
        }

        private void OnDisable() {
            if (toggle != null) {
                toggle.onValueChanged.RemoveListener(HandleChanged);
            }
        }

        private void HandleChanged(bool isOn) => _setting.SetValue(isOn);
    }
}
