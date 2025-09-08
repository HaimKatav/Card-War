using CardWar.Core.Commands.Base;
using CardWar.Core.Context;
using Cysharp.Threading.Tasks;

namespace CardWar.Core.Pipeline
{
    public interface ICommandPipeline
    {
        void AddCommand(IGameCommand command);
        void ClearCommands();
        UniTask<CommandResult> ExecuteAsync(GameContext context);
    }
}
