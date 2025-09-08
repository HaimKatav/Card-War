using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Core.Commands.Base;
using CardWar.Core.Context;
using Cysharp.Threading.Tasks;

namespace CardWar.Core.Pipeline
{
    public sealed class CommandPipeline : ICommandPipeline
    {
        private readonly List<Type> _middlewareTypes;
        private bool _disposed;

        public CommandPipeline()
        {
            _middlewareTypes = new List<Type>();
        }

        public async UniTask<CommandResult> ExecuteAsync(IGameCommand command, GameContext context)
        {
            if (_disposed)
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
            if (_disposed)
                throw new ObjectDisposedException(nameof(CommandPipeline));

            var middlewareType = typeof(TMiddleware);

            if (!_middlewareTypes.Contains(middlewareType))
            {
                _middlewareTypes.Add(middlewareType);
                Debug.Log($"[CommandPipeline] Registered middleware: {middlewareType.Name}");
            }
        }

        public void ClearMiddleware()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CommandPipeline));

            _middlewareTypes.Clear();
            Debug.Log("[CommandPipeline] Cleared all middleware");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            ClearMiddleware();
            _disposed = true;
            Debug.Log("[CommandPipeline] Disposed");
        }
    }
}
