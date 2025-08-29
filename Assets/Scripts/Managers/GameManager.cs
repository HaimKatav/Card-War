using System;
using UnityEngine;
using CardWar.Services;
using CardWar.Core;
using Cysharp.Threading.Tasks;

namespace CardWar.Managers
{
    public class GameManager : MonoBehaviour, IGameStateService, IDisposable
    {
        [Inject] private IDIService _diService;
        [Inject] private GameSettings _gameSettings;
        
        private GameStateMachine _stateMachine;
        private bool _isInitialized;

        public GameState CurrentState => _stateMachine?.CurrentStateType ?? GameState.FirstLoad;
        public GameState PreviousState => _stateMachine?.PreviousStateType ?? GameState.FirstLoad;

        public event Action<GameState, GameState> OnGameStateChanged;
        public event Action<float> OnLoadingProgress;
        public event Action OnClientStartupComplete;

        #region Initialization

        [Initialize(order: 1)]
        public void Initialize()
        {
            if (_isInitialized) return;
            
            ValidateDependencies();
            SetupStateMachine();
            _isInitialized = true;
            
            Debug.Log($"[{GetType().Name}] Initialized");
        }

        private void ValidateDependencies()
        {
            if (_diService == null)
                throw new InvalidOperationException($"[{GetType().Name}] DIService not injected");
            if (_gameSettings == null)
                throw new InvalidOperationException($"[{GetType().Name}] GameSettings not injected");
        }

        private void SetupStateMachine()
        {
            _stateMachine = new GameStateMachine();
            
            var uiService = _diService.GetService<IUIService>();
            var gameController = _diService.GetService<IGameControllerService>();
            var audioService = _diService.GetService<IAudioService>();
            
            _stateMachine.RegisterState(new FirstLoadState(uiService, gameController, audioService));
            _stateMachine.RegisterState(new MainMenuState(uiService, gameController, audioService));
            _stateMachine.RegisterState(new LoadingGameState(uiService, gameController, audioService, UpdateLoadingProgress));
            _stateMachine.RegisterState(new PlayingState(uiService, gameController, audioService));
            _stateMachine.RegisterState(new PausedState(uiService, gameController, audioService));
            _stateMachine.RegisterState(new GameEndedState(uiService, gameController, audioService));
            _stateMachine.RegisterState(new ReturnToMenuState(uiService, gameController, audioService, 
                () => ChangeState(GameState.MainMenu)));
            
            _stateMachine.OnStateChanged += HandleStateChanged;
            
            Debug.Log($"[{GetType().Name}] State machine configured");
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_isInitialized)
            {
                TransitionToMainMenu().Forget();
            }
        }

        private async UniTaskVoid TransitionToMainMenu()
        {
            await UniTask.Delay(100);
            ChangeState(GameState.MainMenu);
        }

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
                Debug.LogError($"[{GetType().Name}] State machine not initialized");
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
            Debug.Log($"[{GetType().Name}] Client startup complete");
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

        public void Dispose()
        {
            if (_stateMachine != null)
            {
                _stateMachine.OnStateChanged -= HandleStateChanged;
            }
            
            OnGameStateChanged = null;
            OnLoadingProgress = null;
            OnClientStartupComplete = null;
            
            Debug.Log($"[{GetType().Name}] Disposed");
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion
    }
}