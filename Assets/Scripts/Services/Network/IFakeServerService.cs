using Cysharp.Threading.Tasks;
using CardWar.Core.Data;

namespace CardWar.Services.Network
{
    public interface IFakeServerService
    {
        UniTask<ServerResponseData<bool>> StartNewGame();
        UniTask<ServerResponseData<GameRoundResultData>> DrawCard();
        UniTask<ServerResponseData<GameStateData>> GetGameState();
        UniTask<ServerResponseData<bool>> EndGame();
    }
}