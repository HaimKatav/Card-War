using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Core.Context;
using CardWar.Common.States;
using CardWar.Services;
using CardWar.Services.State;
using Cysharp.Threading.Tasks;

namespace CardWar.Services.State
{
    public class GameStateManager : BaseService, IGameStateManager
    {
        private GameState _currentState;
        private Dictionary<(GameState, GameState), Func<GameContext, bool>> _transitionRules;
        private IAppStateManager _appStateManager;

        protected override void Awake()
        {
            base.Awake();
            _currentState = GameState.WaitingToStart;
            _transitionRules = InitializeTransitionRules();
        }

        private async void Start()
        {
            _appStateManager = await ServiceLocator.Get<IAppStateManager>();
        }

        public GameState GetCurrentGameState()
        {
            return _currentState;
        }

        public void SetCurrentGameState(GameState state, GameContext context)
        {
            var oldState = _currentState;
            _currentState = state;
            OnGameStateChanged?.Invoke(new StateTransition<GameState>(oldState, state, context));
            Debug.Log($"[{GetType().Name}] State changed: {oldState} -> {state}");
        }

        public bool CanTransition(GameState from, GameState to, GameContext context)
        {
            if (context == null)
                return false;
            if (context.IsAnimating)
                return false;
            if (_appStateManager?.GetCurrentAppState() != AppState.InGame)
                return false;
            var key = (from, to);
            if (!_transitionRules.ContainsKey(key))
                return false;
            return _transitionRules[key](context);
        }

        public string GetInvalidTransitionReason(GameState from, GameState to, GameContext context)
        {
            if (context == null)
                return "Context is null";
            if (context.IsAnimating)
                return "Animation in progress";
            if (_appStateManager?.GetCurrentAppState() != AppState.InGame)
                return "App state not in game";
            if (!_transitionRules.ContainsKey((from, to)))
                return $"No transition defined from {from} to {to}";
            return "Transition blocked by rules";
        }

        public event Action<StateTransition<GameState>> OnGameStateChanged;

        private Dictionary<(GameState, GameState), Func<GameContext, bool>> InitializeTransitionRules()
        {
            return new Dictionary<(GameState, GameState), Func<GameContext, bool>>
            {
                { (GameState.WaitingToStart, GameState.PlayerTurn), Allow },
                { (GameState.PlayerTurn, GameState.OpponentTurn), Allow },
                { (GameState.OpponentTurn, GameState.ResolvingBattle), Allow },
                { (GameState.ResolvingBattle, GameState.CollectingCards), Allow },
                { (GameState.ResolvingBattle, GameState.War), Allow },
                { (GameState.War, GameState.ResolvingBattle), Allow },
                { (GameState.CollectingCards, GameState.CheckingVictory), Allow },
                { (GameState.CheckingVictory, GameState.PlayerTurn), Allow },
                { (GameState.CheckingVictory, GameState.OpponentTurn), Allow },
                { (GameState.PlayerTurn, GameState.Paused), Allow },
                { (GameState.OpponentTurn, GameState.Paused), Allow },
                { (GameState.ResolvingBattle, GameState.Paused), Allow },
                { (GameState.Paused, GameState.PlayerTurn), Allow },
                { (GameState.Paused, GameState.OpponentTurn), Allow },
                { (GameState.Paused, GameState.ResolvingBattle), Allow },
                { (GameState.CheckingVictory, GameState.Error), Allow },
                { (GameState.Error, GameState.WaitingToStart), Allow }
            };
        }

        private bool Allow(GameContext context)
        {
            return true;
        }

        protected override void OnDestroy()
        {
            OnGameStateChanged = null;
            _transitionRules?.Clear();
            base.OnDestroy();
        }
    }
}
