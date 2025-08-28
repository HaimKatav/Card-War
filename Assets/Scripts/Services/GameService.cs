using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using CardWar.Core.Async;
using CardWar.Infrastructure.Events;
using CardWar.Services.Network;

namespace CardWar.Services.Game
{
    public class GameService : IGameService, IInitializable, IDisposable
    {
        private readonly IFakeServerService _server;
        private readonly AsyncOperationManager _asyncManager;
        private readonly SignalBus _signalBus;
        
        private GameState _currentState = GameState.Idle;
        private GameStateData _gameStateData;
        private bool _isProcessingRound;
        private int _retryAttempts = 3;
        
        public GameState CurrentGameState => _currentState;
        public int PlayerCardCount => _gameStateData?.PlayerCardsCount ?? 0;
        public int OpponentCardCount => _gameStateData?.OpponentCardsCount ?? 0;
        
        public event Action<GameState> OnGameStateChanged;
        public event Action<GameRoundResultData> OnRoundComplete;
        
        public GameService(
            IFakeServerService server,
            AsyncOperationManager asyncManager,
            SignalBus signalBus)
        {
            _server = server;
            _asyncManager = asyncManager;
            _signalBus = signalBus;
        }
        
        public void Initialize()
        {
            Debug.Log("[GameService] Initialized");
            SetGameState(GameState.Idle);
        }
        
        public async UniTask StartNewGame()
        {
            if (_currentState == GameState.Playing || _currentState == GameState.Initializing)
            {
                Debug.LogWarning("[GameService] Game already in progress");
                return;
            }
            
            SetGameState(GameState.Initializing);
            
            try
            {
                var response = await ExecuteWithRetry(
                    () => _server.StartNewGame(),
                    "StartNewGame"
                );
                
                if (response.Success)
                {
                    // Get initial game state
                    var stateResponse = await _server.GetGameState();
                    if (stateResponse.Success)
                    {
                        _gameStateData = stateResponse.Data;
                    }
                    
                    SetGameState(GameState.Playing);
                    _signalBus.Fire(new GameStartEvent());
                    
                    Debug.Log("[GameService] New game started successfully");
                }
                else
                {
                    Debug.LogError($"[GameService] Failed to start game: {response.ErrorMessage}");
                    SetGameState(GameState.Idle);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameService] Exception starting game: {ex.Message}");
                SetGameState(GameState.Idle);
                throw;
            }
        }
        
        public async UniTask PlayNextRound()
        {
            if (_currentState != GameState.Playing)
            {
                Debug.LogWarning($"[GameService] Cannot play round in state: {_currentState}");
                return;
            }
            
            if (_isProcessingRound)
            {
                Debug.LogWarning("[GameService] Round already being processed");
                return;
            }
            
            _isProcessingRound = true;
            _signalBus.Fire(new RoundStartEvent());
            
            try
            {
                var response = await ExecuteWithRetry(
                    () => _server.DrawCard(),
                    "DrawCard"
                );
                
                if (response.Success && response.Data != null)
                {
                    var result = response.Data;
                    
                    // Update local game state
                    var stateResponse = await _server.GetGameState();
                    if (stateResponse.Success)
                    {
                        _gameStateData = stateResponse.Data;
                    }
                    
                    // Handle war scenario
                    if (result.Result == GameResult.War)
                    {
                        SetGameState(GameState.War);
                        _signalBus.Fire(new WarStartEvent(result.WarDetails));
                        
                        // Wait for war animation (TODO: make this configurable)
                        await UniTask.Delay(TimeSpan.FromSeconds(result.WarDetails?.EstimatedAnimationDuration ?? 3f));
                        
                        SetGameState(GameState.Playing);
                    }
                    
                    // Fire round complete event
                    OnRoundComplete?.Invoke(result);
                    _signalBus.Fire(new RoundCompleteSignal(result));
                    
                    // Check for game end
                    if (result.IsGameEnded)
                    {
                        HandleGameEnd(result);
                    }
                    else
                    {
                        SetGameState(GameState.RoundComplete);
                        
                        // Brief pause before allowing next round
                        await UniTask.Delay(TimeSpan.FromMilliseconds(500));
                        
                        if (_currentState == GameState.RoundComplete)
                        {
                            SetGameState(GameState.Playing);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[GameService] Failed to play round: {response.ErrorMessage}");
                    
                    // TODO: Show error to player
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameService] Exception playing round: {ex.Message}");
                
                // TODO: Handle error gracefully
            }
            finally
            {
                _isProcessingRound = false;
            }
        }
        
        public void PauseGame()
        {
            if (_currentState == GameState.Playing)
            {
                SetGameState(GameState.Paused);
                Debug.Log("[GameService] Game paused");
            }
        }
        
        public void ResumeGame()
        {
            if (_currentState == GameState.Paused)
            {
                SetGameState(GameState.Playing);
                Debug.Log("[GameService] Game resumed");
            }
        }
        
        public void EndGame()
        {
            if (_currentState == GameState.Idle || _currentState == GameState.GameOver)
            {
                return;
            }
            
            _asyncManager.ExecuteFireAndForget(
                async (cancellationToken) =>
                {
                    await _server.EndGame();
                },
                "EndGame"
            );
            
            SetGameState(GameState.GameOver);
            Debug.Log("[GameService] Game ended");
        }
        
        private void HandleGameEnd(GameRoundResultData finalResult)
        {
            SetGameState(GameState.GameOver);
            
            int winner = finalResult.Result == GameResult.PlayerWin ? 1 : 2;
            _signalBus.Fire(new GameEndEvent(winner));
            
            Debug.Log($"[GameService] Game Over! Winner: {(winner == 1 ? "Player" : "Opponent")}");
        }
        
        private void SetGameState(GameState newState)
        {
            if (_currentState == newState) return;
            
            var previousState = _currentState;
            _currentState = newState;
            
            Debug.Log($"[GameService] State changed: {previousState} -> {newState}");
            
            OnGameStateChanged?.Invoke(newState);
            _signalBus.Fire(new GameStateChangedEvent(newState));
        }
        
        private async UniTask<ServerResponseData<T>> ExecuteWithRetry<T>(
            Func<UniTask<ServerResponseData<T>>> operation,
            string operationName)
        {
            int attempts = 0;
            ServerResponseData<T> lastResponse = null;
            
            while (attempts < _retryAttempts)
            {
                attempts++;
                
                try
                {
                    Debug.Log($"[GameService] Executing {operationName} (attempt {attempts}/{_retryAttempts})");
                    
                    lastResponse = await _asyncManager.ExecuteSafeAsync(
                        async (cancellationToken) => await operation(),
                        operationName: operationName
                    );
                    
                    if (lastResponse.Success)
                    {
                        if (attempts > 1)
                        {
                            Debug.Log($"[GameService] {operationName} succeeded after {attempts} attempts");
                        }
                        return lastResponse;
                    }
                    
                    // Check if error is retryable
                    if (!IsRetryableError(lastResponse.ErrorMessage))
                    {
                        Debug.LogWarning($"[GameService] Non-retryable error: {lastResponse.ErrorMessage}");
                        return lastResponse;
                    }
                    
                    if (attempts < _retryAttempts)
                    {
                        float retryDelay = GetRetryDelay(attempts);
                        Debug.Log($"[GameService] Retrying {operationName} in {retryDelay:F1}s...");
                        await UniTask.Delay(TimeSpan.FromSeconds(retryDelay));
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.Log($"[GameService] {operationName} was cancelled");
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameService] {operationName} exception: {ex.Message}");
                    
                    if (attempts < _retryAttempts)
                    {
                        float retryDelay = GetRetryDelay(attempts);
                        await UniTask.Delay(TimeSpan.FromSeconds(retryDelay));
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            // All attempts failed
            Debug.LogError($"[GameService] {operationName} failed after {_retryAttempts} attempts");
            return lastResponse ?? new ServerResponseData<T>(default, false, "Maximum retry attempts exceeded");
        }
        
        private bool IsRetryableError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage)) return false;
            
            var retryableKeywords = new[] { "timeout", "network", "connection", "unavailable" };
            var lowerError = errorMessage.ToLower();
            
            foreach (var keyword in retryableKeywords)
            {
                if (lowerError.Contains(keyword))
                    return true;
            }
            
            return false;
        }
        
        private float GetRetryDelay(int attemptNumber)
        {
            // Exponential backoff with jitter
            float baseDelay = 1.0f * Mathf.Pow(2, attemptNumber - 1);
            float jitter = UnityEngine.Random.Range(-0.2f, 0.2f) * baseDelay;
            return Mathf.Min(baseDelay + jitter, 10f);
        }
        
        public void Dispose()
        {
            Debug.Log("[GameService] Disposing");
            
            OnGameStateChanged = null;
            OnRoundComplete = null;
            
            if (_currentState == GameState.Playing)
            {
                EndGame();
            }
        }
    }
}