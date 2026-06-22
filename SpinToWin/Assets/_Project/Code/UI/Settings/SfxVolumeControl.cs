using _Project.Code.Core;
using _Project.Code.Settings;

namespace _Project.Code.UI.Settings {
    /// <summary>Binds its slider to the SFX volume.</summary>
    public sealed class SfxVolumeControl : SliderSettingControl {
        protected override ISetting<float> ResolveSetting() => GameManager.Instance.Audio.SfxVolumeSetting;
    }
}
