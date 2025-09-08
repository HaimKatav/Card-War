using System.Collections.Generic;
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

        readonly Dictionary<string, object> properties;

        GameContext(
            GameState state,
            RoundData round,
            float progress,
            string progressMessage,
            CancellationToken token,
            Dictionary<string, object> properties)
        {
            CurrentState = state;
            CurrentRound = round;
            Progress = progress;
            ProgressMessage = progressMessage;
            CancellationToken = token;
            this.properties = properties ?? new Dictionary<string, object>();
        }

        public T GetProperty<T>(string key) where T : class
        {
            return properties.TryGetValue(key, out var value) ? value as T : null;
        }

        public GameContext WithProperty(string key, object value)
        {
            var newProperties = new Dictionary<string, object>(properties);
            newProperties[key] = value;
            return new GameContext(CurrentState, CurrentRound, Progress, ProgressMessage, CancellationToken, newProperties);
        }

        public GameContext WithState(GameState newState)
        {
            return new GameContext(newState, CurrentRound, Progress, ProgressMessage, CancellationToken, properties);
        }

        public GameContext WithProgress(float progress, string message = null)
        {
            return new GameContext(CurrentState, CurrentRound, progress, message ?? ProgressMessage, CancellationToken, properties);
        }

        public GameContext WithRoundData(RoundData round)
        {
            return new GameContext(CurrentState, round, Progress, ProgressMessage, CancellationToken, properties);
        }

        public static GameContext Create(GameState initialState, CancellationToken token = default)
        {
            return new GameContext(initialState, null, 0f, string.Empty, token, new Dictionary<string, object>());
        }
    }
}
