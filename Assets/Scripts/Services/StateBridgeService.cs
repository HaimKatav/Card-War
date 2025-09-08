using UnityEngine;
using CardWar.Common;
using CardWar.Core.StateManagement;

namespace CardWar.Services
{
    public class StateBridgeService : IGameStateService
    {
        private readonly ICommandStateManager _commandStateManager;
        
        public StateBridgeService(ICommandStateManager commandStateManager)
        {
            _commandStateManager = commandStateManager;
            
            _commandStateManager.OnStateChanged += HandleStateTransition;
        }
        
        public GameState CurrentGameState => _commandStateManager.CurrentState;
        
        public void ChangeState(GameState newState)
        {
            Debug.LogWarning($"[StateBridge] Direct state change called for {newState} - should use commands instead");
            _commandStateManager.TransitionTo(newState, _commandStateManager.CurrentContext.WithState(newState));
        }
        
        private void HandleStateTransition(StateTransition transition)
        {
            Debug.Log($"[StateBridge] Command system changed state: {transition.FromState} -> {transition.ToState}");
        }
    }
}