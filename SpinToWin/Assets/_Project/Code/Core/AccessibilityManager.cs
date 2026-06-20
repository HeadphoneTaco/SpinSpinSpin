using CoreUtils.GameEvents;
using UnityEngine;

namespace _Project.Code.Core {
    /// <summary>
    ///     Holds accessibility preferences (currently just high-contrast mode), persists them
    ///     with PlayerPrefs, and broadcasts changes through a GameEventBool so UI/visual
    ///     systems can react without a direct reference. Owned by <see cref="GameManager" />
    ///     and reached via <c>GameManager.Instance.Accessibility</c>.
    ///     The actual visual swap (per-element, palette, or effect) subscribes to
    ///     <see cref="highContrastChanged" /> — that layer is intentionally not decided here.
    /// </summary>
    public class AccessibilityManager : MonoBehaviour {
        private const string HighContrastKey = "accessibility.highContrast";

        [Tooltip("Raised whenever high-contrast mode changes (payload = enabled).")]
        [SerializeField] private GameEventBool highContrastChanged;

        private bool _loaded;

        /// <summary>Whether high-contrast mode is currently enabled.</summary>
        public bool HighContrast { get; private set; }

        private void Awake() {
            Load();
        }

        private void Load() {
            if (_loaded) {
                return;
            }

            _loaded = true;
            HighContrast = PlayerPrefs.GetInt(HighContrastKey, 0) == 1;
        }

        /// <summary>Sets high-contrast mode, persists it, and broadcasts the change.</summary>
        public void SetHighContrast(bool enabled) {
            Load();

            if (HighContrast == enabled) {
                return;
            }

            HighContrast = enabled;
            PlayerPrefs.SetInt(HighContrastKey, enabled ? 1 : 0);
            PlayerPrefs.Save();

            if (highContrastChanged != null) {
                highContrastChanged.Raise(enabled);
            }
        }

        public void Toggle() {
            SetHighContrast(!HighContrast);
        }
    }
}
