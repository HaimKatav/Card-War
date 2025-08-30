using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Common;

namespace CardWar.Core
{
    public class GameStateMachine
    {
        private readonly Dictionary<GameState, StateDefinition> _states = new();
        private GameState _currentState = GameState.FirstLoad;
        private GameState _previousState = GameState.FirstLoad;
        
        public GameState CurrentStateType => _currentState;
        public GameState PreviousStateType => _previousState;
        
        public event Action<GameState, GameState> OnStateChanged;
        
        private class StateDefinition
        {
            public Action OnEnter { get; set; }
            public Action OnExit { get; set; }
            public Action OnUpdate { get; set; }
        }
        
        public void RegisterState(GameState state, Action onEnter = null, Action onExit = null, Action onUpdate = null)
        {
            _states[state] = new StateDefinition
            {
                OnEnter = onEnter,
                OnExit = onExit,
                OnUpdate = onUpdate
            };
            
            Debug.Log($"[GameStateMachine] State registered: {state}");
        }
        
        public void ChangeState(GameState newState)
        {
            if (_currentState == newState)
            {
                Debug.LogWarning($"[GameStateMachine] Already in state: {newState}");
                return;
            }
            
            if (!_states.ContainsKey(newState))
            {
                Debug.LogError($"[GameStateMachine] State not registered: {newState}");
                return;
            }
            
            _previousState = _currentState;
            
            if (_states.TryGetValue(_currentState, out var oldState))
                oldState.OnExit?.Invoke();
                
            _currentState = newState;
            
            if (_states.TryGetValue(_currentState, out var currentState))
                currentState.OnEnter?.Invoke();
                
            Debug.Log($"[GameStateMachine] State transition: {_previousState} -> {_currentState}");
            OnStateChanged?.Invoke(_currentState, _previousState);
        }
        
        public void Update()
        {
            if (_states.TryGetValue(_currentState, out var currentState))
                currentState.OnUpdate?.Invoke();
        }
        
        public bool HasState(GameState state)
        {
            return _states.ContainsKey(state);
        }
        
        public void Clear()
        {
            _states.Clear();
            OnStateChanged = null;
        }
    }
}