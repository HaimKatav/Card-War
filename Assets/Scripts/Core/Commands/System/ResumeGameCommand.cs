using CardWar.Common;
using CardWar.Core.Commands.Base;
using CardWar.Core.Context;
using CardWar.Services;
using Cysharp.Threading.Tasks;

namespace CardWar.Core.Commands.System
{
    public class ResumeGameCommand : BaseGameCommand
    {
        protected override async UniTask<CommandResult> ExecuteAsyncCore(GameContext context)
        {
            var gameStateService = await ServiceLocator.Get<IGameStateService>();

            if (gameStateService.CurrentGameState != GameState.Paused)
            {
                return CommandResult.Failure(context, $"Cannot resume from state: {gameStateService.CurrentGameState}");
            }

            gameStateService.ChangeState(GameState.Playing);

            var gameController = await ServiceLocator.Get<IGameControllerService>();

            return CommandResult.Success(context.WithState(GameState.Playing));
        }
    }
}
