using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardWar.Core.Commands
{
    public abstract class BaseGameCommand : IGameCommand
    {
        readonly ILogger logger;
        readonly string commandId;

        public abstract string CommandName { get; }
        public virtual CommandPriority Priority => CommandPriority.Normal;

        protected BaseGameCommand(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            commandId = Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        public async UniTask<CommandResult> ExecuteAsync(GameContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                logger.Log($"[{CommandName}:{commandId}] Starting execution");
                if (!CanExecute(context))
                {
                    var error = $"Command {CommandName} validation failed";
                    logger.LogWarning($"[{CommandName}:{commandId}] {error}");
                    return CommandResult.ValidationFailed(error);
                }
                var result = await ExecuteCore(context);
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 100)
                {
                    logger.LogWarning($"[{CommandName}:{commandId}] Slow execution: {stopwatch.ElapsedMilliseconds}ms");
                }
                logger.Log($"[{CommandName}:{commandId}] Completed in {stopwatch.ElapsedMilliseconds}ms");
                return result;
            }
            catch (OperationCanceledException)
            {
                logger.Log($"[{CommandName}:{commandId}] Cancelled");
                return CommandResult.Cancelled();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var gameException = new GameException($"{CommandName} execution failed", ex);
                OnError(gameException);
                logger.LogError($"[{CommandName}:{commandId}] Failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
                return CommandResult.Error(gameException);
            }
        }

        protected abstract UniTask<CommandResult> ExecuteCore(GameContext context);
        public abstract bool CanExecute(GameContext context);
        public virtual void OnError(GameException error) { }
    }
}
