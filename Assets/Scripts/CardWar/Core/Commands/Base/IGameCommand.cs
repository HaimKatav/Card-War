using Cysharp.Threading.Tasks;

namespace CardWar.Core.Commands
{
    public interface IGameCommand
    {
        string CommandName { get; }
        CommandPriority Priority { get; }
        UniTask<CommandResult> ExecuteAsync(GameContext context);
        bool CanExecute(GameContext context);
        void OnError(GameException error);
    }
}
