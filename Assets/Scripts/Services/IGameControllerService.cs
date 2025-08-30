using System;
using CardWar.Common;
using CardWar.Game.Logic;
using Cysharp.Threading.Tasks;

namespace CardWar.Services
{
    public interface IGameControllerService
    {
        event Action<RoundData> OnRoundStarted;
        event Action OnCardsDrawn;
        event Action<RoundResult> OnRoundCompleted;
        event Action<int> OnWarStarted;
        event Action OnWarCompleted;
        event Action OnGameCreated;
        event Action OnGamePaused;
        event Action OnGameResumed;
        event Action<bool> OnGameOver;
        
        UniTask CreateNewGame();
        UniTask DrawNextCards();
        void PauseGame();
        void ResumeGame();
        void EndGame(bool playerWon);
        void ResetGame();
    }
}