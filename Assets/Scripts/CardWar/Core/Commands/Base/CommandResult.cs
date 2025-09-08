using System;

namespace CardWar.Core.Commands
{
    public readonly struct CommandResult
    {
        public bool IsSuccess { get; }
        public object Data { get; }
        public string Message { get; }
        public CommandResultType Type { get; }

        CommandResult(bool isSuccess, object data, string message, CommandResultType type)
        {
            IsSuccess = isSuccess;
            Data = data;
            Message = message;
            Type = type;
        }

        public static CommandResult Success(object data = null) => new(true, data, null, CommandResultType.Success);
        public static CommandResult Failure(string message) => new(false, null, message, CommandResultType.Failure);
        public static CommandResult ValidationFailed(string message) => new(false, null, message, CommandResultType.ValidationFailed);
        public static CommandResult Error(Exception exception) => new(false, null, exception.Message, CommandResultType.Error);
        public static CommandResult Cancelled() => new(false, null, null, CommandResultType.Cancelled);

        public enum CommandResultType
        {
            Success,
            Failure,
            ValidationFailed,
            Error,
            Cancelled
        }
    }
}
