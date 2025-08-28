using System;
using Cysharp.Threading.Tasks;
using CardWar.Core.Data;
using CardWar.Core.Enums;

namespace CardWar.Services.Game
{
    public interface IGameService
    {
        GameState CurrentGameState { get; }
        GameStateData GameStateData { get; }
        int PlayerCardCount { get; }
        int OpponentCardCount { get; }
        event Action<GameState> OnGameStateChanged;
        event Action<GameRoundResultData> OnRoundComplete;
        
        UniTask StartNewGame();
        UniTask PlayNextRound();
        void PauseGame();
        void ResumeGame();
        void EndGame();
    }
}