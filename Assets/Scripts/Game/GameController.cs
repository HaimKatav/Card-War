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

        public event Action<RoundData> OnRoundStarted;
        public event Action OnCardsDrawn;
        public event Action<RoundResult> OnRoundCompleted;
        public event Action<int> OnWarStarted;
        public event Action OnWarCompleted;
        public event Action OnGameCreated;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action<bool> OnGameOver;

        private GameAnimationController _animationController;
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

        #region IGameControllerService Implementation

        public async UniTask CreateNewGame()
        {
            Debug.Log("[GameController] Creating new game");

            var playAreaPrefab =
                await _assetService.LoadAssetAsync<GameAnimationController>(GameSettings.PLAY_AREA_ASSET_PATH);

            if (playAreaPrefab == null || _playAreaParent == null)
            {
                Debug.LogError("[GameController] Failed to load play area asset");
                return;
            }
            
            _animationController = Instantiate(playAreaPrefab, _playAreaParent.transform);
            _animationController.Initialize();
            
            _isGameActive = true;
            _isPaused = true;
            
            OnGameCreated?.Invoke();
        }

        private void InitializeGame()
        {
            Debug.Log("[GameController] Game initialized - Ready to play");
        }

        public async UniTask DrawNextCards()
        {
            if (!_isGameActive || _isPaused)
            {
                Debug.LogWarning("[GameController] Cannot draw cards - game not active or paused");
                return;
            }
            
            Debug.Log("[GameController] Drawing next cards");
            OnCardsDrawn?.Invoke();
        }

        public void StartGame()
        {
            _isGameActive = true;
            _isPaused = false;
            Debug.Log("[GameController] Starting game");
        }
        
        public void PauseGame()
        {
            if (!_isGameActive || _isPaused) return;
            
            _isPaused = true;
            Debug.Log("[GameController] Game paused");
            OnGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            if (!_isGameActive || !_isPaused) return;
            
            _isPaused = false;
            Debug.Log("[GameController] Game resumed");
            OnGameResumed?.Invoke();
        }

        public void EndGame(bool playerWon)
        {
            if (!_isGameActive) return;
            
            _isGameActive = false;
            _isPaused = false;
            
            Debug.Log($"[GameController] Game ended - Player won: {playerWon}");
            OnGameOver?.Invoke(playerWon);
        }

        public void ResetGame()
        {
            Debug.Log("[GameController] Resetting game");
            _isGameActive = false;
            _isPaused = false;
        }

        #endregion

        #region Private Methods
        
        private async UniTask PlayRoundWithAnimation(RoundData roundData)
        {
            if (_animationController != null)
            {
                if (roundData.IsWar)
                {
                    await _animationController.PlayWarSequence(roundData);
                }
                else
                {
                    await _animationController.PlayRound(roundData);
                }
            }
            else
            {
                Debug.LogWarning("[GameController] Animation controller not available");
            }
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

        #region Cleanup

        private void OnDestroy()
        {
            OnRoundStarted = null;
            OnCardsDrawn = null;
            OnRoundCompleted = null;
            OnWarStarted = null;
            OnWarCompleted = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnGameOver = null;
        }

        #endregion
    }
}