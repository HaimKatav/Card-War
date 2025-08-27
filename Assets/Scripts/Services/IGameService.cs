using Cysharp.Threading.Tasks;

public interface IGameService
{
    void StartNewGame();
    UniTask<GameRoundResult> PlayRound();
    GameState GetCurrentGameState();
    bool IsGameActive { get; }
}