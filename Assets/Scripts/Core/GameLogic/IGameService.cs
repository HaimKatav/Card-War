using Cysharp.Threading.Tasks;
using CardWar.Core.Data;

namespace CardWar.Services.Game
{
    public interface IGameService
    {
        void StartNewGame();
        UniTask<GameRoundResultData> PlayRound();
        GameStateData GetCurrentGameState();
        bool IsGameActive { get; }
    }
}