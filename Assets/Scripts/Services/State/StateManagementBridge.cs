using CardWar.Common.States;
using CardWar.Services;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using CardWar.Core.Context;

namespace CardWar.Services.State
{
    public class StateManagementBridge : BaseService, IGameStateService
    {
        private IAppStateManager _appStateManager;
        private IGameStateManager _gameStateManager;

        private async UniTask<IAppStateManager> GetAppStateManager()
        {
            if (_appStateManager == null)
                _appStateManager = await ServiceLocator.Get<IAppStateManager>();
            return _appStateManager;
        }

        private async UniTask<IGameStateManager> GetGameStateManager()
        {
            if (_gameStateManager == null)
                _gameStateManager = await ServiceLocator.Get<IGameStateManager>();
            return _gameStateManager;
        }

        public GameState CurrentState => GetGameStateManager().GetAwaiter().GetResult().GetCurrentGameState();

        public event Action<GameState> GameStateChanged;

        protected override async void Awake()
        {
            base.Awake();
            var manager = await GetGameStateManager();
            manager.OnGameStateChanged += t => GameStateChanged?.Invoke(t.To);
        }

        public async void ChangeState(GameState newState)
        {
            var gameStateManager = await GetGameStateManager();
            var appStateManager = await GetAppStateManager();
            var from = gameStateManager.GetCurrentGameState();
            var context = GameContext.Create(appStateManager.GetCurrentAppState(), from);
            if (gameStateManager.CanTransition(from, newState, context))
            {
                var newContext = context.WithGameState(newState);
                gameStateManager.SetCurrentGameState(newState, newContext);
            }
            else
            {
                var reason = gameStateManager.GetInvalidTransitionReason(from, newState, context);
                Debug.LogWarning($"[StateManagementBridge] {reason}");
            }
        }
    }
}
