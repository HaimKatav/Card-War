using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using CardWar.Game.Logic;
using CardWar.Core;
using CardWar.Common;

namespace CardWar.Game
{
    public class GameServerHandler : MonoBehaviour
    {
        public event Action<bool> OnServerInitialized;
        public event Action<RoundData> OnCardsDrawn;
        public event Action<RoundData> OnWarResolved;
        public event Action<GameStatus> OnGameStatusChanged;
        public event Action<string> OnServerError;
        
        private FakeWarServer _warServer;
        private GameSettings _gameSettings;
        private const int MAX_RETRY_ATTEMPTS = 3;
        
        public GameStatus CurrentGameStatus => _warServer?.Status ?? GameStatus.NotStarted;
        
        #region Initialization
        
        public void Initialize(GameSettings gameSettings)
        {
            _gameSettings = gameSettings;
            _warServer = new FakeWarServer(_gameSettings);
            
            Debug.Log("[GameServerHandler] Initialized");
        }
        
        #endregion
        
        #region Server Operations
        
        public async UniTask<bool> InitializeNewGame()
        {
            Debug.Log("[GameServerHandler] Initializing new game on server");
            
            var success = await ExecuteWithRetry(
                async () => await _warServer.InitializeNewGame(),
                "InitializeNewGame"
            );
            
            if (success)
            {
                Debug.Log("[GameServerHandler] Server game initialized successfully");
                var stats = await GetGameStats();
                if (stats != null)
                {
                    Debug.Log($"[GameServerHandler] Initial state - Player: {stats.PlayerCardCount}, Opponent: {stats.OpponentCardCount}");
                }
            }
            else
            {
                Debug.LogError("[GameServerHandler] Failed to initialize game on server");
            }
            
            OnServerInitialized?.Invoke(success);
            return success;
        }
        
        public async UniTask<RoundData> DrawCards()
        {
            Debug.Log("[GameServerHandler] Requesting card draw from server");
            
            var roundData = await ExecuteWithRetry(
                async () => await _warServer.DrawCards(),
                "DrawCards"
            );
            
            if (roundData != null)
            {
                Debug.Log($"[GameServerHandler] Cards drawn - Round {roundData.RoundNumber}: " +
                         $"Player {roundData.PlayerCard.Rank} vs Opponent {roundData.OpponentCard.Rank}");
                
                if (roundData.IsWar)
                {
                    Debug.Log("[GameServerHandler] WAR detected!");
                }
                else
                {
                    Debug.Log($"[GameServerHandler] Round result: {roundData.Result}");
                }
                
                OnCardsDrawn?.Invoke(roundData);
                CheckGameStatus();
            }
            else
            {
                var error = "[GameServerHandler] Failed to draw cards from server";
                Debug.LogError(error);
                OnServerError?.Invoke(error);
            }
            
            return roundData;
        }
        
        public async UniTask<RoundData> ResolveWar()
        {
            Debug.Log("[GameServerHandler] Requesting war resolution from server");
            
            var warData = await ExecuteWithRetry(
                async () => await _warServer.ResolveWar(),
                "ResolveWar"
            );
            
            if (warData != null)
            {
                Debug.Log($"[GameServerHandler] War resolved - " +
                         $"Player {warData.PlayerCard.Rank} vs Opponent {warData.OpponentCard.Rank}");
                
                if (warData.HasChainedWar)
                {
                    Debug.Log("[GameServerHandler] CHAINED WAR detected!");
                }
                else
                {
                    Debug.Log($"[GameServerHandler] War winner: {warData.Result}");
                }
                
                OnWarResolved?.Invoke(warData);
                CheckGameStatus();
            }
            else
            {
                var error = "[GameServerHandler] Failed to resolve war on server";
                Debug.LogError(error);
                OnServerError?.Invoke(error);
            }
            
            return warData;
        }
        
        public async UniTask<GameStats> GetGameStats()
        {
            return await _warServer.GetGameStats();
        }
        
        #endregion
        
        #region Private Methods
        
        private async UniTask<T> ExecuteWithRetry<T>(Func<UniTask<T>> operation, string operationName)
        {
            for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    var result = await operation();
                    
                    if (!EqualityComparer<T>.Default.Equals(result, default(T)))
                    {
                        if (attempt > 1)
                        {
                            Debug.Log($"[GameServerHandler] {operationName} succeeded on attempt {attempt}");
                        }
                        return result;
                    }
            
                    Debug.LogWarning($"[GameServerHandler] {operationName} returned default value on attempt {attempt}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameServerHandler] {operationName} failed on attempt {attempt}: {e.Message}");
                }
        
                if (attempt < MAX_RETRY_ATTEMPTS)
                {
                    var retryDelay = (int)(_gameSettings.FakeNetworkDelay * 1000 * attempt);
                    Debug.Log($"[GameServerHandler] Retrying {operationName} in {retryDelay}ms...");
                    await UniTask.Delay(retryDelay);
                }
            }
    
            return default(T);
        }
        
        private void CheckGameStatus()
        {
            if (_warServer == null) return;
            
            var status = _warServer.Status;
            
            if (status == GameStatus.PlayerWon || status == GameStatus.OpponentWon)
            {
                Debug.Log($"[GameServerHandler] Game ended - {status}");
                OnGameStatusChanged?.Invoke(status);
            }
        }
        
        #endregion
        
        #region Cleanup
        
        public void ResetServer()
        {
            _warServer = null;
            Debug.Log("[GameServerHandler] Server reset");
        }
        
        private void OnDestroy()
        {
            OnServerInitialized = null;
            OnCardsDrawn = null;
            OnWarResolved = null;
            OnGameStatusChanged = null;
            OnServerError = null;
            
            _warServer = null;
        }
        
        #endregion
    }
}