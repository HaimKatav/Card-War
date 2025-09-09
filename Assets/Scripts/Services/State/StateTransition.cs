using System;
using CardWar.Common.States;
using CardWar.Core.Context;

namespace CardWar.Services.State
{
    public readonly struct StateTransition<TState>
    {
        public TState From { get; }
        public TState To { get; }
        public GameContext Context { get; }
        public DateTime Timestamp { get; }

        public StateTransition(TState from, TState to, GameContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            From = from;
            To = to;
            Context = context;
            Timestamp = DateTime.UtcNow;
        }
    }
}
