using System;
using UnityEngine;
using CardWar.Services;
using CardWar.Game.Logic;
using CardWar.Common;
using CardWar.Core;

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
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action<bool> OnGameOver;

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            _gameStateService = ServiceLocator.Instance.Get<IGameStateService>();
            
            Debug.Log("[GameController] Initialized");
        }
        
        #endregion

        #region IGameControllerService Implementation

        public void StartNewGame()
        {
            Debug.Log("[GameController] Starting new game");
            _isGameActive = true;
            _isPaused = false;
            InitializeGame();
        }

        private void InitializeGame()
        {
            Debug.Log("[GameController] Game initialized - Ready to play");
        }

        public void DrawNextCards()
        {
            if (!_isGameActive || _isPaused)
            {
                Debug.LogWarning("[GameController] Cannot draw cards - game not active or paused");
                return;
            }
            
            Debug.Log("[GameController] Drawing next cards");
            OnCardsDrawn?.Invoke();
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
            
            _gameStateService?.ChangeState(GameState.GameEnded);
        }

        public void ResetGame()
        {
            Debug.Log("[GameController] Resetting game");
            _isGameActive = false;
            _isPaused = false;
        }

        #endregion

        #region Private Methods

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