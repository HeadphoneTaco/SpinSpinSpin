using _Project.Code.Core;
using _Project.Code.Settings;

namespace _Project.Code.UI.Settings {
    /// <summary>Binds its slider to the music volume. The only thing it knows is where the setting lives.</summary>
    public sealed class MusicVolumeControl : SliderSettingControl {
        protected override ISetting<float> ResolveSetting() => GameManager.Instance.Audio.MusicVolumeSetting;
    }
}
