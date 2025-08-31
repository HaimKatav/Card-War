using System;
using CardWar.Common;

namespace CardWar.Services
{
    public interface IGameStateService : IBaseServiceProvider
    {
        GameStatus MatchStatus { get; }
        event Action<GameState> GameStateChanged;
        event Action<float> OnLoadingProgress;
    }
}