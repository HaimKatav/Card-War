using System;
using CardWar.Common;
using UnityEngine;
using CardWar.Services;
using CardWar.Core;
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
            
            ServiceLocator.Instance.Register<IAssetService>(_assetManager);
            ServiceLocator.Instance.Register<IAudioService>(_audioManager);
        }

        private T CreateService<T>(string name) where T : Component
        {
            var serviceObject = new GameObject(name);
            serviceObject.transform.SetParent(transform);
            return serviceObject.AddComponent<T>();
        }

        private async UniTask LoadUIManager()
        {
            var uiPrefab = await _assetManager.LoadAssetAsync<UIManager>(GameSettings.UI_MANAGER_ASSET_PATH);
            
            if (uiPrefab != null)
            {
                var uiObject = Instantiate(uiPrefab);
                    
                ServiceLocator.Instance.Register<IUIService>(_uiManager);
                Debug.Log("[GameManager] UIManager loaded");
            }
            else
            {
                Debug.LogWarning("[GameManager] UIManager prefab not found, creating new");
            }
        }
        
        private void SetupStateMachine()
        {
            _stateMachine = new GameStateMachine();
            
            var uiService = ServiceLocator.Instance.Get<IUIService>();
            var gameControllerService = ServiceLocator.Instance.Get<IGameControllerService>();
            var audioService = ServiceLocator.Instance.Get<IAudioService>();
            
            _stateMachine.RegisterState(new FirstLoadState(uiService, gameControllerService, audioService));
            _stateMachine.RegisterState(new MainMenuState(uiService, gameControllerService, audioService));
            _stateMachine.RegisterState(new LoadingGameState(uiService, gameControllerService, audioService, UpdateLoadingProgress));
            _stateMachine.RegisterState(new PlayingState(uiService, gameControllerService, audioService));
            _stateMachine.RegisterState(new PausedState(uiService, gameControllerService, audioService));
            _stateMachine.RegisterState(new GameEndedState(uiService, gameControllerService, audioService));
            _stateMachine.RegisterState(new ReturnToMenuState(uiService, gameControllerService, audioService, 
                () => ChangeState(GameState.MainMenu)));
            
            _stateMachine.OnStateChanged += HandleStateChanged;
            
            Debug.Log($"[GameManager] State machine configured");
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

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (_stateMachine != null)
            {
                _stateMachine.OnStateChanged -= HandleStateChanged;
            }
            
            OnGameStateChanged = null;
            OnLoadingProgress = null;
            OnClientStartupComplete = null;
            
            ServiceLocator.Instance.Clear();
        }

        #endregion
    }
}