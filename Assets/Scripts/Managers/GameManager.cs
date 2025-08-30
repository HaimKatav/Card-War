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
            
            CreateServices();
            
            await UniTask.DelayFrame(1);
            
            await LoadUIManager();
            
            SetupStateMachine();
            SetupEventSubscriptions();
            
            _isInitialized = true;
            
            Debug.Log($"[GameManager] Initialization complete");
            
            NotifyStartupComplete();
            
            await UniTask.Delay(100);
            ChangeState(GameState.MainMenu);
        }

        private void CreateServices()
        {
            _assetManager = CreateService<AssetManager>("AssetManager");
            _audioManager = CreateService<AudioManager>("AudioManager");
            _gameController = CreateService<GameController>("GameController");
            
            ServiceLocator.Instance.Register<IAssetService>(_assetManager);
            ServiceLocator.Instance.Register<IAudioService>(_audioManager);
            ServiceLocator.Instance.Register<IGameControllerService>(_gameController);
        }

        private T CreateService<T>(string name) where T : Component
        {
            var serviceObject = new GameObject(name);
            serviceObject.transform.SetParent(transform);
            return serviceObject.AddComponent<T>();
        }

        private async UniTask LoadUIManager()
        {
            if (_gameSettings == null)
            {
                Debug.LogError("[GameManager] GameSettings not assigned!");
                return;
            }
            
            var uiPrefab = await _assetManager.LoadAssetAsync<UIManager>("Prefabs/GameCanvas");
            if (uiPrefab != null)
            {
                var uiObject = Instantiate(uiPrefab);
            }
            else
            {
                Debug.LogWarning("[GameManager] UIManager prefab not found, creating new");
            }
        }

        #endregion

        #region State Machine Setup

        private void SetupStateMachine()
        {
            _stateMachine = new GameStateMachine();
            
            RegisterFirstLoadState();
            RegisterMainMenuState();
            RegisterLoadingGameState();
            RegisterPlayingState();
            RegisterPausedState();
            RegisterGameEndedState();
            RegisterReturnToMenuState();
            
            _stateMachine.OnStateChanged += HandleStateChanged;
            
            Debug.Log($"[GameManager] State machine configured");
        }

        private void RegisterFirstLoadState()
        {
            _stateMachine.RegisterState(GameState.FirstLoad,
                onEnter: () => 
                {
                    Debug.Log("[GameManager] Entering FirstLoad state");
                },
                onExit: () => 
                {
                    Debug.Log("[GameManager] Exiting FirstLoad state");
                }
            );
        }

        private void RegisterMainMenuState()
        {
            _stateMachine.RegisterState(GameState.MainMenu,
                onEnter: () => 
                {
                    Debug.Log("[GameManager] Entering MainMenu state");
                    _uiManager?.ShowMainMenu(true);
                    _uiManager?.ToggleLoadingScreen(false);
                    _uiManager?.ShowGameUI(false);
                    _uiManager?.ToggleGameOverScreen(false, false);
                },
                onExit: () => 
                {
                    Debug.Log("[GameManager] Exiting MainMenu state");
                    _uiManager?.ShowMainMenu(false);
                }
            );
        }

        private void RegisterLoadingGameState()
        {
            _stateMachine.RegisterState(GameState.LoadingGame,
                onEnter: () => 
                {
                    Debug.Log("[GameManager] Entering LoadingGame state");
                    _uiManager?.ToggleLoadingScreen(true);
                    _uiManager?.ShowMainMenu(false);
                    _gameController?.StartNewGame();
                    SimulateLoading().Forget();
                },
                onExit: () => 
                {
                    Debug.Log("[GameManager] Exiting LoadingGame state");
                    _uiManager?.ToggleLoadingScreen(false);
                }
            );
        }

        private void RegisterPlayingState()
        {
            _stateMachine.RegisterState(GameState.Playing,
                onEnter: () => 
                {
                    Debug.Log("[GameManager] Entering Playing state");
                    _uiManager?.ShowGameUI(true);
                    _uiManager?.ToggleLoadingScreen(false);
                },
                onExit: () => 
                {
                    Debug.Log("[GameManager] Exiting Playing state");
                }
            );
        }

        private void RegisterPausedState()
        {
            _stateMachine.RegisterState(GameState.Paused,
                onEnter: () => 
                {
                    Debug.Log("[GameManager] Entering Paused state");
                    _gameController?.PauseGame();
                },
                onExit: () => 
                {
                    Debug.Log("[GameManager] Exiting Paused state");
                    _gameController?.ResumeGame();
                }
            );
        }

        private void RegisterGameEndedState()
        {
            _stateMachine.RegisterState(GameState.GameEnded,
                onEnter: () => 
                {
                    Debug.Log("[GameManager] Entering GameEnded state");
                    _uiManager?.ShowGameUI(false);
                },
                onExit: () => 
                {
                    Debug.Log("[GameManager] Exiting GameEnded state");
                    _uiManager?.ToggleGameOverScreen(false, false);
                }
            );
        }

        private void RegisterReturnToMenuState()
        {
            _stateMachine.RegisterState(GameState.ReturnToMenu,
                onEnter: () => 
                {
                    Debug.Log("[GameManager] Entering ReturnToMenu state");
                    _gameController?.ResetGame();
                    _uiManager?.ShowGameUI(false);
                    _uiManager?.ToggleGameOverScreen(false, false);
                    DelayedStateChange(GameState.MainMenu, 100).Forget();
                },
                onExit: () => 
                {
                    Debug.Log("[GameManager] Exiting ReturnToMenu state");
                }
            );
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
            if (_gameController != null)
            {
                _gameController.OnGameOver += HandleGameOver;
            }
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

        public void ChangeState(GameState newState)
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

        private void HandleStateChanged(GameState newState, GameState previousState)
        {
            OnGameStateChanged?.Invoke(newState, previousState);
        }

        private void HandleGameOver(bool playerWon)
        {
            _uiManager?.ToggleGameOverScreen(true, playerWon);
            ChangeState(GameState.GameEnded);
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (_stateMachine != null)
            {
                _stateMachine.OnStateChanged -= HandleStateChanged;
                _stateMachine.Clear();
            }
            
            if (_gameController != null)
            {
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