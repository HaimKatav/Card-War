using System;
using CardWar.Common;

namespace CardWar.Services
{
    public interface IGameStateService : IBaseServiceProvider
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