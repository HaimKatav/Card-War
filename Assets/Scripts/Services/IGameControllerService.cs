using System;
using CardWar.Common;
using CardWar.Game.Logic;

namespace CardWar.Services
{
    public interface IGameControllerService
    {
        event Action<RoundData> RoundStartedEvent;
        event Action CardsDrawnEvent;
        event Action<RoundResult> RoundCompletedEvent;
        event Action<int> WarStartedEvent;
        event Action WarCompletedEvent;
        event Action GamePausedEvent;
        event Action GameResumedEvent;
        event Action<GameStatus> GameOverEvent;
    }
}