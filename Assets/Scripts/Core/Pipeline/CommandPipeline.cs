using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Core.Commands.Base;
using CardWar.Core.Context;
using CardWar.Services;
using Cysharp.Threading.Tasks;

namespace CardWar.Core.Pipeline
{
    public class CommandPipeline : ICommandPipeline
    {
        readonly List<IGameCommand> commands = new();
        readonly string pipelineName;

        public CommandPipeline(string pipelineName)
        {
            this.pipelineName = pipelineName ?? throw new ArgumentNullException(nameof(pipelineName));
        }

        public void AddCommand(IGameCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            commands.Add(command);
            Debug.Log($"[{pipelineName}] Added command: {command.GetType().Name}");
        }

        public void ClearCommands()
        {
            commands.Clear();
            Debug.Log($"[{pipelineName}] Cleared all commands");
        }

        public async UniTask<CommandResult> ExecuteAsync(GameContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            Debug.Log($"[{pipelineName}] Starting pipeline with {commands.Count} commands");

            if (commands.Count == 0)
            {
                Debug.LogWarning($"[{pipelineName}] Pipeline has no commands");
                return CommandResult.Success(context);
            }

            var currentContext = context;

            for (var i = 0; i < commands.Count; i++)
            {
                if (currentContext.CancellationToken.IsCancellationRequested)
                {
                    Debug.LogWarning($"[{pipelineName}] Pipeline cancelled at command {i + 1}/{commands.Count}");
                    return CommandResult.Cancelled(currentContext);
                }

                var command = commands[i];
                Debug.Log($"[{pipelineName}] Executing command {i + 1}/{commands.Count}: {command.GetType().Name}");

                var result = await command.ExecuteAsync(currentContext);

                if (result.IsCancelled)
                {
                    Debug.LogWarning($"[{pipelineName}] Pipeline cancelled by command: {command.GetType().Name}");
                    return result;
                }

                if (!result.IsSuccess)
                {
                    Debug.LogError($"[{pipelineName}] Pipeline failed at command: {command.GetType().Name}");
                    return result;
                }

                currentContext = result.Context;
            }

            Debug.Log($"[{pipelineName}] Pipeline completed successfully");
            return CommandResult.Success(currentContext);
        }
    }
}
