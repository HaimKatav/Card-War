using System;
using Cysharp.Threading.Tasks;
using CardWar.Core.Context;

namespace CardWar.Core.Commands.Base
{
    public interface IGameCommand
    {
        UniTask<CommandResult> ExecuteAsync(GameContext context);
    }
}
