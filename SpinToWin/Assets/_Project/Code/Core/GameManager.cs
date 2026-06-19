using UnityEngine;

namespace _Project.Code.Core {
    /// <summary>
    ///     Single front door for SpinToWin's core systems. A plain MonoBehaviour singleton
    ///     (see <see cref="Singleton{T}" />): created on first access to
    ///     <see cref="Singleton{T}.Instance" /> and kept across scene loads. No GameObject
    ///     needs to exist in the scene beforehand.
    ///     Sub-managers live as components on this same GameObject and are reached through
    ///     here, e.g. <c>GameManager.Instance.State.StartGame()</c>.
    /// </summary>
    public class GameManager : Singleton<GameManager> {
        private StateManager _state;
        private AudioManager _audio;

        /// <summary>
        ///     Owns the high-level game state machine. Fetched (or added) on first access so
        ///     it is always ready, regardless of script initialization order.
        /// </summary>
        public StateManager State => GetOrAdd(ref _state);

        /// <summary>Handles music and sound-effect playback. Lazily created, as with <see cref="State" />.</summary>
        public AudioManager Audio => GetOrAdd(ref _audio);

        /// <summary>Returns the cached sub-manager, fetching or adding the component on demand.</summary>
        private TManager GetOrAdd<TManager>(ref TManager field) where TManager : Component {
            if (field == null) {
                field = GetComponent<TManager>();
                if (field == null) {
                    field = gameObject.AddComponent<TManager>();
                }
            }

            return field;
        }
    }
}
