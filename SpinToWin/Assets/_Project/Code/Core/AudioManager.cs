using UnityEngine;

namespace _Project.Code.Core {
    /// <summary>
    ///     Simple audio playback skeleton: one looping music channel and one
    ///     channel for fire-and-forget sound effects. Not a singleton: created and owned
    ///     by <see cref="GameManager" /> and accessed via <c>GameManager.Instance.Audio</c>.
    ///     This is intentionally minimal; expand with mixer groups, fades, pooling, etc.
    /// </summary>
    public class AudioManager : MonoBehaviour {
        [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        private void Awake() {
            // Created at runtime since this component is added programmatically by
            // GameManager (no prefab/inspector setup required).
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.volume = musicVolume;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.volume = sfxVolume;
        }

        // --- Music ---------------------------------------------------------------

        /// <summary>Plays a looping music track. Ignores the call if the same clip is already playing.</summary>
        public void PlayMusic(AudioClip clip) {
            if (clip == null) {
                return;
            }

            if (_musicSource.isPlaying && _musicSource.clip == clip) {
                return;
            }

            _musicSource.clip = clip;
            _musicSource.volume = musicVolume;
            _musicSource.Play();
        }

        public void StopMusic() {
            _musicSource.Stop();
        }

        // --- SFX -----------------------------------------------------------------

        /// <summary>Plays a one-shot sound effect over the top of anything already playing.</summary>
        public void PlaySfx(AudioClip clip) {
            if (clip == null) {
                return;
            }

            _sfxSource.PlayOneShot(clip, sfxVolume);
        }

        // --- Volume --------------------------------------------------------------

        public void SetMusicVolume(float volume) {
            musicVolume = Mathf.Clamp01(volume);
            _musicSource.volume = musicVolume;
        }

        public void SetSfxVolume(float volume) {
            sfxVolume = Mathf.Clamp01(volume);
            _sfxSource.volume = sfxVolume;
        }
    }
}
