using System;
using UnityEngine;
using CardWar.Services;
using CardWar.Core;
using CardWar.Common;
using CardWar.Game;
using Cysharp.Threading.Tasks;

namespace CardWar.Managers
{
    // Developer notes:
    // This Code Was made By Haim Katav for Beach Bum as a home assignment.
    
    // I would like to state that i really tried "attacking" this job from multiple angles to
    // show capabilities in a few areas. I know it made the architecture a bit large for a home assignment, sorry about that.
    // Some of the code here were used in other projects of mine and were integrated into this project like:
    // Asset Manager,Game State Machine and Generic pool - They were all designed and made by me.
    // - Service Locator - I made for this project, since DI was overkill here.
    // - The rest of the classes were made by me with the occasional Rider suggestions and Context Actions fixes.
    // Please view the AI generated Overview PDF I've created for better introduction for this project.
    
    // Things I would do differently:
    // 1) Migrate state related code into classes instead of using enums and all the code in game manager.
    // 2) Instead of Creating this huge UIManager class, use more subtle solutions and create a functional structure to do the UI instead of putting all the code in one place - This was one of the things I decided to no invest my time in for this project.
    // 3) GameSetting - would make room for a proper DataService that gets the data from the server on client startup.
    // 4) GameUIView should be renamed to GameUIController in order to justify the communication to GameController Service and take on the role of the game's board UI controlling entity.
    // 5) GameBoardController - Dismantle this class into smaller classes that govern our capabilities on the board.   
    // 6) Creating an IServerService interface in order to be able to change the server code easily and quickly. 
    
    // There are probably more things i could improve on to make this project better but this is from the top of my head hor now.
    // I Hope you'll enjoy checking this project.
    
    public class GameManager : MonoBehaviour, IGameStateService
    {
        [SerializeField] private GameSettings _gameSettings;
        
        private GameStateMachine _stateMachine;
        private AssetManager _assetManager;
        private AudioManager _audioManager;
        private UIManager _uiManager;
        private GameController _gameController;

        public event Action<GameState> GameStateChanged;
        public event Action<float> OnLoadingProgress;
        public GameStatus MatchStatus { get; private set; } = GameStatus.NotStarted;

        private GameState CurrentState => _stateMachine?.CurrentStateType ?? GameState.FirstLoad;
        
        private float _loadingProgress;


        #region Initialization

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            InitializeGame().Forget();
        }

        private async UniTaskVoid InitializeGame()
        {
            Debug.Log($"[GameManager] Starting initialization");
            
            ServiceLocator.Instance.Dispose();
            ServiceLocator.Instance.Register<IGameStateService>(this);
            ServiceLocator.Instance.Register(_gameSettings);
            
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
            
            ServiceLocator.Instance.Register<IAssetService>(_assetManager);
            ServiceLocator.Instance.Register<IAudioService>(_audioManager);
            
            var uiPrefab = await _assetManager.LoadAssetAsync<UIManager>(GameSettings.UI_MANAGER_ASSET_PATH);
            _uiManager = Instantiate(uiPrefab);
            ServiceLocator.Instance.Register<IUIService>(_uiManager);
            
            _gameController = CreateService<GameController>("GameController", false);
        
            ServiceLocator.Instance.Register<IGameControllerService>(_gameController);
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
            SimulateLoading().Forget();
            var result = await _gameController.CreateNewGame();
            
            if (!result)
            {
                Debug.Log("[GameManager] Failed to create new game - Returning to Main Menu");
                ChangeState(GameState.MainMenu);
                return;
            }
            
            ChangeState(GameState.Playing);
        }
        
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

        private void UpdateLoadingProgress(float progress)
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
        
        private void OnDestroy()
        {
            _stateMachine?.Clear();

            UnregisterEvents();

            GameStateChanged = null;
            OnLoadingProgress = null;

            ServiceLocator.Instance.Dispose();
        }

        #endregion
    }
}