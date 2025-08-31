using System;
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
        private IGameStateService _gameStateService;
        
        private bool _isPaused;
        private bool _isGameActive;
        private bool _isProcessingRound;
        private bool _isInWar;
        
        public event Action<RoundData> RoundStartedEvent;
        public event Action CardsDrawnEvent;
        public event Action<RoundResult> RoundCompletedEvent;
        public event Action<int> WarStartedEvent;
        public event Action WarCompletedEvent;
        public event Action GamePausedEvent;
        public event Action GameResumedEvent;
        public event Action<GameStatus> GameOverEvent;

        private GameBoardController _boardController;
        private GameObject _playAreaParent;
        private IAssetService _assetService;
        private FakeWarServer _warServer;
        private GameSettings _gameSettings;

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
            
            var success = await _warServer.InitializeNewGame();
            
            if (!success)
            {
                Debug.LogError("[GameController] Failed to initialize FakeWarServer");
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
            var roundData = await _warServer.DrawCards();
            
            if (roundData == null)
            {
                Debug.LogError("[GameController] Failed to draw cards from server");
                _isProcessingRound = false;
                return;
            }
            
            Debug.Log($"[GameController] Round {_warServer.RoundNumber}: Player {roundData.PlayerCard.Rank} vs Opponent {roundData.OpponentCard.Rank}");
            
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
            var warData = await _warServer.ResolveWar();
            
            if (warData == null)
            {
                Debug.LogError("[GameController] Failed to resolve war");
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

        #endregion

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