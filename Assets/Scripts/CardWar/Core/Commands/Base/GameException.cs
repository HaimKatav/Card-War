using System;

namespace CardWar.Core.Commands
{
    public class GameException : Exception
    {
        public GameErrorType ErrorType { get; }

        public GameException(string message, Exception innerException) : base(message, innerException)
        {
            ErrorType = GameErrorType.Unknown;
        }

        public GameException(string message, GameErrorType errorType, Exception innerException = null) : base(message, innerException)
        {
            ErrorType = errorType;
        }

        public enum GameErrorType
        {
            Unknown,
            ValidationFailed,
            AssetLoadFailed,
            NetworkError,
            StateTransitionFailed,
            AnimationFailed
        }
    }
}
