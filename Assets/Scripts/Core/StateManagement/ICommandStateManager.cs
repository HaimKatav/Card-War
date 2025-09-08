using System;
using CardWar.Common;
using CardWar.Core.Context;

namespace CardWar.Core.StateManagement
{
    public interface ICommandStateManager
    {
        GameState CurrentState { get; }
        GameContext CurrentContext { get; }
        bool CanTransitionTo(GameState newState);
        void TransitionTo(GameState newState, GameContext context);
        event Action<StateTransition> OnStateChanged;
    }
    
    public struct StateTransition
    {
        public GameState FromState { get; }
        public GameState ToState { get; }
        public GameContext Context { get; }
        public DateTime Timestamp { get; }
        
        public StateTransition(GameState from, GameState to, GameContext context)
        {
            FromState = from;
            ToState = to;
            Context = context;
            Timestamp = DateTime.UtcNow;
        }
    }
}