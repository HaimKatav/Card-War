namespace CardWar.Common.States
{
    public enum AppState
    {
        Initializing,
        MainMenu,
        LoadingGame,
        InGame,
        GameOver
    }

    public enum GameState
    {
        WaitingToStart,
        PlayerTurn,
        OpponentTurn,
        ResolvingBattle,
        War,
        CollectingCards,
        CheckingVictory,
        Paused,
        Error
    }
}
