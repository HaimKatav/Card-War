using System;
using UnityEngine;
using CardWar.Core.Context;
using CardWar.Services;
using Cysharp.Threading.Tasks;
using CardWar.Common;

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
                Context = context
            };
        }

        public static CommandResult Failure(GameContext context, string errorMessage, Exception exception = null)
        {
            return new CommandResult
            {
                IsSuccess = false,
                Context = context,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }

        public static CommandResult Cancelled(GameContext context)
        {
            return new CommandResult
            {
                IsCancelled = true,
                Context = context
            };
        }
    }
}
