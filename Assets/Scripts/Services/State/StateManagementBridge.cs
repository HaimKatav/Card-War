using UnityEngine;
using CardWar.Common.States;
using CardWar.Core.Context;
using CardWar.Services;

namespace CardWar.Services.State
{
    public class StateManagementBridge : IGameStateService
    {
        private IAppStateManager _appStateManager;
        private IGameStateManager _gameStateManager;
        private IAppStateManager AppStateManager => _appStateManager ??= ServiceLocator.Get<IAppStateManager>();
        private IGameStateManager GameStateManager => _gameStateManager ??= ServiceLocator.Get<IGameStateManager>();

        public StateManagementBridge()
        {
            ServiceLocator.Instance.Register(typeof(IGameStateService), this);
        }

        public GameState CurrentGameState => GameStateManager.GetCurrentGameState();

        public void ChangeState(GameState newState)
        {
            var from = GameStateManager.GetCurrentGameState();
            var context = GameContext.Create(AppStateManager.GetCurrentAppState(), from);
            if (GameStateManager.CanTransition(from, newState, context))
            {
                var newContext = context.WithGameState(newState);
                GameStateManager.SetCurrentGameState(newState, newContext);
            }
            else
            {
                var reason = GameStateManager.GetInvalidTransitionReason(from, newState, context);
                Debug.LogWarning($"[StateManagementBridge] {reason}");
            }
        }
    }
}
