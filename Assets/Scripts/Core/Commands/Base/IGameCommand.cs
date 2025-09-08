using CardWar.Core.Context;
using Cysharp.Threading.Tasks;

namespace CardWar.Core.Commands.Base
{
    public interface IGameCommand
    {
        UniTask<CommandResult> ExecuteAsync(GameContext context);
    }
}
