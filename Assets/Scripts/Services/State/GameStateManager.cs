using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Core.Context;
using CardWar.Core.States;
using CardWar.Services;

namespace CardWar.Services.State
{
    public class GameStateManager : IGameStateManager
    {
        private GameState _currentState;
        private readonly Dictionary<(GameState, GameState), Func<GameContext, bool>> _transitionRules;
        private readonly IAppStateManager _appStateManager;

        public GameStateManager()
        {
            _appStateManager = ServiceLocator.Get<IAppStateManager>();
            _currentState = GameState.WaitingToStart;
            _transitionRules = InitializeTransitionRules();
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
            Debug.Log($"[GameStateManager] State changed: {oldState} -> {state}");
        }

        public bool CanTransition(GameState from, GameState to, GameContext context)
        {
            if (context == null)
                return false;
            if (context.IsAnimating)
                return false;
            if (_appStateManager.GetCurrentAppState() != AppState.InGame)
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
            if (_appStateManager.GetCurrentAppState() != AppState.InGame)
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
                { (GameState.WaitingToStart, GameState.PlayerTurn), ctx => true },
                { (GameState.PlayerTurn, GameState.OpponentTurn), ctx => true },
                { (GameState.OpponentTurn, GameState.ResolvingBattle), ctx => true },
                { (GameState.ResolvingBattle, GameState.CollectingCards), ctx => true },
                { (GameState.ResolvingBattle, GameState.War), ctx => true },
                { (GameState.War, GameState.ResolvingBattle), ctx => true },
                { (GameState.CollectingCards, GameState.CheckingVictory), ctx => true },
                { (GameState.CheckingVictory, GameState.PlayerTurn), ctx => true },
                { (GameState.CheckingVictory, GameState.OpponentTurn), ctx => true },
                { (GameState.PlayerTurn, GameState.Paused), ctx => true },
                { (GameState.OpponentTurn, GameState.Paused), ctx => true },
                { (GameState.ResolvingBattle, GameState.Paused), ctx => true },
                { (GameState.Paused, GameState.PlayerTurn), ctx => true },
                { (GameState.Paused, GameState.OpponentTurn), ctx => true },
                { (GameState.Paused, GameState.ResolvingBattle), ctx => true },
                { (GameState.CheckingVictory, GameState.Error), ctx => true },
                { (GameState.Error, GameState.WaitingToStart), ctx => true }
            };
        }
    }
}
