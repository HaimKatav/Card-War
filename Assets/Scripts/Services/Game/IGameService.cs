using Cysharp.Threading.Tasks;
using CardWar.Core.Data;

namespace CardWar.Core.GameLogic
{
    public interface IGameService
    {
        void StartNewGame();
        UniTask<GameRoundResultData> PlayRound();
        GameStateData GetCurrentGameState();
        bool IsGameActive { get; }
    }
}