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


        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            _gameStateService = ServiceLocator.Instance.Get<IGameStateService>();
            _assetService = ServiceLocator.Instance.Get<IAssetService>();
            
            var uiService = ServiceLocator.Instance.Get<IUIService>();

            _playAreaParent = uiService.GetGameAreaParent();
        }
        
        #endregion

        
        #region Game Creation

        public async UniTask CreateNewGame()
        {
            Debug.Log("[GameController] Creating new game");

            var playAreaPrefab =
                await _assetService.LoadAssetAsync<GameBoardController>(GameSettings.PLAY_AREA_ASSET_PATH);

            if (playAreaPrefab == null || _playAreaParent == null)
            {
                Debug.LogError("[GameController] Failed to load play area asset");
                return;
            }
            
            _boardController = Instantiate(playAreaPrefab, _playAreaParent.transform);
            
            //TODO: Get Game Entry Data from FakeServer 
            
            if (_boardController == null)
            {
                Debug.LogError("[GameController] Failed to load play area asset - Try again later.");
                return;
            }
            
            _boardController.Initialize();
            
            _isGameActive = true;
            _isPaused = true;
            
            RegisterDrawButton();
            RegisterGameEvents();
        }

        private void RegisterDrawButton()
        {
            _boardController.OnDrawButtonPressed += OnDrawButtonPressed;
        }

        private void RegisterGameEvents()
        {
            _gameStateService.GameStateChanged += HandleGameStateChanged;
        }

        #endregion Game Creation
        
        
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
            DrawNextCards().Forget();
        }

        #endregion Event Handlers


        #region Public Methods

        public async UniTask DrawNextCards()
        {
            if (!_isGameActive || _isPaused)
            {
                Debug.LogWarning("[GameController] Cannot draw cards - game not active or paused");
                return;
            }
            
            Debug.Log("[GameController] Drawing next cards");
            CardsDrawnEvent?.Invoke();
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
            GamePausedEvent?.Invoke();
        }

        private void ResumeGame()
        {
            if (!_isGameActive || !_isPaused) return;
            
            _isPaused = false;
            Debug.Log("[GameController] Game resumed");
            GameResumedEvent?.Invoke();
        }

        private void EndGame(GameStatus playerWon)
        {
            if (!_isGameActive) return;
            
            _isGameActive = false;
            _isPaused = false;
            
            Debug.Log($"[GameController] Game ended - Winner: {playerWon}");
            GameOverEvent?.Invoke(playerWon);
        }

        public void ResetGame()
        {
            Debug.Log("[GameController] Resetting game");
            _isGameActive = false;
            _isPaused = false;
            
            // TODO: Restart Game
        }

        private void DestroyGame()
        {
            Debug.Log("[GameController] Destroying game");
            if (_boardController != null)
                Destroy(_boardController);
        }

        #endregion Public Methods

  
        #region Private Methods
        
        private async UniTask PlayRoundWithAnimation(RoundData roundData)
        {
            if (_boardController != null)
            {
                if (roundData.IsWar)
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

        #endregion

        
        #region Cleanup

        private void UnregisterDrawButton()
        {
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