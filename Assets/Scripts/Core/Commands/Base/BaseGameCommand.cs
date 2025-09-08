using System;
using UnityEngine;
using CardWar.Core.Context;
using CardWar.Services;
using Cysharp.Threading.Tasks;
using CardWar.Common;

namespace CardWar.Core.Commands.Base
{
    public abstract class BaseGameCommand : IGameCommand
    {
        public async UniTask<CommandResult> ExecuteAsync(GameContext context)
        {
            var commandName = GetType().Name;
            Debug.Log($"[{commandName}] Executing command");

            try
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if (context.CancellationToken.IsCancellationRequested)
                {
                    Debug.Log($"[{commandName}] Command cancelled before execution");
                    return CommandResult.Cancelled(context);
                }

                var result = await ExecuteAsyncCore(context);

                if (result.IsSuccess)
                {
                    Debug.Log($"[{commandName}] Command completed successfully");
                }
                else
                {
                    Debug.LogWarning($"[{commandName}] Command failed: {result.ErrorMessage}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{commandName}] Error: {ex.Message}");
                return CommandResult.Failure(context, ex.Message, ex);
            }
        }

        protected abstract UniTask<CommandResult> ExecuteAsyncCore(GameContext context);
    }
}
