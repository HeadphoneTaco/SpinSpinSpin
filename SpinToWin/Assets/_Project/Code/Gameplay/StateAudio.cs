using _Project.Code.Core;
using _Project.Code.Core.Enums;
using UnityEngine;

namespace _Project.Code.Gameplay {
    /// <summary>
    ///     Drives music/SFX off the game state: menu music in MainMenu, gameplay music in
    ///     Playing, and a stinger on GameOver. Reacts to <see cref="StateManager.OnStateChanged" />
    ///     so it stays decoupled from whatever triggers the transitions. Drop it on any
    ///     persistent object and assign clips in the inspector.
    /// </summary>
    public class StateAudio : MonoBehaviour {
        [Header("Music")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameplayMusic;

        [Header("Stingers")]
        [SerializeField] private AudioClip gameOverSfx;

        private void OnEnable() {
            StateManager state = GameManager.Instance.State;
            state.OnStateChanged += HandleStateChanged;
            // Apply audio for whatever state we start in.
            ApplyState(state.CurrentState);
        }

        private void OnDisable() {
            if (GameManager.Exists) {
                GameManager.Instance.State.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState previous, GameState current) {
            ApplyState(current);
        }

        private void ApplyState(GameState state) {
            AudioManager audioManager = GameManager.Instance.Audio;

            switch (state) {
                case GameState.MainMenu:
                    audioManager.PlayMusic(menuMusic);
                    break;
                case GameState.Playing:
                    audioManager.PlayMusic(gameplayMusic);
                    break;
                case GameState.Paused:
                    // Leave music playing; time control handles the freeze separately.
                    break;
                case GameState.GameOver:
                    audioManager.StopMusic();
                    audioManager.PlaySfx(gameOverSfx);
                    break;
            }
        }
    }
}
