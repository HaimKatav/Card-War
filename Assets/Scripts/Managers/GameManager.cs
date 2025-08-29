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
        private IDIService _diService;
        private IAssetService _assetService;
        private GameSettings _gameSettings;
        private UIManager _uiManager;
        private GameController _gameController;
        
        private GameState _currentState;
        private GameState _previousState;
        private bool _isTransitioning;
        

        public GameState CurrentState => _currentState;
        public GameState PreviousState => _previousState;

        public event Action<GameState, GameState> OnGameStateChanged;
        public event Action<float> OnLoadingProgress;
        public event Action OnClientStartupComplete;

        [Inject]
        public void Initialize(IDIService diService, IAssetService assetService, GameSettings gameSettings)
        {
            _diService = diService;
            _assetService = assetService;
            _gameSettings = gameSettings;
            _currentState = GameState.FirstLoad;
            _previousState = GameState.FirstLoad;
        }

        private void Start()
        {
            InitializeManagers();
        }

        private void InitializeManagers()
        {
            CreateUIManager();
            CreateGameController();
            TransitionToMainMenu();
        }

        private async void CreateUIManager()
        {
            _uiManager = await _assetService.LoadAssetAsync<UIManager>("MainCanvas");
        }

        private void CreateGameController()
        {
            GameObject controllerObject = new GameObject("GameController");
            controllerObject.transform.SetParent(transform);
            _gameController = controllerObject.AddComponent<GameController>();
        }

        private void TransitionToMainMenu()
        {
            UniTask.Create(async () =>
            {
                await UniTask.Delay(100);
                ChangeState(GameState.MainMenu);
                NotifyStartupComplete();
            }).Forget();
        }

        public void ChangeState(GameState newState)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning($"State transition in progress, ignoring change to {newState}");
                return;
            }

            if (_currentState == newState)
            {
                Debug.LogWarning($"Already in state {newState}");
                return;
            }

            _isTransitioning = true;
            _previousState = _currentState;
            _currentState = newState;
            
            Debug.Log($"State transition: {_previousState} -> {_currentState}");
            
            HandleStateTransition(newState, _previousState);
            OnGameStateChanged?.Invoke(newState, _previousState);
            
            _isTransitioning = false;
        }

        private void HandleStateTransition(GameState newState, GameState oldState)
        {
            switch (newState)
            {
                case GameState.FirstLoad:
                    HandleFirstLoad();
                    break;
                    
                case GameState.MainMenu:
                    HandleMainMenu();
                    break;
                    
                case GameState.LoadingGame:
                    HandleLoadingGame();
                    break;
                    
                case GameState.Playing:
                    HandlePlaying();
                    break;
                    
                case GameState.Paused:
                    HandlePaused();
                    break;
                    
                case GameState.GameEnded:
                    HandleGameEnded();
                    break;
                    
                case GameState.ReturnToMenu:
                    HandleReturnToMenu();
                    break;
            }
        }

        private void HandleFirstLoad()
        {
            UpdateLoadingProgress(0f);
        }

        private void HandleMainMenu()
        {
            _uiManager?.ShowMainMenu(true);
            _uiManager?.ShowLoadingScreen(false);
            _uiManager?.ShowGameUI(false);
        }

        private void HandleLoadingGame()
        {
            _uiManager?.ShowMainMenu(false);
            _uiManager?.ShowLoadingScreen(true);
            StartGameLoading().Forget();
        }

        private async UniTaskVoid StartGameLoading()
        {
            UpdateLoadingProgress(0.1f);
            
            IAssetService assetService = _diService.GetService<IAssetService>();
            if (assetService != null)
            {
                await assetService.PreloadCardAssets();
                UpdateLoadingProgress(0.5f);
            }
            
            _gameController?.StartNewGame();
            UpdateLoadingProgress(0.8f);
            
            await UniTask.Delay(TimeSpan.FromSeconds(_gameSettings.LoadingScreenMinDuration));
            UpdateLoadingProgress(1.0f);
            
            await UniTask.Delay(500);
            ChangeState(GameState.Playing);
        }

        private void HandlePlaying()
        {
            _uiManager?.ShowLoadingScreen(false);
            _uiManager?.ShowGameUI(true);
        }

        private void HandlePaused()
        {
            _gameController?.PauseGame();
        }

        private void HandleGameEnded()
        {
            bool playerWon = DetermineWinner();
            _uiManager?.ShowGameOverScreen(true, playerWon);
        }

        private void HandleReturnToMenu()
        {
            ResetGame();
            ChangeState(GameState.MainMenu);
        }

        private bool DetermineWinner()
        {
            return UnityEngine.Random.value > 0.5f;
        }

        private void ResetGame()
        {
            _gameController?.ReturnToMenu();
            _uiManager?.ShowGameOverScreen(false, false);
            _uiManager?.ShowGameUI(false);
        }

        public void UpdateLoadingProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            OnLoadingProgress?.Invoke(progress);
        }

        public void NotifyStartupComplete()
        {
            Debug.Log("Client startup complete");
            OnClientStartupComplete?.Invoke();
        }

        public void StartGame()
        {
            if (_currentState == GameState.MainMenu)
            {
                ChangeState(GameState.LoadingGame);
            }
        }

        public void PauseGame()
        {
            if (_currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (_currentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
                _gameController?.ResumeGame();
            }
        }

        public void EndGame()
        {
            if (_currentState == GameState.Playing || _currentState == GameState.Paused)
            {
                ChangeState(GameState.GameEnded);
            }
        }

        public void ReturnToMainMenu()
        {
            ChangeState(GameState.ReturnToMenu);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _currentState == GameState.Playing)
            {
                PauseGame();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _currentState == GameState.Playing)
            {
                PauseGame();
            }
        }

        public void Dispose()
        {
            OnGameStateChanged = null;
            OnLoadingProgress = null;
            OnClientStartupComplete = null;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}