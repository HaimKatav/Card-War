using System;
using UnityEngine;
using System.Threading;
using CardWar.Common;
using CardWar.Game.Logic;

namespace CardWar.Core.Context
{
    public sealed class GameContext
    {
        public GameState CurrentState { get; }
        public RoundData CurrentRound { get; }
        public float Progress { get; }
        public string ProgressMessage { get; }
        public CancellationToken CancellationToken { get; }

        public GameContext(
            GameState currentState,
            RoundData currentRound = null,
            float progress = 0f,
            string progressMessage = null,
            CancellationToken cancellationToken = default)
        {
            CurrentState = currentState;
            CurrentRound = currentRound;
            Progress = progress;
            ProgressMessage = progressMessage;
            CancellationToken = cancellationToken;
        }

        public GameContext WithState(GameState newState)
        {
            return new GameContext(newState, CurrentRound, Progress, ProgressMessage, CancellationToken);
        }

        public GameContext WithProgress(float progress, string message = null)
        {
            return new GameContext(CurrentState, CurrentRound, progress, message ?? ProgressMessage, CancellationToken);
        }

        public GameContext WithRound(RoundData round)
        {
            return new GameContext(CurrentState, round, Progress, ProgressMessage, CancellationToken);
        }
    }
}
