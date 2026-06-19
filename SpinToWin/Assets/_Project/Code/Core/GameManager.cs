using CoreUtils;

namespace _Project.Code.Core {
    /// <summary>
    ///     Single front door for SpinToWin's core systems.
    ///     Built on CoreUtils <see cref="Singleton{T}" />, so it is lazily created on first
    ///     access to <see cref="GameManager.Instance" /> and survives scene loads
    ///     (DontDestroyOnLoad). No GameObject needs to exist in the scene beforehand.
    ///     Sub-managers live as components on this same GameObject and are reached through
    ///     here, e.g. <c>GameManager.Instance.State.StartGame()</c>.
    /// </summary>
    public class GameManager : Singleton<GameManager> {
        /// <summary>Owns the high-level game state machine.</summary>
        public StateManager State { get; private set; }

        /// <summary>Handles music and sound-effect playback.</summary>
        public AudioManager Audio { get; private set; }

        private void Awake() {
            // Attach (or reuse) the sub-managers on this GameObject so they share its
            // lifetime and DontDestroyOnLoad persistence.
            State = this.GetOrAddComponent<StateManager>();
            Audio = this.GetOrAddComponent<AudioManager>();
        }

        // CoreUtils.Singleton uses OnEnable/OnDisable to register the instance,
        // so any override MUST call the base implementation.
        public override void OnEnable() {
            base.OnEnable();
        }
    }
}
