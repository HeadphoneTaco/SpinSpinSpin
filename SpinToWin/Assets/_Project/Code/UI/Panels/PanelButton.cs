using _Project.Code.UI.Transitions;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Code.UI.Panels {
    /// <summary>
    ///     A <see cref="Button" /> that shows, hides, or toggles a target panel GameObject.
    ///     One reusable component replaces the ad-hoc "open settings" / "close settings"
    ///     code that used to live inside the menu and settings controllers — those controllers
    ///     no longer carry visibility logic at all (Single Responsibility + reuse).
    ///
    ///     The actual show/hide happens behind the bubble transition: it covers the screen,
    ///     swaps the panel while hidden, then reveals. Untick <see cref="useTransition" /> for an
    ///     instant swap (e.g. a sub-panel where the bubble sweep would be overkill).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PanelButton : MonoBehaviour {
        public enum VisibilityAction {
            Show,
            Hide,
            Toggle
        }

        [SerializeField] private Button button;
        [SerializeField] private GameObject targetPanel;
        [SerializeField] private VisibilityAction action = VisibilityAction.Show;

        [Tooltip("Play the screen transition (bubbles) around the swap. Off = instant.")]
        [SerializeField] private bool useTransition = true;

        protected virtual void Reset() {
            button = GetComponent<Button>();
        }

        private void OnEnable() {
            if (button == null) {
                button = GetComponent<Button>();
            }

            button.onClick.AddListener(Apply);
        }

        private void OnDisable() {
            if (button != null) {
                button.onClick.RemoveListener(Apply);
            }
        }

        private void Apply() {
            if (targetPanel == null) {
                return;
            }

            bool show = action switch {
                VisibilityAction.Show => true,
                VisibilityAction.Hide => false,
                _ => !targetPanel.activeSelf
            };

            if (useTransition && ScreenTransition.Instance != null) {
                // Cover the screen, swap the panel while it's hidden, then reveal.
                ScreenTransition.Instance.Play(() => targetPanel.SetActive(show));
            } else {
                targetPanel.SetActive(show);
            }
        }
    }
}
