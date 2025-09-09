using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Core.Context;
using CardWar.Common.States;
using CardWar.Services;

namespace CardWar.Services.State
{
    public class AppStateManager : BaseService, IAppStateManager
    {
        private AppState _currentState;
        private Dictionary<(AppState, AppState), Func<GameContext, bool>> _transitionRules;

        protected override void Awake()
        {
            base.Awake();
            _currentState = AppState.Initializing;
            _transitionRules = InitializeTransitionRules();
        }

        public AppState GetCurrentAppState()
        {
            return _currentState;
        }

        public void SetCurrentAppState(AppState state, GameContext context)
        {
            var oldState = _currentState;
            _currentState = state;
            OnAppStateChanged?.Invoke(new StateTransition<AppState>(oldState, state, context));
            Debug.Log($"[{GetType().Name}] State changed: {oldState} -> {state}");
        }

        public bool CanTransition(AppState from, AppState to, GameContext context)
        {
            if (context == null)
                return false;
            if (context.IsAnimating)
                return false;
            var key = (from, to);
            if (!_transitionRules.ContainsKey(key))
                return false;
            return _transitionRules[key](context);
        }

        public string GetInvalidTransitionReason(AppState from, AppState to, GameContext context)
        {
            if (context == null)
                return "Context is null";
            if (context.IsAnimating)
                return "Animation in progress";
            if (!_transitionRules.ContainsKey((from, to)))
                return $"No transition defined from {from} to {to}";
            return "Transition blocked by rules";
        }

        public event Action<StateTransition<AppState>> OnAppStateChanged;

        private Dictionary<(AppState, AppState), Func<GameContext, bool>> InitializeTransitionRules()
        {
            return new Dictionary<(AppState, AppState), Func<GameContext, bool>>
            {
                { (AppState.Initializing, AppState.MainMenu), Allow },
                { (AppState.MainMenu, AppState.LoadingGame), Allow },
                { (AppState.LoadingGame, AppState.InGame), Allow },
                { (AppState.InGame, AppState.GameOver), Allow }
            };
        }

        protected override void OnDestroy()
        {
            OnAppStateChanged = null;
            _transitionRules?.Clear();
            base.OnDestroy();
        }

        private bool Allow(GameContext context)
        {
            return true;
        }
    }
}
