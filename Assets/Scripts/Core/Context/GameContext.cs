using System.Threading;
using CardWar.Core.States;
using CardWar.Game.Logic;

namespace CardWar.Core.Context
{
    public sealed class GameContext
    {
        public AppState AppState { get; }
        public GameState GameState { get; }
        public RoundData CurrentRound { get; }
        public float Progress { get; }
        public string ProgressMessage { get; }
        public bool IsAnimating { get; }
        public bool IsWaitingForNetwork { get; }
        public CancellationToken CancellationToken { get; }

        public GameContext(
            AppState appState,
            GameState gameState,
            RoundData currentRound = null,
            float progress = 0f,
            string progressMessage = null,
            bool isAnimating = false,
            bool isWaitingForNetwork = false,
            CancellationToken cancellationToken = default)
        {
            AppState = appState;
            GameState = gameState;
            CurrentRound = currentRound;
            Progress = progress;
            ProgressMessage = progressMessage;
            IsAnimating = isAnimating;
            IsWaitingForNetwork = isWaitingForNetwork;
            CancellationToken = cancellationToken;
        }

        public GameContext WithAppState(AppState appState)
        {
            return new GameContext(appState, GameState, CurrentRound, Progress, ProgressMessage, IsAnimating, IsWaitingForNetwork, CancellationToken);
        }

        public GameContext WithGameState(GameState gameState)
        {
            return new GameContext(AppState, gameState, CurrentRound, Progress, ProgressMessage, IsAnimating, IsWaitingForNetwork, CancellationToken);
        }

        public GameContext WithProgress(float progress, string message = null)
        {
            return new GameContext(AppState, GameState, CurrentRound, progress, message ?? ProgressMessage, IsAnimating, IsWaitingForNetwork, CancellationToken);
        }

        public GameContext WithRound(RoundData round)
        {
            return new GameContext(AppState, GameState, round, Progress, ProgressMessage, IsAnimating, IsWaitingForNetwork, CancellationToken);
        }

        public GameContext WithAnimation(bool isAnimating)
        {
            return new GameContext(AppState, GameState, CurrentRound, Progress, ProgressMessage, isAnimating, IsWaitingForNetwork, CancellationToken);
        }

        public GameContext WithNetworkWait(bool isWaiting)
        {
            return new GameContext(AppState, GameState, CurrentRound, Progress, ProgressMessage, IsAnimating, isWaiting, CancellationToken);
        }
    }
}
