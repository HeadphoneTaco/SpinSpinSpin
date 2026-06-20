using _Project.Code.Core;
using CoreUtils.GameEvents;
using UnityEngine;

namespace _Project.Code.Cameras {
    /// <summary>
    ///     Diegetic camera framing: activates one Cinemachine viewpoint at a time so the
    ///     CinemachineBrain blends between framings as the game state changes (wide
    ///     laundromat for the menu, the washing machine for play, the controls for pause,
    ///     a detergent bottle for settings).
    ///
    ///     This script is Cinemachine-version-agnostic: it just enables/disables the
    ///     viewpoint GameObjects (each holding a CinemachineCamera). The Brain does the
    ///     blending. Assign each viewpoint and the two events.
    /// </summary>
    public class CameraDirector : MonoBehaviour {
        [Header("Viewpoints (objects with a CinemachineCamera)")]
        [SerializeField] private GameObject mainMenuView;
        [SerializeField] private GameObject settingsView;
        [SerializeField] private GameObject playingView;
        [SerializeField] private GameObject pausedView;
        [Tooltip("Optional; falls back to the playing view if left empty.")]
        [SerializeField] private GameObject gameOverView;

        [Header("Events")]
        [SerializeField] private GameEventString stateEntered;
        [Tooltip("Raised true while the settings panel is open (look at the detergent).")]
        [SerializeField] private GameEventBool settingsViewActive;

        // The viewpoint for the underlying game state, restored when settings closes.
        private GameObject _stateView;

        private void OnEnable() {
            if (stateEntered != null) {
                stateEntered.Event += HandleStateEntered;
            }

            if (settingsViewActive != null) {
                settingsViewActive.Event += HandleSettingsView;
            }

            // Frame whatever state is already active when we start.
            if (GameManager.Exists) {
                HandleStateEntered(GameManager.Instance.CurrentStateName);
            }
        }

        private void OnDisable() {
            if (stateEntered != null) {
                stateEntered.Event -= HandleStateEntered;
            }

            if (settingsViewActive != null) {
                settingsViewActive.Event -= HandleSettingsView;
            }
        }

        private void HandleStateEntered(string stateName) {
            GameObject view = ViewFor(stateName);
            if (view != null) {
                _stateView = view;
                Activate(view);
            }
        }

        private void HandleSettingsView(bool open) {
            if (open) {
                Activate(settingsView);
            } else if (_stateView != null) {
                // Settings closed — return to the current state's framing.
                Activate(_stateView);
            }
        }

        private GameObject ViewFor(string stateName) {
            switch (stateName) {
                case GameStateNames.MainMenu:
                    return mainMenuView;
                case GameStateNames.Playing:
                    return playingView;
                case GameStateNames.Paused:
                    return pausedView;
                case GameStateNames.GameOver:
                    return gameOverView != null ? gameOverView : playingView;
                default:
                    return null;
            }
        }

        private void Activate(GameObject target) {
            if (target == null) {
                return;
            }

            // Enable only the target; the Brain blends from the previously live camera.
            SetActive(mainMenuView, target);
            SetActive(settingsView, target);
            SetActive(playingView, target);
            SetActive(pausedView, target);
            SetActive(gameOverView, target);
        }

        private static void SetActive(GameObject view, GameObject target) {
            if (view != null) {
                view.SetActive(view == target);
            }
        }
    }
}
