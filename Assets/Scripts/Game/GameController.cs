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
        private AnimationDataBundle _animationDataBundle;
        
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

        #region Public Properties
        
        public GameServerHandler ServerHandler => _serverHandler;
        public bool IsGameActive => _isGameActive;
        public bool IsProcessingRound => _isProcessingRound;
        public bool IsInWar => _isInWar;
        
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
            _animationDataBundle = AnimationDataBundle.CreateFromSettings(_gameSettings.AnimationSettings);
            
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
            
            var boardPrefab = await _assetService.LoadAssetAsync<GameBoardController>(
                GameSettings.PLAY_AREA_ASSET_PATH
            );
            
            if (boardPrefab == null || _playAreaParent == null)
            {
                Debug.LogError("[GameController] Failed to load play area asset");
                return false;
            }
            
            var serverSuccess = await _serverHandler.InitializeNewGame();
            if (!serverSuccess)
            {
                Debug.LogError("[GameController] Failed to initialize server");
                return false;
            }
            
            var boardObject = Instantiate(boardPrefab, _playAreaParent.transform);
            _boardController = boardObject;
            
            if (_boardController == null)
            {
                Debug.LogError("[GameController] Failed to create board controller");
                Destroy(boardObject.gameObject);
                return false;
            }
            
            InitializeBoard();
            
            _isGameActive = true;
            _isPaused = true;
            _isProcessingRound = false;
            _isInWar = false;
            
            await PlayInitialSetup();
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
            
            await PlayBattleAnimations(roundData);
            
            if (roundData.IsWar)
            {
                _isInWar = true;
                WarStartedEvent?.Invoke(1);
                Debug.Log("[GameController] WAR started - will auto-continue");
                
                // FIXED: Reset processing flag before war continuation
                _isProcessingRound = false;
                
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
            
            await PlayWarAnimations(warData);
            
            if (warData.HasChainedWar)
            {
                WarStartedEvent?.Invoke(2);
                Debug.Log("[GameController] Chained WAR - will auto-continue");
                
                // FIXED: Reset processing flag before chained war continuation  
                _isProcessingRound = false;
                
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
            var timingConfig = _animationDataBundle.Timing;
            
            Debug.Log("[GameController] Playing initial setup animation");
            await UniTask.Delay((int)(timingConfig.RoundStartDelay * 1000));
            
            await _boardController.ShowInitialDeckSetup();
            
            Debug.Log("[GameController] Initial setup complete");
        }
        
        private async UniTask PlayBattleAnimations(RoundData roundData)
        {
            var battleConfig = _animationDataBundle.Battle;
            var roundDelay = _animationDataBundle.Timing.RoundEndDelay;
            
            await _boardController.DrawBattleCards(roundData);
            
            await UniTask.Delay((int)(battleConfig.PreBattleDelay * 1000));
            
            await _boardController.FlipBattleCards();
            
            await UniTask.Delay((int)(roundDelay * 1000));
            
            if (!roundData.IsWar)
            {
                await _boardController.HighlightWinner(roundData.Result);
                
                await UniTask.Delay((int)(battleConfig.PostBattleDelay * 1000));
                
                await _boardController.CollectBattleCards(roundData.Result);
            }
        }
        
        private async UniTask PlayWarAnimations(RoundData warData)
        {
            var sequenceDelay = _animationDataBundle.War.SequenceDelay;
            var timing = _animationDataBundle.Timing;
            
            await _boardController.PlaceWarCards(warData);
            
            await UniTask.Delay((int)(sequenceDelay * 1000));
            await _boardController.RevealWarCards();
            
            await UniTask.Delay((int)(timing.RoundEndDelay * 1000));
            
            if (!warData.HasChainedWar)
            {
                await _boardController.RevealAllWarCards();
                await UniTask.Delay((int)(sequenceDelay * 1000));
                
                await _boardController.CollectWarCards(warData.Result);
            }
        }
        
        #endregion
        
        #region Board Management
        
        private void InitializeBoard()
        {
            _boardController.Initialize(_animationDataBundle);
            
            var poolConfig = _animationDataBundle.CardPool;
            
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
        }
        
        private void HandleWarResolved(RoundData warData)
        {
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
            _boardController?.PauseAnimationsWithTransition();
            
            GamePausedEvent?.Invoke();
            Debug.Log("[GameController] Game paused");
        }
        
        private void ResumeGame()
        {
            if (!_isGameActive || !_isPaused) return;
            
            _isPaused = false;
            _boardController?.ResumeAnimationsWithTransition();
            
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
        
        #endregion State Management
        
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
        
        #endregion Utility
        
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

            if (_animationDataBundle != null)
            {
                
            }
            
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
        
        #endregion Cleanup
    }
}