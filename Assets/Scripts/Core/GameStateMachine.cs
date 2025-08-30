using System;
using System.Collections.Generic;
using CardWar.Common;
using UnityEngine;
using CardWar.Services;

namespace CardWar.Core
{
    public interface IGameState
    {
        GameState StateType { get; }
        void Enter();
        void Exit();
        void Update();
    }

    public class GameStateMachine
    {
        private readonly Dictionary<GameState, IGameState> _states;
        private IGameState _currentState;
        private GameState _previousStateType;
        
        public GameState CurrentStateType => _currentState?.StateType ?? GameState.FirstLoad;
        public GameState PreviousStateType => _previousStateType;
        
        public event Action<GameState, GameState> OnStateChanged;

        public GameStateMachine()
        {
            _states = new Dictionary<GameState, IGameState>();
        }

        #region Public Methods

        public void RegisterState(IGameState state)
        {
            if (state == null) return;
            
            _states[state.StateType] = state;
            Debug.Log($"[GameStateMachine] State registered: {state.StateType}");
        }

        public void ChangeState(GameState newStateType)
        {
            if (_currentState != null && _currentState.StateType == newStateType)
            {
                Debug.LogWarning($"[GameStateMachine] Already in state: {newStateType}");
                return;
            }

            if (!_states.TryGetValue(newStateType, out var newState))
            {
                Debug.LogError($"[GameStateMachine] State not registered: {newStateType}");
                return;
            }

            _previousStateType = _currentState?.StateType ?? GameState.FirstLoad;
            
            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
            
            Debug.Log($"[GameStateMachine] State transition: {_previousStateType} -> {newStateType}");
            OnStateChanged?.Invoke(newStateType, _previousStateType);
        }

        public void Update()
        {
            _currentState?.Update();
        }

        #endregion
    }
    
 
}