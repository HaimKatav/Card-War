using System;
using UnityEngine;
using CardWar.Services;
using CardWar.Core;
using CardWar.Game;
using Cysharp.Threading.Tasks;
using Zenject;

namespace CardWar.Managers
{
    public class GameManager : MonoBehaviour, IGameStateService, IDisposable
    {
        private DiContainer _container;
        private IDIService _diService;
        private GameSettings _gameSettings;
        private IAssetService _assetService;
        
        private GameStateMachine _stateMachine;
        private IUIService _uiService;
        private IGameControllerService _gameController;
        private bool _isInitialized;

        public GameState CurrentState => _stateMachine?.CurrentStateType ?? GameState.FirstLoad;
        public GameState PreviousState => _stateMachine?.PreviousStateType ?? GameState.FirstLoad;

        public event Action<GameState, GameState> OnGameStateChanged;
        public event Action<float> OnLoadingProgress;
        public event Action OnClientStartupComplete;

        #region Initialization

        [Inject]
        public void Construct(DiContainer container, IDIService diService, GameSettings gameSettings, IAssetService assetService)
        {
            _container = container;
            _diService = diService;
            _gameSettings = gameSettings;
            _assetService = assetService;
            
            Debug.Log($"[GameManager] Constructed with dependencies");
        }

        private void Start()
        {
            CreateUIAndGameController().Forget();
        }

        private async UniTaskVoid CreateUIAndGameController()
        {
            Debug.Log($"[GameManager] Creating UI and GameController");
            
            await CreateUIManager();
            CreateGameController();
            
            SetupStateMachine();
            
            _isInitialized = true;
            OnClientStartupComplete?.Invoke();
            
            await UniTask.DelayFrame(1);
            ChangeState(GameState.MainMenu);
            
            Debug.Log($"[GameManager] Initialization complete");
        }

        private async UniTask CreateUIManager()
        {
            var uiPrefab = await _assetService.LoadAssetAsync<GameObject>(GameSettings.UI_MANAGER_ASSET_PATH);
            if (uiPrefab != null)
            {
                var uiManagerObject = _container.InstantiatePrefab(uiPrefab);
                _uiService = uiManagerObject.GetComponent<UIManager>();
                
                _container.Bind<IUIService>().FromInstance(_uiService).AsSingle();
                _diService.RegisterService<IUIService>(_uiService);
                
                Debug.Log($"[GameManager] UIManager created and registered");
            }
            else
            {
                Debug.LogError($"[GameManager] Failed to load UIManager prefab");
            }
        }

        private void CreateGameController()
        {
            var gameControllerObject = new GameObject("GameController");
            gameControllerObject.transform.SetParent(transform);
            
            _gameController = gameControllerObject.AddComponent<GameController>();
            var gameControllerComponent = _gameController as GameController;
            
            _container.Inject(gameControllerComponent);
            
            _container.Bind<IGameControllerService>().FromInstance(_gameController).AsSingle();
            _diService.RegisterService<IGameControllerService>(_gameController);
            
            Debug.Log($"[GameManager] GameController created and registered");
        }
        
        private void SetupStateMachine()
        {
            _stateMachine = new GameStateMachine();
            
            var audioService = _diService.GetService<IAudioService>();
            
            _stateMachine.RegisterState(new FirstLoadState(_uiService, _gameController, audioService));
            _stateMachine.RegisterState(new MainMenuState(_uiService, _gameController, audioService));
            _stateMachine.RegisterState(new LoadingGameState(_uiService, _gameController, audioService, UpdateLoadingProgress));
            _stateMachine.RegisterState(new PlayingState(_uiService, _gameController, audioService));
            _stateMachine.RegisterState(new PausedState(_uiService, _gameController, audioService));
            _stateMachine.RegisterState(new GameEndedState(_uiService, _gameController, audioService));
            
            _stateMachine.ChangeState(GameState.FirstLoad);
            
            Debug.Log($"[GameManager] State machine initialized");
        }

        #endregion

        #region State Management

        public void ChangeState(GameState newState)
        {
            if (!_isInitialized) return;
            
            var previousState = CurrentState;
            _stateMachine.ChangeState(newState);
            
            OnGameStateChanged?.Invoke(newState, previousState);
            Debug.Log($"[GameManager] State changed: {previousState} -> {newState}");
        }

        public void NotifyStartupComplete()
        {
            throw new NotImplementedException();
        }

        private void UpdateLoadingProgress(float progress)
        {
            OnLoadingProgress?.Invoke(progress);
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (_gameController != null && _gameController is IDisposable disposableController)
            {
                disposableController.Dispose();
            }
            
            if (_uiService != null && _uiService is IDisposable disposableUI)
            {
                disposableUI.Dispose();
            }
            
            Debug.Log($"[GameManager] Disposed");
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion
    }
}