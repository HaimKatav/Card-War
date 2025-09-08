using System;
using UnityEngine;
using CardWar.Core.Context;
using CardWar.Services;
using Cysharp.Threading.Tasks;
using CardWar.Common;

namespace CardWar.Core.Commands.Base
{
    public interface IGameCommand
    {
        UniTask<CommandResult> ExecuteAsync(GameContext context);
    }
}
