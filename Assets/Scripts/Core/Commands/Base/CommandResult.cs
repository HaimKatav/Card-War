using System;
using CardWar.Core.Context;

namespace CardWar.Core.Commands.Base
{
    public sealed class CommandResult
    {
        public bool IsSuccess { get; private set; }
        public bool IsCancelled { get; private set; }
        public GameContext Context { get; private set; }
        public string ErrorMessage { get; private set; }
        public Exception Exception { get; private set; }

        CommandResult() { }

        public static CommandResult Success(GameContext context)
        {
            return new CommandResult
            {
                IsSuccess = true,
                IsCancelled = false,
                Context = context ?? throw new ArgumentNullException(nameof(context))
            };
        }

        public static CommandResult Failure(GameContext context, string errorMessage, Exception exception = null)
        {
            return new CommandResult
            {
                IsSuccess = false,
                IsCancelled = false,
                Context = context ?? throw new ArgumentNullException(nameof(context)),
                ErrorMessage = errorMessage ?? "Unknown error",
                Exception = exception
            };
        }

        public static CommandResult Cancelled(GameContext context)
        {
            return new CommandResult
            {
                IsSuccess = false,
                IsCancelled = true,
                Context = context ?? throw new ArgumentNullException(nameof(context)),
                ErrorMessage = "Operation was cancelled"
            };
        }
    }
}
