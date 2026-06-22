using _Project.Code.Core;
using CoreUtils.GameEvents;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     Drives music/SFX off the game state: menu music in MainMenu, gameplay music in
    ///     Playing, and a stinger on GameOver. Reacts to the StateEntered GameEvent, so it
    ///     stays decoupled from the state machine. Assign the event and the clips, and drop
    ///     one in each scene (menu clip in the menu scene, gameplay clip in the game scene).
    /// </summary>
    public class StateAudio : MonoBehaviour {
        [Header("Music")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameplayMusic;

        [Header("Stingers")]
        [SerializeField] private AudioClip gameOverSfx;

        [Header("Events")]
        [SerializeField] private GameEventString stateEntered;

        private void OnEnable() {
            if (stateEntered != null) {
                stateEntered.Event += ApplyState;
            }

            // Apply audio for whatever state is already active (e.g. after a scene load).
            if (GameManager.Exists) {
                ApplyState(GameManager.Instance.CurrentStateName);
            }
        }

        private void OnDisable() {
            if (stateEntered != null) {
                stateEntered.Event -= ApplyState;
            }
        }

        private void ApplyState(string stateName) {
            AudioManager audioManager = GameManager.Instance.Audio;

            switch (stateName) {
                case GameStateNames.MainMenu:
                    audioManager.PlayMusic(menuMusic);
                    break;
                case GameStateNames.Playing:
                    audioManager.PlayMusic(gameplayMusic);
                    break;
                case GameStateNames.Paused:
                    // Leave music playing; time control handles the freeze separately.
                    break;
                case GameStateNames.GameOver:
                    audioManager.StopMusic();
                    audioManager.PlaySfx(gameOverSfx);
                    break;
            }
        }
    }
}
