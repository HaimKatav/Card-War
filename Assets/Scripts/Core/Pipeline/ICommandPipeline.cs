using System;
using UnityEngine;
using CardWar.Core.Context;
using CardWar.Services;
using Cysharp.Threading.Tasks;
using CardWar.Core.Commands.Base;

namespace CardWar.Core.Pipeline
{
    public interface ICommandPipeline
    {
        UniTask<CommandResult> ExecuteAsync(IGameCommand command, GameContext context);
    }
}

