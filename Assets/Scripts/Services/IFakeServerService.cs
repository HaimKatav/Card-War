using Cysharp.Threading.Tasks;

public interface IFakeServerService
{
    UniTask<ServerResponse<GameRoundResult>> DrawCard();
    UniTask<ServerResponse<GameState>> GetGameState();
    UniTask<ServerResponse<bool>> StartNewGame();
}