using System;
using CardWar.Common;
using UnityEngine;
using CardWar.Services;
using CardWar.Core;
using CardWar.Game.Logic;
using Cysharp.Threading.Tasks;
using Zenject;

namespace CardWar.Game
{
    public class GameController : MonoBehaviour, IGameControllerService, IDisposable
    {
        private IDIService _diService;
        private IGameStateService _gameStateService;
        private IUIService _uiService;
        private IAssetService _assetService;
        private GameSettings _gameSettings;
        
        private bool _isInitialized;
        private bool _isGameActive;

        public event Action<RoundData> OnRoundStarted;
        public event Action OnCardsDrawn;
        public event Action<RoundResult> OnRoundCompleted;
        public event Action<int> OnWarStarted;
        public event Action OnWarCompleted;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action<bool> OnGameOver;

        #region Initialization

        [Inject]
        public void Construct(IDIService diService, IGameStateService gameStateService, 
            IUIService uiService, IAssetService assetService, GameSettings gameSettings)
        {
            _diService = diService;
            _gameStateService = gameStateService;
            _uiService = uiService;
            _assetService = assetService;
            _gameSettings = gameSettings;
            
            _uiService.RegisterResetCallback(ResetGame);
            
            _isInitialized = true;
            Debug.Log($"[GameController] Initialized with all dependencies");
        }

        #endregion

        #region Game Control

        public void StartNewGame()
        {
            Debug.Log($"[GameController] Starting new game");
            _isGameActive = true;
            
            SimulateGameStart().Forget();
        }

        public void DrawNextCards()
        {
            throw new NotImplementedException();
        }

        private async UniTaskVoid SimulateGameStart()
        {
            await UniTask.Delay(1000);
            Debug.Log($"[GameController] Game started - ready to play");
            
            _gameStateService.ChangeState(GameState.Playing);
        }

        public void PauseGame()
        {
            if (!_isGameActive) return;
            
            Debug.Log($"[GameController] Game paused");
            OnGamePaused?.Invoke();
            _gameStateService.ChangeState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (!_isGameActive) return;
            
            Debug.Log($"[GameController] Game resumed");
            OnGameResumed?.Invoke();
            _gameStateService.ChangeState(GameState.Playing);
        }

        public void ReturnToMenu()
        {
            throw new NotImplementedException();
        }

        public void EndGame(bool playerWon)
        {
            _isGameActive = false;
            Debug.Log($"[GameController] Game ended - Player {(playerWon ? "won" : "lost")}");
            
            OnGameOver?.Invoke(playerWon);
            _gameStateService.ChangeState(GameState.GameEnded);
        }

        private void ResetGame()
        {
            Debug.Log($"[GameController] Resetting game");
            _isGameActive = false;
        }

        #endregion

        #region Unity Lifecycle

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _isGameActive)
            {
                PauseGame();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _isGameActive)
            {
                PauseGame();
            }
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            _isGameActive = false;
            Debug.Log($"[GameController] Disposed");
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion
    }
}