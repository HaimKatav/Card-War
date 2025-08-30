using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardWar.Services;
using CardWar.Core;
using CardWar.Game.UI;
using Cysharp.Threading.Tasks;
using Zenject;

namespace CardWar.Managers
{
    public class UIManager : MonoBehaviour, IUIService, IDisposable
    {
        [Header("UI Layers")]
        [SerializeField] private GameObject _mainMenuLayer;
        [SerializeField] private GameObject _loadingLayer;
        [SerializeField] private GameObject _gameLayer;
        [SerializeField] private GameObject _gameOverLayer;
        
        [Header("Play Area")]
        [SerializeField] private Transform _playAreaParent;
        
        [Header("Main Menu")]
        [SerializeField] private Button _startButton;
        
        [Header("Loading Screen")]
        [SerializeField] private Slider _loadingProgressBar;
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private TextMeshProUGUI _loadingPercentage;
        
        [Header("Game Over Screen")]
        [SerializeField] private TextMeshProUGUI _gameOverMessage;
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _mainMenuButton;
        
        private IDIService _diService;
        private IGameStateService _gameStateService;
        private GameSettings _gameSettings;
        
        private IGameControllerService _gameController;
        private IAudioService _audioService;
        private IAssetService _assetService;
        
        private GameObject _playAreaInstance;
        private GameUIView _gameUIView;
        
        private UIState _currentUIState;
        private Action _resetCallback;
        private float _targetLoadingProgress;
        private float _currentLoadingProgress;
        private bool _isInitialized;

        public UIState CurrentUIState => _currentUIState;

        public event Action<string> OnAnimationStarted;
        public event Action<string> OnAnimationCompleted;
        public event Action<UIState> OnUIStateChanged;

        #region Initialization

        [Inject]
        public void Initialize(IAudioService audioService, IAssetService assetService)
        {
            if (_isInitialized) return;

            _audioService = audioService;
            _assetService = assetService;
            
            SetUIState(UIState.FirstEntry);
            
            SetupButtons();
            SubscribeToEvents();
            InitializeUI();
            
            _isInitialized = true;
            
            SetUIState(UIState.Idle);
            
            Debug.Log($"[{GetType().Name}] Initialized successfully");
        }

        #endregion

        #region Button Setup

        private void SetupButtons()
        {
            _startButton.onClick.RemoveAllListeners();
            _startButton.onClick.AddListener(HandleStartButtonClick);

            _playAgainButton.onClick.RemoveAllListeners();
            _playAgainButton.onClick.AddListener(HandlePlayAgainButtonClick);
            
            _mainMenuButton.onClick.RemoveAllListeners();
            _mainMenuButton.onClick.AddListener(HandleMainMenuButtonClick);
        }

        #endregion

        #region Event Management

        private void SubscribeToEvents()
        {
            if (_gameStateService == null)
            {
                Debug.LogError($"[{GetType().Name}] Cannot subscribe to events - GameStateService is null");
                return;
            }
            
            _gameStateService.OnGameStateChanged += HandleGameStateChanged;
            _gameStateService.OnLoadingProgress += HandleLoadingProgress;
        }

        private void UnsubscribeFromEvents()
        {
            if (_gameStateService != null)
            {
                _gameStateService.OnGameStateChanged -= HandleGameStateChanged;
                _gameStateService.OnLoadingProgress -= HandleLoadingProgress;
            }
        }

        #endregion

        #region UI State Management

        private void InitializeUI()
        {
            HideAllLayers();
            ShowMainMenu(true);
        }

        public void SetUIState(UIState state)
        {
            if (_currentUIState == state) return;
            
            var previousState = _currentUIState;
            _currentUIState = state;
            
            Debug.Log($"[{GetType().Name}] UI State changed: {previousState} -> {state}");
            OnUIStateChanged?.Invoke(state);
        }

        public void ShowLoadingScreen(bool show)
        {
            _loadingLayer.SetActive(show);
            if (show)
            {
                SetUIState(UIState.Loading);
                ResetLoadingProgress();
            }
            Debug.Log($"[{GetType().Name}] Loading screen: {show}");
        }

        public void ShowMainMenu(bool show)
        {
            if (_mainMenuLayer != null)
            {
                _mainMenuLayer.SetActive(show);
            }
        }

        public void ShowGameUI(bool show)
        {
            if (_gameLayer != null)
            {
                _gameLayer.SetActive(show);
                Debug.Log($"[{GetType().Name}] Game UI: {show}");
                
                if (show && _playAreaInstance == null && _playAreaParent != null)
                {
                    LoadPlayAreaAsync().Forget();
                }
            }
        }

        public void ToggleGameOverScreen(bool show, bool playerWon)
        {
            if (_gameOverLayer == null)
            {
                Debug.LogError($"[{GetType().Name}] _gameOverLayer is null");
                return;
            }
            
            _gameOverLayer.SetActive(show);

            if (!show) return;
            
            _gameOverMessage.text = playerWon ? "Victory!" : "Defeat!";
            _gameOverMessage.text = playerWon 
                ? "Congratulations! You've won the war!" 
                : "Better luck next time!";
        }

        public void RegisterResetCallback(Action callback)
        {
            _resetCallback = callback;
        }

        private void HideAllLayers()
        {
            ShowMainMenu(false);
            ShowLoadingScreen(false);
            ShowGameUI(false);
            ToggleGameOverScreen(false, false);
        }

        #endregion

        #region Play Area Management

        private async UniTaskVoid LoadPlayAreaAsync()
        {
            if (_playAreaParent == null)
            {
                Debug.LogError($"[{GetType().Name}] Cannot load PlayArea - PlayAreaParent is null");
                return;
            }
            
            Debug.Log($"[{GetType().Name}] Loading PlayArea...");
            
            GameObject playAreaPrefab = null;
            
            if (_assetService != null)
            {
                playAreaPrefab = await _assetService.LoadAssetAsync<GameObject>("Prefabs/PlayArea");
            }
            else
            {
                playAreaPrefab = Resources.Load<GameObject>("Prefabs/PlayArea");
            }
            
            if (playAreaPrefab == null)
            {
                Debug.LogError($"[{GetType().Name}] Failed to load PlayArea prefab");
                return;
            }
            
            _playAreaInstance = Instantiate(playAreaPrefab, _playAreaParent);
            _playAreaInstance.name = "PlayArea";
            
            _gameUIView = _playAreaInstance.GetComponentInChildren<GameUIView>();
            
            Debug.Log($"[{GetType().Name}] PlayArea loaded and instantiated");
        }

        private void CleanupPlayArea()
        {
            if (_playAreaInstance != null)
            {
                Destroy(_playAreaInstance);
                _playAreaInstance = null;
                _gameUIView = null;
                Debug.Log($"[{GetType().Name}] PlayArea cleaned up");
            }
        }

        #endregion

        #region Event Handlers

        private void HandleStartButtonClick()
        {
            Debug.Log($"[{GetType().Name}] Start button clicked");
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            _gameStateService?.ChangeState(GameState.LoadingGame);
        }

        private void HandlePlayAgainButtonClick()
        {
            Debug.Log($"[{GetType().Name}] Play again button clicked");
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            _resetCallback?.Invoke();
            _gameStateService?.ChangeState(GameState.LoadingGame);
        }

        private void HandleMainMenuButtonClick()
        {
            Debug.Log($"[{GetType().Name}] Main menu button clicked");
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            _gameStateService?.ChangeState(GameState.ReturnToMenu);
        }

        private void HandleGameStateChanged(GameState newState, GameState previousState)
        {
            Debug.Log($"[{GetType().Name}] Handling state change: {previousState} -> {newState}");
            
            switch (newState)
            {
                case GameState.MainMenu:
                    HideAllLayers();
                    ShowMainMenu(true);
                    break;
                    
                case GameState.LoadingGame:
                    ShowMainMenu(false);
                    ShowLoadingScreen(true);
                    ShowGameUI(false);
                    ToggleGameOverScreen(false, false);
                    break;
                    
                case GameState.Playing:
                    ShowLoadingScreen(false);
                    ShowGameUI(true);
                    break;
                    
                case GameState.GameEnded:
                    DetermineWinnerAndShowGameOver();
                    break;
                    
                case GameState.ReturnToMenu:
                    HideAllLayers();
                    CleanupPlayArea();
                    ShowMainMenu(true);
                    break;
            }
        }

        private void HandleLoadingProgress(float progress)
        {
            _targetLoadingProgress = progress;
        }

        private void DetermineWinnerAndShowGameOver()
        {
            var playerWon = UnityEngine.Random.value > 0.5f;
            ToggleGameOverScreen(true, playerWon);
        }

        #endregion

        #region Loading Animation

        private void ResetLoadingProgress()
        {
            _currentLoadingProgress = 0f;
            _targetLoadingProgress = 0f;
            
            if (_loadingProgressBar != null)
            {
                _loadingProgressBar.value = 0f;
            }
            
            if (_loadingPercentage != null)
            {
                _loadingPercentage.text = "0%";
            }
            
            if (_loadingText != null)
            {
                _loadingText.text = "Loading";
            }
        }

        private void Update()
        {
            if (_currentUIState == UIState.Loading && _loadingProgressBar != null)
            {
                _currentLoadingProgress = Mathf.Lerp(_currentLoadingProgress, _targetLoadingProgress, Time.deltaTime * 3f);
                _loadingProgressBar.value = _currentLoadingProgress;
                
                if (_loadingPercentage != null)
                {
                    _loadingPercentage.text = $"{Mathf.RoundToInt(_currentLoadingProgress * 100)}%";
                }
                
                if (_loadingText != null)
                {
                    var dotCount = Mathf.FloorToInt(Time.time * 2f) % 4;
                    _loadingText.text = "Loading" + new string('.', dotCount);
                }
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            UnsubscribeFromEvents();
            CleanupPlayArea();
            
            Debug.Log($"[{GetType().Name}] Disposed");
        }

        #endregion
    }
}