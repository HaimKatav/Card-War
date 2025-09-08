using CardWar.Common;

namespace CardWar.Services
{
    public interface IGameStateService
    {
        GameState CurrentGameState { get; }
        void ChangeState(GameState newState);
    }
}