using System;
using UnityEngine;
using CardWar.Core.Context;
using CardWar.Services;
using Cysharp.Threading.Tasks;
using CardWar.Common;
using CardWar.Core.Commands.Base;

namespace CardWar.Core.Pipeline
{
    public interface ICommandPipeline : IDisposable
    {
        UniTask<CommandResult> ExecuteAsync(IGameCommand command, GameContext context);
        void RegisterMiddleware<TMiddleware>() where TMiddleware : class;
        void ClearMiddleware();
    }
}

