using System;
using CardWar.Core.Context;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardWar.Core.Commands.Base
{
    public abstract class BaseGameCommand : IGameCommand
    {
        public async UniTask<CommandResult> ExecuteAsync(GameContext context)
        {
            var name = GetType().Name;
            Debug.Log($"[{name}] Executing command");

            try
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if (context.CancellationToken.IsCancellationRequested)
                {
                    Debug.Log($"[{name}] Command cancelled before execution");
                    return CommandResult.Cancelled(context);
                }

                var result = await ExecuteAsyncCore(context);

                if (result.IsSuccess)
                {
                    Debug.Log($"[{name}] Command completed successfully");
                }
                else
                {
                    Debug.LogWarning($"[{name}] Command failed: {result.ErrorMessage}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{name}] Error: {ex.Message}");
                return CommandResult.Failure(context, ex.Message, ex);
            }
        }

        protected abstract UniTask<CommandResult> ExecuteAsyncCore(GameContext context);
    }
}
