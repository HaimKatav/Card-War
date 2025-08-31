using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Services;
using CardWar.Game.Logic;
using CardWar.Common;
using CardWar.Core;
using CardWar.Game.UI;
using Cysharp.Threading.Tasks;

namespace CardWar.Game
{
    public class GameController : MonoBehaviour, IGameControllerService
    {
        public event Action<RoundData> RoundStartedEvent;
        public event Action CardsDrawnEvent;
        public event Action<RoundResult> RoundCompletedEvent;
        public event Action<int> WarStartedEvent;
        public event Action WarCompletedEvent;
        public event Action GamePausedEvent;
        public event Action GameResumedEvent;
        public event Action<GameStatus> GameOverEvent;

        private IGameStateService _gameStateService;
        private IAssetService _assetService;
        
        private GameBoardController _boardController;
        private FakeWarServer _warServer;
        private GameObject _playAreaParent;
        private GameSettings _gameSettings;

        private const int MAX_RETRY_ATTEMPTS = 3;
        
        private bool _isPaused;
        private bool _isGameActive;
        private bool _isProcessingRound;
        private bool _isInWar;

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            _gameStateService = ServiceLocator.Instance.Get<IGameStateService>();
            _assetService = ServiceLocator.Instance.Get<IAssetService>();
            _gameSettings = ServiceLocator.Instance.Get<GameSettings>();
            
            var uiService = ServiceLocator.Instance.Get<IUIService>();
            _playAreaParent = uiService.GetGameAreaParent();
            
            _warServer = new FakeWarServer(_gameSettings);
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _isGameActive && !_isPaused)
            {
                PauseGame();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _isGameActive && !_isPaused)
            {
                PauseGame();
            }
        }
        
        #endregion

        #region Game Creation

        public async UniTask<bool> CreateNewGame()
        {
            Debug.Log("[GameController] Creating new game");

            var playAreaPrefab =
                await _assetService.LoadAssetAsync<GameBoardController>(GameSettings.PLAY_AREA_ASSET_PATH);

            if (playAreaPrefab == null || _playAreaParent == null)
            {
                Debug.LogError("[GameController] Failed to load play area asset");
                return false;
            }
            
            _boardController = Instantiate(playAreaPrefab, _playAreaParent.transform);
           
            var success = await ExecuteWithRetry(
                async () => await _warServer.InitializeNewGame(),
                "InitializeNewGame"
            );
            
            if (!success)
            {
                Debug.LogError("[GameController] Failed to initialize FakeWarServer after retries");
                return false;
            }
            
            if (_boardController == null)
            {
                Debug.LogError("[GameController] Failed to load play area asset - Try again later.");
                return false;
            }
            
            _boardController.Initialize();
            
            _isGameActive = true;
            _isPaused = true;
            _isProcessingRound = false;
            _isInWar = false;
            
            RegisterDrawButton();
            RegisterGameEvents();
            RegisterBoardEvents();
            
            var initialStats = await _warServer.GetGameStats();
            if (initialStats != null)
            {
                var initialRound = new RoundData
                {
                    RoundNumber = 0,
                    PlayerCardsRemaining = initialStats.PlayerCardCount,
                    OpponentCardsRemaining = initialStats.OpponentCardCount
                };
                RoundStartedEvent?.Invoke(initialRound);
            }
            
            return true;
        }

        private void RegisterDrawButton()
        {
            if (_boardController != null)
                _boardController.OnDrawButtonPressed += OnDrawButtonPressed;
        }

        private void RegisterGameEvents()
        {
            _gameStateService.GameStateChanged += HandleGameStateChanged;
        }

        private void RegisterBoardEvents()
        {
            if (_boardController != null)
                _boardController.OnRoundAnimationComplete += HandleRoundAnimationComplete;
        }

        #endregion

        #region Event Handlers
        
        private void HandleGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    if (_isGameActive) 
                        ResumeGame();
                    else
                        StartGame();
                    break;
                
                case GameState.Paused:
                    PauseGame();
                    break;
                
                case GameState.MainMenu:
                    UnregisterDrawButton();
                    DestroyGame();
                    break;
            }
        }

        private void OnDrawButtonPressed()
        {
            if (!_isProcessingRound)
            {
                DrawNextCards().Forget();
            }
        }

        private void HandleRoundAnimationComplete()
        {
            if (!_isInWar)
            {
                _isProcessingRound = false;
            }
            else
            {
                ContinueWarSequence().Forget();
            }
        }

        #endregion

        #region Public Methods

        public async UniTask DrawNextCards()
        {
            if (!_isGameActive || _isPaused || _isProcessingRound)
            {
                Debug.LogWarning("[GameController] Cannot draw cards - game not active, paused, or round in progress");
                return;
            }
            
            Debug.Log("[GameController] Drawing next cards");
            
            _isProcessingRound = true;
            
            try
            {
                if (_isInWar)
                {
                    await ProcessWarRound();
                }
                else
                {
                    await ProcessNormalRound();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameController] Error processing round: {e.Message}");
                _isProcessingRound = false;
                _isInWar = false;
            }
        }

        private async UniTaskVoid ContinueWarSequence()
        {
            await UniTask.Delay(1000);
            
            if (_isInWar && !_isPaused)
            {
                _isProcessingRound = true;
                await ProcessWarRound();
            }
        }

        private void StartGame()
        {
            _isGameActive = true;
            _isPaused = false;
            Debug.Log("[GameController] Starting game");
        }
        
        private void PauseGame()
        {
            if (!_isGameActive || _isPaused) return;
            
            _isPaused = true;
            Debug.Log("[GameController] Game paused");
            
            if (_boardController != null)
            {
                _boardController.PauseAnimations();
            }
            
            GamePausedEvent?.Invoke();
        }

        private void ResumeGame()
        {
            if (!_isGameActive || !_isPaused) return;
            
            _isPaused = false;
            Debug.Log("[GameController] Game resumed");
            
            if (_boardController != null)
            {
                _boardController.ResumeAnimations();
            }
            
            GameResumedEvent?.Invoke();
            
            if (_isInWar && !_isProcessingRound)
            {
                ContinueWarSequence().Forget();
            }
        }

        private void EndGame(GameStatus status)
        {
            if (!_isGameActive) return;
            
            _isGameActive = false;
            _isPaused = false;
            _isProcessingRound = false;
            _isInWar = false;
            
            Debug.Log($"[GameController] Game ended - Status: {status}");
            GameOverEvent?.Invoke(status);
        }

        public void ResetGame()
        {
            Debug.Log("[GameController] Resetting game");
            _isGameActive = false;
            _isPaused = false;
            _isProcessingRound = false;
            _isInWar = false;
        }

        private void DestroyGame()
        {
            Debug.Log("[GameController] Destroying game");
            if (_boardController != null)
            {
                Destroy(_boardController.gameObject);
                _boardController = null;
            }
            
            ResetGame();
        }

        #endregion

        #region Private Methods

       private async UniTask ProcessNormalRound()
        {
            var roundData = await ExecuteWithRetry(
                async () => await _warServer.DrawCards(),
                "DrawCards"
            );
            
            if (roundData == null)
            {
                Debug.LogError("[GameController] Failed to draw cards from server after retries");
                _isProcessingRound = false;
                return;
            }
            
            Debug.Log($"[GameController] Round {roundData.RoundNumber}: Player {roundData.PlayerCard.Rank} vs Opponent {roundData.OpponentCard.Rank}");
            
            CardsDrawnEvent?.Invoke();
            
            await PlayRoundWithAnimation(roundData);
            
            RoundStartedEvent?.Invoke(roundData);
            
            if (roundData.IsWar)
            {
                _isInWar = true;
                WarStartedEvent?.Invoke(1);
                Debug.Log("[GameController] WAR! Both players drew same rank");
            }
            else
            {
                RoundCompletedEvent?.Invoke(roundData.Result);
            }
            
            CheckGameStatus();
        }

        private async UniTask ProcessWarRound()
        {
            var warData = await ExecuteWithRetry(
                async () => await _warServer.ResolveWar(),
                "ResolveWar"
            );
            
            if (warData == null)
            {
                Debug.LogError("[GameController] Failed to resolve war after retries");
                _isProcessingRound = false;
                _isInWar = false;
                return;
            }
            
            Debug.Log($"[GameController] War resolved: Player {warData.PlayerCard.Rank} vs Opponent {warData.OpponentCard.Rank}");
            
            await PlayRoundWithAnimation(warData);
            
            RoundStartedEvent?.Invoke(warData);
            
            if (warData.HasChainedWar)
            {
                WarStartedEvent?.Invoke(2);
                Debug.Log("[GameController] CHAINED WAR! Another war required");
            }
            else
            {
                _isInWar = false;
                WarCompletedEvent?.Invoke();
                RoundCompletedEvent?.Invoke(warData.Result);
                _isProcessingRound = false;
            }
            
            CheckGameStatus();
        }
        
        private async UniTask PlayRoundWithAnimation(RoundData roundData)
        {
            if (_boardController != null)
            {
                if (roundData.IsWar && roundData.PlayerWarCards != null && roundData.PlayerWarCards.Count > 0)
                {
                    await _boardController.PlayWarSequence(roundData);
                }
                else
                {
                    await _boardController.PlayRound(roundData);
                }
            }
            else
            {
                Debug.LogWarning("[GameController] Animation controller not available");
            }
        }

        private void CheckGameStatus()
        {
            if (_warServer.Status == GameStatus.PlayerWon)
            {
                EndGame(GameStatus.PlayerWon);
            }
            else if (_warServer.Status == GameStatus.OpponentWon)
            {
                EndGame(GameStatus.OpponentWon);
            }
        }

        #endregion Private Methods

        
        #region Server Retry Methods
        
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
                            Debug.Log($"[GameController] {operationName} succeeded on attempt {attempt}");
                        }
                        return result;
                    }
            
                    Debug.LogWarning($"[GameController] {operationName} returned default value on attempt {attempt}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameController] {operationName} failed on attempt {attempt}: {e.Message}");
                }
        
                if (attempt < MAX_RETRY_ATTEMPTS)
                {
                    var retryDelay = (int)(_gameSettings.FakeNetworkDelay * 1000 * attempt);
                    Debug.Log($"[GameController] Retrying {operationName} in {retryDelay}ms...");
                    await UniTask.Delay(retryDelay);
                }
            }
    
            return default(T);
        }
        
        #endregion Server Methods
        
        #region Cleanup

        private void UnregisterDrawButton()
        {
            if (_boardController != null)
            {
                _boardController.OnDrawButtonPressed -= OnDrawButtonPressed;
                _boardController.OnRoundAnimationComplete -= HandleRoundAnimationComplete;
            }
                
            if (_gameStateService != null) 
                _gameStateService.GameStateChanged -= HandleGameStateChanged;
        }

        private void OnDestroy()
        {
            UnregisterDrawButton();
            
            RoundStartedEvent = null;
            CardsDrawnEvent = null;
            RoundCompletedEvent = null;
            WarStartedEvent = null;
            WarCompletedEvent = null;
            GamePausedEvent = null;
            GameResumedEvent = null;
            GameOverEvent = null;
        }

        #endregion
    }
}