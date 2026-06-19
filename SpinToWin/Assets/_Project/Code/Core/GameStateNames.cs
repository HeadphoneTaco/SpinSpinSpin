namespace _Project.Code.Core {
    /// <summary>
    ///     Canonical names for the game states. These MUST match the names of the child
    ///     GameObjects under the CoreUtils StateMachine, since transitions and the
    ///     StateEntered/StateExited events identify states by name.
    /// </summary>
    public static class GameStateNames {
        public const string MainMenu = "MainMenu";
        public const string Playing = "Playing";
        public const string Paused = "Paused";
        public const string GameOver = "GameOver";
    }
}
