using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Core.Commands.Base;
using CardWar.Core.Context;
using CardWar.Services;
using Cysharp.Threading.Tasks;
using CardWar.Common;

namespace CardWar.Core.Pipeline
{
    public sealed class CommandPipeline : ICommandPipeline
    {
        readonly List<Type> middlewareTypes;
        bool disposed;

        public CommandPipeline()
        {
            middlewareTypes = new List<Type>();
        }

        public async UniTask<CommandResult> ExecuteAsync(IGameCommand command, GameContext context)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(CommandPipeline));

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            Debug.Log($"[CommandPipeline] Executing command: {command.GetType().Name}");

            return await command.ExecuteAsync(context);
        }

        public void RegisterMiddleware<TMiddleware>() where TMiddleware : class
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(CommandPipeline));

            var middlewareType = typeof(TMiddleware);

            if (!middlewareTypes.Contains(middlewareType))
            {
                middlewareTypes.Add(middlewareType);
                Debug.Log($"[CommandPipeline] Registered middleware: {middlewareType.Name}");
            }
        }

        public void ClearMiddleware()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(CommandPipeline));

            middlewareTypes.Clear();
            Debug.Log("[CommandPipeline] Cleared all middleware");
        }

        public void Dispose()
        {
            if (disposed)
                return;

            ClearMiddleware();
            disposed = true;
            Debug.Log("[CommandPipeline] Disposed");
        }
    }
}

