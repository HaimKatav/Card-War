using System;
using UnityEngine;
using CardWar.Services;
using CardWar.Game.Logic;
using CardWar.Common;
using CardWar.Core;
using CardWar.Game.UI;
using CardWar.Animation.Data;
using Cysharp.Threading.Tasks;

namespace CardWar.Game
{
    public class GameController : MonoBehaviour, IGameControllerService
    {
        #region Events
        
        public event Action<RoundData> RoundStartedEvent;
        public event Action CardsDrawnEvent;
        public event Action<RoundResult> RoundCompletedEvent;
        public event Action<int> WarStartedEvent;
        public event Action WarCompletedEvent;
        public event Action GamePausedEvent;
        public event Action GameResumedEvent;
        public event Action<GameStatus> GameOverEvent;
        
        #endregion
        
        #region Dependencies
        
        private IGameStateService _gameStateService;
        private IAssetService _assetService;
        private IUIService _uiService;
        private IGameBoardController _boardController;
        private GameServerHandler _serverHandler;
        private AnimationConfigManager _animationConfig;
        
        #endregion
        
        #region State
        
        private GameObject _playAreaParent;
        private GameSettings _gameSettings;
        
        private bool _isPaused;
        private bool _isGameActive;
        private bool _isProcessingRound;
        private bool _isInWar;
        private bool _isInitialized;
        
        #endregion
        
        #region Initialization
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            if (_isInitialized) return;
            
            _gameStateService = ServiceLocator.Instance.Get<IGameStateService>();
            _assetService = ServiceLocator.Instance.Get<IAssetService>();
            _uiService = ServiceLocator.Instance.Get<IUIService>();
            _gameSettings = ServiceLocator.Instance.Get<GameSettings>();
            
            _animationConfig = new AnimationConfigManager(_assetService);
            _playAreaParent = _uiService.GetGameAreaParent();
            
            SetupServerHandler();
            RegisterGameEvents();
            
            _isInitialized = true;
            Debug.Log("[GameController] Initialized");
        }
        
        private void SetupServerHandler()
        {
            _serverHandler = gameObject.AddComponent<GameServerHandler>();
            _serverHandler.Initialize(_gameSettings);
            
            _serverHandler.OnServerInitialized += HandleServerInitialized;
            _serverHandler.OnCardsDrawn += HandleCardsDrawn;
            _serverHandler.OnWarResolved += HandleWarResolved;
            _serverHandler.OnGameStatusChanged += HandleGameStatusChanged;
            _serverHandler.OnServerError += HandleServerError;
        }
        
        private void RegisterGameEvents()
        {
            if (_gameStateService != null)
            {
                _gameStateService.GameStateChanged += HandleGameStateChange;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        public async UniTask<bool> CreateNewGame()
        {
            Debug.Log("[GameController] Creating new game");
            
            // Load board controller prefab
            var boardPrefab = await _assetService.LoadAssetAsync<GameBoardController>(
                GameSettings.PLAY_AREA_ASSET_PATH
            );
            
            if (boardPrefab == null || _playAreaParent == null)
            {
                Debug.LogError("[GameController] Failed to load play area asset");
                return false;
            }
            
            // Initialize server
            var serverSuccess = await _serverHandler.InitializeNewGame();
            if (!serverSuccess)
            {
                Debug.LogError("[GameController] Failed to initialize server");
                return false;
            }
            
            // Create board controller
            var boardObject = Instantiate(boardPrefab, _playAreaParent.transform);
            _boardController = boardObject as IGameBoardController;
            
            if (_boardController == null)
            {
                Debug.LogError("[GameController] Failed to create board controller");
                Destroy(boardObject.gameObject);
                return false;
            }
            
            // Initialize board
            InitializeBoard();
            
            // Set initial state
            _isGameActive = true;
            _isPaused = true;
            _isProcessingRound = false;
            _isInWar = false;
            
            // Play initial animation
            await PlayInitialSetup();
            
            // Fire initial round event
            await FireInitialRoundEvent();
            
            return true;
        }
        
        public async UniTask DrawNextCards()
        {
            if (!CanDrawCards())
            {
                LogDrawBlockedReason();
                return;
            }
            
            Debug.Log("[GameController] Drawing next cards");
            _isProcessingRound = true;
            
            try
            {
                if (_isInWar)
                {
                    var warData = await _serverHandler.ResolveWar();
                    if (warData != null)
                    {
                        await ProcessWarRound(warData);
                    }
                }
                else
                {
                    var roundData = await _serverHandler.DrawCards();
                    if (roundData != null)
                    {
                        await ProcessNormalRound(roundData);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameController] Error processing round: {e.Message}");
            }
            finally
            {
                // Always ensure we reset if not in war
                if (!_isInWar)
                {
                    _isProcessingRound = false;
                    Debug.Log("[GameController] Round processing complete");
                }
            }
        }
        
        public void ResetGame()
        {
            Debug.Log("[GameController] Resetting game");
            _isGameActive = false;
            _isPaused = false;
            _isProcessingRound = false;
            _isInWar = false;
        }
        
        #endregion
        
        #region Game Flow
        
        private async UniTask ProcessNormalRound(RoundData roundData)
        {
            CardsDrawnEvent?.Invoke();
            RoundStartedEvent?.Invoke(roundData);
            
            // Play animations
            await PlayBattleAnimations(roundData);
            
            // Handle result
            if (roundData.IsWar)
            {
                _isInWar = true;
                WarStartedEvent?.Invoke(1);
                Debug.Log("[GameController] WAR started - will auto-continue");
                
                // Auto-continue war after delay
                await UniTask.Delay(1000);
                if (_isInWar && !_isPaused)
                {
                    await DrawNextCards();
                }
            }
            else
            {
                RoundCompletedEvent?.Invoke(roundData.Result);
                _isProcessingRound = false;
                Debug.Log("[GameController] Normal round complete");
            }
        }
        
        private async UniTask ProcessWarRound(RoundData warData)
        {
            RoundStartedEvent?.Invoke(warData);
            
            // Play war animations
            await PlayWarAnimations(warData);
            
            // Handle result
            if (warData.HasChainedWar)
            {
                WarStartedEvent?.Invoke(2);
                Debug.Log("[GameController] Chained WAR - will auto-continue");
                
                // Auto-continue war after delay
                await UniTask.Delay(1000);
                if (_isInWar && !_isPaused)
                {
                    await DrawNextCards();
                }
            }
            else
            {
                _isInWar = false;
                WarCompletedEvent?.Invoke();
                RoundCompletedEvent?.Invoke(warData.Result);
                _isProcessingRound = false;
                Debug.Log("[GameController] War complete");
            }
        }
        
        #endregion
        
        #region Animation Control
        
        private async UniTask PlayInitialSetup()
        {
            var transitionConfig = _animationConfig.GetTransitionConfig();
            var timingConfig = _animationConfig.GetTimingConfig();
            
            Debug.Log("[GameController] Playing initial setup animation");
            await UniTask.Delay((int)(timingConfig.RoundStartDelay * 1000));
            await _boardController.ShowInitialDeckSetup(transitionConfig.FadeInDuration);
            Debug.Log("[GameController] Initial setup complete");
        }
        
        private async UniTask PlayBattleAnimations(RoundData roundData)
        {
            var battleConfig = _animationConfig.GetBattleConfig();
            var timingConfig = _animationConfig.GetTimingConfig();
            
            // Draw cards
            await _boardController.DrawBattleCards(
                roundData,
                battleConfig.DrawAnimation.Duration,
                battleConfig.DrawAnimation.EasingCurve
            );
            
            // Flip cards
            await UniTask.Delay((int)(battleConfig.PreBattleDelay * 1000));
            await _boardController.FlipBattleCards(
                battleConfig.RevealAnimation.Duration,
                battleConfig.RevealAnimation.DelayBetweenFlips,
                battleConfig.RevealAnimation.EasingCurve
            );
            
            // Only collect if not war
            if (!roundData.IsWar)
            {
                // Highlight winner
                var winnerConfig = _animationConfig.GetWinnerConfig();
                if (winnerConfig.EnableHighlight)
                {
                    await _boardController.HighlightWinner(
                        roundData.Result,
                        winnerConfig.ScaleMultiplier,
                        winnerConfig.ScaleDuration,
                        winnerConfig.TintColor
                    );
                }
                
                // Collect cards
                await UniTask.Delay((int)(timingConfig.RoundEndDelay * 1000));
                var collectionConfig = _animationConfig.GetCollectionConfig();
                await _boardController.CollectBattleCards(
                    roundData.Result,
                    collectionConfig.Duration,
                    collectionConfig.StaggerDelay,
                    collectionConfig.EasingCurve
                );
            }
        }
        
        private async UniTask PlayWarAnimations(RoundData warData)
        {
            var warConfig = _animationConfig.GetWarConfig();
            var timingConfig = _animationConfig.GetTimingConfig();
            
            if (warData.PlayerWarCards != null && warData.PlayerWarCards.Count > 0)
            {
                // Place war cards
                await _boardController.PlaceWarCards(
                    warData,
                    warConfig.FaceDownCardsPerPlayer,
                    warConfig.PlaceCardsAnimation.Duration,
                    warConfig.CardSpacing
                );
                
                // Reveal fighting cards
                await UniTask.Delay((int)(warConfig.RevealDelay * 1000));
                await _boardController.RevealWarCards(
                    warConfig.RevealAnimation.Duration,
                    warConfig.RevealAnimation.EasingCurve
                );
                
                // If war ends, collect all cards
                if (!warData.HasChainedWar)
                {
                    await UniTask.Delay((int)(timingConfig.RoundEndDelay * 1000));
                    await _boardController.RevealAllWarCards();
                    
                    await UniTask.Delay((int)(warConfig.SequenceDelay * 1000));
                    var collectionConfig = _animationConfig.GetCollectionConfig();
                    await _boardController.CollectWarCards(
                        warData.Result,
                        collectionConfig.Duration,
                        collectionConfig.StaggerDelay,
                        collectionConfig.EasingCurve
                    );
                }
            }
        }
        
        #endregion
        
        #region Board Management
        
        private void InitializeBoard()
        {
            _boardController.Initialize();
            
            var poolConfig = _animationConfig.GetCardPoolConfig();
            _boardController.SetupCardPool(
                poolConfig.InitialPoolSize,
                poolConfig.MaxPoolSize,
                poolConfig.PrewarmPool
            );
            
            _boardController.OnDrawButtonPressed += OnDrawButtonPressed;
            _boardController.OnRoundAnimationComplete += OnRoundAnimationComplete;
            
            Debug.Log($"[GameController] Board initialized with pool size: {poolConfig.InitialPoolSize}");
        }
        
        private async UniTask FireInitialRoundEvent()
        {
            var stats = await _serverHandler.GetGameStats();
            if (stats != null)
            {
                var initialRound = new RoundData
                {
                    RoundNumber = 0,
                    PlayerCardsRemaining = stats.PlayerCardCount,
                    OpponentCardsRemaining = stats.OpponentCardCount
                };
                RoundStartedEvent?.Invoke(initialRound);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleGameStateChange(GameState state)
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
                    DestroyGame();
                    break;
            }
        }
        
        private void OnDrawButtonPressed()
        {
            Debug.Log($"[GameController] Draw button pressed - Active: {_isGameActive}, Paused: {_isPaused}, Processing: {_isProcessingRound}");
            DrawNextCards().Forget();
        }
        
        private void OnRoundAnimationComplete()
        {
            Debug.Log("[GameController] Round animation complete event received");
        }
        
        private void HandleServerInitialized(bool success)
        {
            Debug.Log($"[GameController] Server initialized: {success}");
        }
        
        private void HandleCardsDrawn(RoundData roundData)
        {
            // Server has drawn cards, handled in ProcessNormalRound
        }
        
        private void HandleWarResolved(RoundData warData)
        {
            // Server has resolved war, handled in ProcessWarRound
        }
        
        private void HandleGameStatusChanged(GameStatus status)
        {
            EndGame(status);
        }
        
        private void HandleServerError(string error)
        {
            Debug.LogError($"[GameController] Server error: {error}");
            _isProcessingRound = false;
        }
        
        #endregion
        
        #region State Management
        
        private void StartGame()
        {
            _isGameActive = true;
            _isPaused = false;
            Debug.Log("[GameController] Game started");
        }
        
        private void PauseGame()
        {
            if (!_isGameActive || _isPaused) return;
            
            _isPaused = true;
            var transitionConfig = _animationConfig.GetTransitionConfig();
            _boardController?.PauseAnimationsWithTransition(transitionConfig.PauseFadeDuration);
            
            GamePausedEvent?.Invoke();
            Debug.Log("[GameController] Game paused");
        }
        
        private void ResumeGame()
        {
            if (!_isGameActive || !_isPaused) return;
            
            _isPaused = false;
            var transitionConfig = _animationConfig.GetTransitionConfig();
            _boardController?.ResumeAnimationsWithTransition(transitionConfig.PauseFadeDuration);
            
            GameResumedEvent?.Invoke();
            Debug.Log("[GameController] Game resumed");
        }
        
        private void EndGame(GameStatus status)
        {
            if (!_isGameActive) return;
            
            _isGameActive = false;
            _isPaused = false;
            _isProcessingRound = false;
            _isInWar = false;
            
            GameOverEvent?.Invoke(status);
            Debug.Log($"[GameController] Game ended: {status}");
        }
        
        private void DestroyGame()
        {
            Debug.Log("[GameController] Destroying game");
            
            if (_boardController != null)
            {
                var boardObject = _boardController as MonoBehaviour;
                if (boardObject != null)
                {
                    Destroy(boardObject.gameObject);
                }
                _boardController = null;
            }
            
            ResetGame();
        }
        
        #endregion
        
        #region Utility
        
        private bool CanDrawCards()
        {
            return _isGameActive && !_isPaused && !_isProcessingRound;
        }
        
        private void LogDrawBlockedReason()
        {
            if (!_isGameActive)
                Debug.Log("[GameController] Cannot draw - game not active");
            else if (_isPaused)
                Debug.Log("[GameController] Cannot draw - game is paused");
            else if (_isProcessingRound)
                Debug.Log("[GameController] Cannot draw - round still processing");
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            if (_boardController != null)
            {
                _boardController.OnDrawButtonPressed -= OnDrawButtonPressed;
                _boardController.OnRoundAnimationComplete -= OnRoundAnimationComplete;
            }
            
            if (_gameStateService != null)
            {
                _gameStateService.GameStateChanged -= HandleGameStateChange;
            }
            
            if (_serverHandler != null)
            {
                _serverHandler.OnServerInitialized -= HandleServerInitialized;
                _serverHandler.OnCardsDrawn -= HandleCardsDrawn;
                _serverHandler.OnWarResolved -= HandleWarResolved;
                _serverHandler.OnGameStatusChanged -= HandleGameStatusChanged;
                _serverHandler.OnServerError -= HandleServerError;
            }
            
            // Clear all events
            RoundStartedEvent = null;
            CardsDrawnEvent = null;
            RoundCompletedEvent = null;
            WarStartedEvent = null;
            WarCompletedEvent = null;
            GamePausedEvent = null;
            GameResumedEvent = null;
            GameOverEvent = null;
            
            _isInitialized = false;
        }
        
        #endregion
    }
}