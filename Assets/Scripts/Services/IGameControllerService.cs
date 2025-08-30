using System;
using CardWar.Common;
using CardWar.Game.Logic;

namespace CardWar.Services
{
    public interface IGameControllerService : IBaseServiceProvider
    {
        event Action<RoundData> OnRoundStarted;
        event Action OnCardsDrawn;
        event Action<RoundResult> OnRoundCompleted;
        event Action<int> OnWarStarted;
        event Action OnWarCompleted;
        event Action OnGamePaused;
        event Action OnGameResumed;
        event Action<bool> OnGameOver;
        
        void StartNewGame();
        void DrawNextCards();
        void PauseGame();
        void ResumeGame();
        void EndGame(bool playerWon);
        void ResetGame();
    }
}