using System;
using UnityEngine;
using CardWar.Services;
using CardWar.Game.Logic;
using CardWar.Common;
using Zenject;

namespace CardWar.Game
{
    public class GameController : MonoBehaviour, IGameControllerService, IDisposable
    {
        private IDIService _diService;
        private IGameStateService _gameStateService;
        private IUIService _uiService;
        
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

        [Inject]
        public void Initialize(IDIService diService, IGameStateService gameStateService, IUIService uiService)
        {
            _diService = diService;
            _gameStateService = gameStateService;
            _uiService = uiService;
            
            _diService.RegisterService<IGameControllerService>(this);

            RegisterResetCallback();
        }

        private void RegisterResetCallback()
        {
            _uiService?.RegisterResetCallback(ResetGame);
        }

        public void StartNewGame()
        {
            Debug.Log("Starting new game");
            _isGameActive = true;
            _isPaused = false;
            InitializeGame();
        }

        private void InitializeGame()
        {
            Debug.Log("Game initialized - Ready to play");
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
            Debug.Log("Game paused");
            OnGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            if (!_isGameActive || !_isPaused) return;
            
            _isPaused = false;
            Debug.Log("Game resumed");
            OnGameResumed?.Invoke();
        }

        public void ReturnToMenu()
        {
            ResetGame();
            Debug.Log("Returned to menu");
        }

        private void ResetGame()
        {
            _isGameActive = false;
            _isPaused = false;
            Debug.Log("Game reset");
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

        public void Dispose()
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

        private void OnDestroy()
        {
            Dispose();
        }
    }
}