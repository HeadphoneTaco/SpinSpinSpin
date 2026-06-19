namespace _Project.Code.Core.Enums {
    /// <summary>
    ///     High-level states the game can be in. Owned and driven by
    ///     <see cref="GameManager" />.
    /// </summary>
    public enum GameState {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }
}
