using System;
using CardWar.Core.Context;

namespace CardWar.Core.Commands.Base
{
    public class GameException : Exception
    {
        public GameErrorType ErrorType { get; }
        public GameContext Context { get; }

        public GameException(string message, GameErrorType errorType, GameContext context = null, Exception innerException = null)
            : base(message, innerException)
        {
            ErrorType = errorType;
            Context = context;
        }
    }

    public enum GameErrorType
    {
        ValidationFailed,
        AssetLoadFailed,
        NetworkError,
        StateTransitionFailed,
        AnimationFailed
    }
}
