using System;
using CardWar.Game.Logic;
using CardWar.Common;

namespace CardWar.Services
{
    public interface IGameControllerService
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
        void ReturnToMenu();
    }
}