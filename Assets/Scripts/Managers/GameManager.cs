using System;
using UnityEngine;
using CardWar.Services;
using CardWar.Core;
using CardWar.Common;
using CardWar.Game;
using Cysharp.Threading.Tasks;

namespace CardWar.Managers
{
    public class GameManager : BaseService, IGameStateService
    {
        private GameStateMachine _stateMachine;
        private AssetManager _assetManager;
        private AudioManager _audioManager;
        private UIManager _uiManager;
        private GameController _gameController;

        public event Action<GameState> GameStateChanged;
        public event Action<float, string> OnLoadingProgress;
        public GameStatus MatchStatus { get; private set; } = GameStatus.NotStarted;

        private GameState CurrentState => _stateMachine?.CurrentStateType ?? GameState.FirstLoad;
        
        private float _loadingProgress;


        #region Initialization

        protected override void Awake()
        {
            ServiceLocator.Instance.Dispose();
            base.Awake();
            DontDestroyOnLoad(gameObject);
            InitializeGame().Forget();
        }

        private async UniTaskVoid InitializeGame()
        {
            Debug.Log($"[GameManager] Starting initialization");
            
            await CreateServices();
            
            await UniTask.DelayFrame(1);
            
            SetupStateMachine();
            RegisterEvents();
            
            Debug.Log($"[GameManager] Initialization complete");
            
            ChangeState(GameState.MainMenu);
        }

        private async UniTask CreateServices()
        {
            _assetManager = CreateService<AssetManager>("AssetManager");
            _audioManager = CreateService<AudioManager>("AudioManager");
            
            var uiPrefab = await _assetManager.LoadAssetAsync<UIManager>(GameSettings.UI_MANAGER_ASSET_PATH);
            _uiManager = Instantiate(uiPrefab);
            
            _gameController = CreateService<GameController>("GameController", false);
        }

        private T CreateService<T>(string serviceName, bool dontDestroy = true) where T : Component
        {
            var serviceObject = new GameObject(serviceName);
            
            if (dontDestroy) 
                serviceObject.transform.SetParent(transform);
            
            return serviceObject.AddComponent<T>();
        }

        private void RegisterEvents()
        {
            _gameController.GameOverEvent += HandleGameOverEvent;

            _uiManager.StartButtonPressedEvent += HandleStartButtonPressed;
            _uiManager.RestartButtonPressedEvent += HandleRestartButtonPressed;
            _uiManager.MenuButtonPressedEvent += HandleMenuButtonPressed;
            _uiManager.PauseButtonPressedEvent += HandlePauseButtonPressed;
            _uiManager.ResumeButtonPressedEvent += HandleResumeButtonPressed;
        }
        
        #endregion Initialization

        
        #region State Machine Setup

        private void SetupStateMachine()
        {
            _stateMachine = new GameStateMachine();

            _stateMachine.RegisterState(GameState.FirstLoad);
            _stateMachine.RegisterState(GameState.MainMenu);
            _stateMachine.RegisterState(GameState.LoadingGame);
            _stateMachine.RegisterState(GameState.Playing);
            _stateMachine.RegisterState(GameState.Paused);
            _stateMachine.RegisterState(GameState.GameEnded);
        }
        
        #endregion
        
        
        #region State Handling
        
        private async UniTask LoadGame()
        {
            Debug.Log("[GameManager] Entering LoadingGame state");
    
            var result = await _gameController.CreateNewGame(UpdateLoadingProgress);
    
            if (!result)
            {
                Debug.Log("[GameManager] Failed to create new game - Returning to Main Menu");
                ChangeState(GameState.MainMenu);
                return;
            }
    
            await UniTask.Delay(200);
    
            ChangeState(GameState.Playing);
        }
        
        #endregion State Handling
        
        
        #region Unity Lifecycle

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && CurrentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && CurrentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }

        #endregion Unity Lifecycle

        
        #region IGameStateService Implementation

        private void ChangeState(GameState newState)
        {
            if (_stateMachine == null)
            {
                Debug.LogError($"[GameManager] State machine not initialized");
                return;
            }
            
            _stateMachine.ChangeState(newState);
            GameStateChanged?.Invoke(CurrentState);
        }

        private void UpdateLoadingProgress(float progress, string loadingMessage)
        {
            progress = Mathf.Clamp01(progress);
            OnLoadingProgress?.Invoke(progress, loadingMessage);
            
            if (progress >= 1f && CurrentState == GameState.LoadingGame)
            {
                DelayedStateChange(GameState.Playing, 500).Forget();
            }
        }

        private async UniTaskVoid DelayedStateChange(GameState newState, int delayMs)
        {
            await UniTask.Delay(delayMs);
            ChangeState(newState);
        }
        
        #endregion IGameStateService Implementation

        
        #region Event Handlers

        private void HandleStartButtonPressed()
        {
            LoadGame().Forget();
            ChangeState(GameState.LoadingGame);
        }

        private void HandleRestartButtonPressed()
        {
            _gameController.ResetGame();
            ChangeState(GameState.Playing);
        }
        
        private void HandleResumeButtonPressed() => ChangeState(GameState.Playing);
        private void HandlePauseButtonPressed() => ChangeState(GameState.Paused);
        private void HandleMenuButtonPressed() => ChangeState(GameState.MainMenu);
        
        private void HandleGameOverEvent(GameStatus playerWon)
        {
            MatchStatus = playerWon;
            ChangeState(GameState.GameEnded);
        }

        #endregion Event Handlers

        
        #region Cleanup

        private void UnregisterEvents()
        {
            if (_gameController != null)
            {
                _gameController.GameOverEvent -= HandleGameOverEvent;
            }

            if (_uiManager != null)
            {
                _uiManager.StartButtonPressedEvent -= HandleStartButtonPressed;
                _uiManager.RestartButtonPressedEvent -= HandleRestartButtonPressed;
                _uiManager.MenuButtonPressedEvent -= HandleMenuButtonPressed;
                _uiManager.PauseButtonPressedEvent -= HandlePauseButtonPressed;
                _uiManager.ResumeButtonPressedEvent -= HandleResumeButtonPressed;
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            _stateMachine?.Clear();

            UnregisterEvents();

            GameStateChanged = null;
            OnLoadingProgress = null;

            ServiceLocator.Instance.Dispose();
        }

        #endregion
    }
}