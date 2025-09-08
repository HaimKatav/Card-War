using System;
using Cysharp.Threading.Tasks;
using CardWar.Core.Commands.Base;
using CardWar.Core.Context;

namespace CardWar.Core.Pipeline
{
    public interface ICommandPipeline : IDisposable
    {
        UniTask<CommandResult> ExecuteAsync(IGameCommand command, GameContext context);
        void RegisterMiddleware<TMiddleware>() where TMiddleware : class;
        void ClearMiddleware();
    }
}
