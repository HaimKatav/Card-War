using System;

namespace CardWar.Services
{
    public enum GameState
    {
        FirstLoad,
        LoadingGame,
        MainMenu,
        Playing,
        Paused,
        GameEnded,
        ReturnToMenu
    }

    public interface IGameStateService
    {
        GameState CurrentState { get; }
        GameState PreviousState { get; }
        
        event Action<GameState, GameState> OnGameStateChanged;
        event Action<float> OnLoadingProgress;
        event Action OnClientStartupComplete;
        
        void ChangeState(GameState newState);
        void UpdateLoadingProgress(float progress);
        void NotifyStartupComplete();
    }
}