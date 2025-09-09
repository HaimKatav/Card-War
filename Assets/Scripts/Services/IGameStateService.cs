using CardWar.Common.States;

namespace CardWar.Services
{
    public interface IGameStateService : IBaseServiceProvider
    {
        GameState CurrentGameState { get; }
        void ChangeState(GameState newState);
    }
}