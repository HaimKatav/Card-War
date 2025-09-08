using System;
using UnityEngine;
using CardWar.Core.Commands.Base;
using CardWar.Core.Context;
using CardWar.Services;
using Cysharp.Threading.Tasks;

namespace CardWar.Core.Pipeline
{
    public sealed class CommandPipeline : ICommandPipeline
    {
        public async UniTask<CommandResult> ExecuteAsync(IGameCommand command, GameContext context)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var commandName = command.GetType().Name;
            Debug.Log($"[CommandPipeline] Executing {commandName}");

            if (context.CancellationToken.IsCancellationRequested)
            {
                Debug.LogWarning("[CommandPipeline] Command cancelled before execution");
                return CommandResult.Cancelled(context);
            }

            var result = await command.ExecuteAsync(context);

            if (result.IsCancelled)
            {
                Debug.LogWarning($"[CommandPipeline] Command cancelled: {commandName}");
                return result;
            }

            if (!result.IsSuccess)
            {
                Debug.LogError($"[CommandPipeline] Command failed: {commandName}");
                return result;
            }

            Debug.Log($"[CommandPipeline] Command succeeded: {commandName}");
            return result;
        }
    }
}

