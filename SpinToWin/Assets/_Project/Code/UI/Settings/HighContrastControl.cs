using _Project.Code.Core;
using _Project.Code.Settings;

namespace _Project.Code.UI.Settings {
    /// <summary>Binds its toggle to high-contrast mode.</summary>
    public sealed class HighContrastControl : ToggleSettingControl {
        protected override ISetting<bool> ResolveSetting() => GameManager.Instance.Accessibility.HighContrastSetting;
    }
}
