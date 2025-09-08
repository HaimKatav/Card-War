using System;
using UnityEngine;
using CardWar.Common;
using CardWar.Core.Commands.Base;
using CardWar.Core.Context;
using CardWar.Services;
using Cysharp.Threading.Tasks;

namespace CardWar.Core.Commands.System
{
    public class PauseGameCommand : BaseGameCommand
    {
        protected override async UniTask<CommandResult> ExecuteAsyncCore(GameContext context)
        {
            var gameStateService = await ServiceLocator.Get<IGameStateService>();

            if (gameStateService.CurrentGameState != GameState.Playing)
            {
                return CommandResult.Failure(context, $"Cannot pause from state: {gameStateService.CurrentGameState}");
            }

            gameStateService.ChangeState(GameState.Paused);

            var gameController = await ServiceLocator.Get<IGameControllerService>();

            return CommandResult.Success(context.WithState(GameState.Paused));
        }
    }
}
