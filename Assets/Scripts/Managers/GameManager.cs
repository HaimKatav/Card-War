using System;
using UnityEngine;
using CardWar.Services;
using CardWar.Core;
using CardWar.Common;
using CardWar.Game;
using Cysharp.Threading.Tasks;

namespace CardWar.Managers
{
    public class GameManager : MonoBehaviour, IGameStateService
    {
        [SerializeField] private GameSettings _gameSettings;
        
        private GameStateMachine _stateMachine;
        private AssetManager _assetManager;
        private AudioManager _audioManager;
        private UIManager _uiManager;
        private GameController _gameController;
        
        private bool _isInitialized;
        private float _loadingProgress;

        public GameState CurrentState => _stateMachine?.CurrentStateType ?? GameState.FirstLoad;
        public GameState PreviousState => _stateMachine?.PreviousStateType ?? GameState.FirstLoad;

        public event Action<GameState, GameState> OnGameStateChanged;
        public event Action<float> OnLoadingProgress;
        public event Action OnClientStartupComplete;

        #region Initialization

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            InitializeGame().Forget();
        }

        private async UniTaskVoid InitializeGame()
        {
            Debug.Log($"[GameManager] Starting initialization");
            
            ServiceLocator.Instance.Clear();
            ServiceLocator.Instance.Register<IGameStateService>(this);
            ServiceLocator.Instance.Register(_gameSettings);
            
            await CreateServices();
            
            await UniTask.DelayFrame(1);
            
            SetupStateMachine();
            SetupEventSubscriptions();
            
            _isInitialized = true;
            
            Debug.Log($"[GameManager] Initialization complete");
            
            NotifyStartupComplete();
            
            await UniTask.Delay(100);
            ChangeState(GameState.MainMenu);
        }

        private async UniTask CreateServices()
        {
            _assetManager = CreateService<AssetManager>("AssetManager");
            _audioManager = CreateService<AudioManager>("AudioManager");
            
            ServiceLocator.Instance.Register<IAssetService>(_assetManager);
            ServiceLocator.Instance.Register<IAudioService>(_audioManager);
            
            var uiPrefab = await _assetManager.LoadAssetAsync<UIManager>(GameSettings.UI_MANAGER_ASSET_PATH);
            _uiManager = Instantiate(uiPrefab);
            ServiceLocator.Instance.Register<IUIService>(_uiManager);
            
            _gameController = CreateService<GameController>("GameController", false);
            
        
            ServiceLocator.Instance.Register<IGameControllerService>(_gameController);
        }

        private T CreateService<T>(string name, bool dontDestroy = true) where T : Component
        {
            var serviceObject = new GameObject(name);
            
            if (dontDestroy) 
                serviceObject.transform.SetParent(transform);
            
            return serviceObject.AddComponent<T>();
        }

        #endregion

        #region State Machine Setup

        private void SetupStateMachine()
        {
            _stateMachine = new GameStateMachine();

            _stateMachine.RegisterState(GameState.FirstLoad, null, null);
            _stateMachine.RegisterState(GameState.MainMenu, OnMainMenuStateEnter, OnMainMenuStateExit);;
            _stateMachine.RegisterState(GameState.LoadingGame, OnLoadingStateEnter, OnLoadingStateExit);
            _stateMachine.RegisterState(GameState.Playing, OnPlayingStateEnter, OnPlayingStateExit);
            _stateMachine.RegisterState(GameState.Paused, OnPausedStateEnter, OnPausedStateExit);
            _stateMachine.RegisterState(GameState.GameEnded, OnGameEndedStateEnter, OnGameEndedStateExit);
            _stateMachine.RegisterState(GameState.ReturnToMenu, OnReturnToMenuStateEnter, OnReturnToMenuStateExit);
        }
        
        
        private void OnMainMenuStateEnter()
        {
            Debug.Log("[GameManager] Entering MainMenu state");
            _uiManager?.ToggleMainMenu(true);
            _uiManager?.ToggleLoadingScreen(false);
            _uiManager?.ToggleGameUI(false);
            _uiManager?.ToggleGameOverScreen(false, false);
        }

        private void OnMainMenuStateExit()
        {
            Debug.Log("[GameManager] Exiting MainMenu state");
            _uiManager?.ToggleMainMenu(false);
        }

        private void OnLoadingStateEnter()
        {
            Debug.Log($"[GameManager] Loading state entered");
            HandleLoadingStateEntered().Forget();
        }

        private async UniTask HandleLoadingStateEntered()
        {
            Debug.Log("[GameManager] Entering LoadingGame state");
            SimulateLoading().Forget();
            await _gameController.CreateNewGame();
            
            ChangeState(GameState.Playing);
        }

        private void OnLoadingStateExit()
        {
            Debug.Log($"[GameManager] Loading state exited");
        }

        private void OnPlayingStateEnter()
        {
            Debug.Log("[GameManager] Entering Playing state");
            _uiManager?.ToggleGameUI(true);
            _uiManager?.ToggleLoadingScreen(false);
            _gameController?.StartGame();
        }

        private void OnPlayingStateExit()
        {
            Debug.Log("[GameManager] Exiting Playing state");

        }

        private void OnPausedStateEnter()
        {
            Debug.Log("[GameManager] Entering Paused state");
            _gameController?.PauseGame();
        }

        private void OnPausedStateExit()
        {
            Debug.Log("[GameManager] Exiting Paused state");
            _gameController?.ResumeGame();
        }

        private void OnGameEndedStateEnter()
        {
            Debug.Log("[GameManager] Entering GameEnded state");
            _uiManager?.ToggleGameUI(false);
        }

        private void OnGameEndedStateExit()
        {
            Debug.Log("[GameManager] Exiting GameEnded state");
            _uiManager?.ToggleGameOverScreen(false, false);
        }

        private void OnReturnToMenuStateEnter()
        {
            Debug.Log("[GameManager] Entering ReturnToMenu state");
            _gameController?.ResetGame();
            _uiManager?.ToggleGameUI(false);
            _uiManager?.ToggleGameOverScreen(false, false);
            DelayedStateChange(GameState.MainMenu, 100).Forget();
        }

        private void OnReturnToMenuStateExit()
        {
            Debug.Log("[GameManager] Exiting ReturnToMenu state");

        }
        
        #endregion

        #region State Management

        private async UniTaskVoid SimulateLoading()
        {
            _loadingProgress = 0f;
            for (var i = 0; i <= 10; i++)
            {
                _loadingProgress = i / 10f;
                UpdateLoadingProgress(_loadingProgress);
                await UniTask.Delay(200);
            }
        }

        private void SetupEventSubscriptions()
        {
            if (_gameController == null)
            {
                Debug.LogError("[GameManager] Game Controller is null");
                return;
            }

            _gameController.OnGameCreated += HandleGameCreated;
            _gameController.OnGameOver += HandleGameOver;


            if (_uiManager == null)
            {
                Debug.LogError("[GameManager] No UI Manager assigned to GameManager");
                return;
            }

            _uiManager.OnStartButtonPressed += HandleStartButtonPressed;
            _uiManager.OnRestartButtonPressed += HandleRestartButtonPressed;
            _uiManager.OnMenuButtonPressed += HandleMenuButtonPressed;
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            _stateMachine?.Update();
        }

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

        #endregion

        #region IGameStateService Implementation

        private void ChangeState(GameState newState)
        {
            if (_stateMachine == null)
            {
                Debug.LogError($"[GameManager] State machine not initialized");
                return;
            }
            
            _stateMachine.ChangeState(newState);
        }

        public void UpdateLoadingProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            OnLoadingProgress?.Invoke(progress);
            
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

        public void NotifyStartupComplete()
        {
            Debug.Log($"[GameManager] Client startup complete");
            OnClientStartupComplete?.Invoke();
        }

        #endregion

        #region Event Handlers

        private void HandleGameCreated()
        {
            _uiManager?.ToggleGameUI(true);
        }
        
        private void HandleGameOver(bool playerWon)
        {
            _uiManager?.ToggleGameOverScreen(true, playerWon);
            ChangeState(GameState.GameEnded);
        }
        
        private void HandleStartButtonPressed()
        {
            Debug.Log("[GameManager] Start button pressed - changing to LoadingGame");
            ChangeState(GameState.LoadingGame);
        }
    
        private void HandleRestartButtonPressed()
        {
            Debug.Log("[GameManager] Restart button pressed - restarting game");
            ChangeState(GameState.LoadingGame);
        }
    
        private void HandleMenuButtonPressed()
        {
            Debug.Log("[GameManager] Menu button pressed - returning to menu");
            ChangeState(GameState.ReturnToMenu);
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            _stateMachine?.Clear();

            if (_gameController != null)
            {
                _gameController.OnGameCreated -= HandleGameCreated;
                _gameController.OnGameOver -= HandleGameOver;
            }

            OnGameStateChanged = null;
            OnLoadingProgress = null;
            OnClientStartupComplete = null;

            ServiceLocator.Instance.Clear();
        }

        #endregion
    }
}