using CardWar.Core.Data;
using Cysharp.Threading.Tasks;

namespace CardWar.Services.Network
{
    public interface IFakeServerService
    {
        UniTask<ServerResponseData<GameRoundResultData>> DrawCard();
        UniTask<ServerResponseData<GameStateData>> GetGameState();
        UniTask<ServerResponseData<bool>> StartNewGame();
    }
}