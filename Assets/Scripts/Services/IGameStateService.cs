using CardWar.Common.States;
using System;

namespace CardWar.Services
{
    public interface IGameStateService : IBaseServiceProvider
    {
        GameState CurrentState { get; }
        void ChangeState(GameState newState);
        event Action<GameState> GameStateChanged;
    }
}
