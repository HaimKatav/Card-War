using System;
using CardWar.Core.Context;
using CardWar.Core.States;
using CardWar.Services;

namespace CardWar.Services.State
{
    public interface IGameStateManager : IBaseServiceProvider
    {
        GameState GetCurrentGameState();
        void SetCurrentGameState(GameState state, GameContext context);
        bool CanTransition(GameState from, GameState to, GameContext context);
        string GetInvalidTransitionReason(GameState from, GameState to, GameContext context);
        event Action<StateTransition<GameState>> OnGameStateChanged;
    }
}
