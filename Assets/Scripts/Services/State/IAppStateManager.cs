using System;
using CardWar.Core.Context;
using CardWar.Core.States;
using CardWar.Services;

namespace CardWar.Services.State
{
    public interface IAppStateManager : IBaseServiceProvider
    {
        AppState GetCurrentAppState();
        void SetCurrentAppState(AppState state, GameContext context);
        bool CanTransition(AppState from, AppState to, GameContext context);
        string GetInvalidTransitionReason(AppState from, AppState to, GameContext context);
        event Action<StateTransition<AppState>> OnAppStateChanged;
    }
}
