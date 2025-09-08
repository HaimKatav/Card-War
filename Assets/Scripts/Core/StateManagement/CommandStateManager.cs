using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Common;
using CardWar.Core.Context;

namespace CardWar.Core.StateManagement
{
    public class CommandStateManager : ICommandStateManager
    {
        private GameState _currentState;
        private GameContext _currentContext;
        private readonly Dictionary<GameState, HashSet<GameState>> _validTransitions;
        
        public GameState CurrentState => _currentState;
        public GameContext CurrentContext => _currentContext;
        public event Action<StateTransition> OnStateChanged;
        
        public CommandStateManager()
        {
            _currentState = GameState.Menu;
            _currentContext = new GameContext(_currentState);
            _validTransitions = DefineTransitionRules();
        }
        
        private Dictionary<GameState, HashSet<GameState>> DefineTransitionRules()
        {
            return new Dictionary<GameState, HashSet<GameState>>
            {
                [GameState.Menu] = new HashSet<GameState> 
                { 
                    GameState.Loading, 
                    GameState.Settings 
                },
                
                [GameState.Loading] = new HashSet<GameState> 
                { 
                    GameState.Playing, 
                    GameState.Menu 
                },
                
                [GameState.Playing] = new HashSet<GameState> 
                { 
                    GameState.Paused, 
                    GameState.GameOver, 
                    GameState.Menu 
                },
                
                [GameState.Paused] = new HashSet<GameState> 
                { 
                    GameState.Playing, 
                    GameState.Menu, 
                    GameState.Settings 
                },
                
                [GameState.GameOver] = new HashSet<GameState> 
                { 
                    GameState.Menu, 
                    GameState.Loading 
                },
                
                [GameState.Settings] = new HashSet<GameState> 
                { 
                    GameState.Menu, 
                    GameState.Paused 
                }
            };
        }
        
        public bool CanTransitionTo(GameState newState)
        {
            if (_currentState == newState)
                return false;
                
            if (_validTransitions.TryGetValue(_currentState, out var validStates))
                return validStates.Contains(newState);
                
            return false;
        }
        
        public void TransitionTo(GameState newState, GameContext context)
        {
            if (!CanTransitionTo(newState))
            {
                Debug.LogWarning($"[CommandStateManager] Invalid transition: {_currentState} -> {newState}");
                return;
            }
            
            var transition = new StateTransition(_currentState, newState, context);
            _currentState = newState;
            _currentContext = context;
            
            Debug.Log($"[CommandStateManager] State transition: {transition.FromState} -> {transition.ToState}");
            OnStateChanged?.Invoke(transition);
        }
    }
}