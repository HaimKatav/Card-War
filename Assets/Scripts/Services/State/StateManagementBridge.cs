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

        public GameState CurrentState => _gameStateManager?.GetCurrentGameState() ?? GameState.WaitingToStart;

        public event Action<GameState> GameStateChanged;

        private async void Start()
        {
            _gameStateManager = await ServiceLocator.Get<IGameStateManager>();
            _appStateManager = await ServiceLocator.Get<IAppStateManager>();
            _gameStateManager.OnGameStateChanged += HandleStateChange;
        }

        private void HandleStateChange(StateTransition<GameState> transition)
        {
            GameStateChanged?.Invoke(transition.To);
        }

        public void ChangeState(GameState newState)
        {
            if (_gameStateManager == null || _appStateManager == null)
                return;
            var from = _gameStateManager.GetCurrentGameState();
            var context = GameContext.Create(_appStateManager.GetCurrentAppState(), from);
            if (_gameStateManager.CanTransition(from, newState, context))
            {
                var newContext = context.WithGameState(newState);
                _gameStateManager.SetCurrentGameState(newState, newContext);
            }
            else
            {
                var reason = _gameStateManager.GetInvalidTransitionReason(from, newState, context);
                Debug.LogWarning($"[{GetType().Name}] {reason}");
            }
        }

        protected override void OnDestroy()
        {
            if (_gameStateManager != null)
                _gameStateManager.OnGameStateChanged -= HandleStateChange;
            base.OnDestroy();
        }
    }
}
