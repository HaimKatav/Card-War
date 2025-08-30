using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardWar.Services;
using CardWar.Core;
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
        private IAudioService _audioService;
        private IAssetService _assetService;
        
        private UIState _currentUIState;
        private Action _resetCallback;
        private bool _isInitialized;

        public UIState CurrentUIState => _currentUIState;
        
        public event Action<UIState> OnUIStateChanged;

        #region Initialization

        [Inject]
        public void Construct(IDIService diService, IGameStateService gameStateService, 
            IAudioService audioService, IAssetService assetService)
        {
            _diService = diService;
            _gameStateService = gameStateService;
            _audioService = audioService;
            _assetService = assetService;
            
            SetupButtons();
            SubscribeToEvents();
            InitializeUI();
            
            _isInitialized = true;
            SetUIState(UIState.Idle);
            
            Debug.Log($"[UIManager] Initialized with all dependencies");
        }

        private void InitializeUI()
        {
            HideAllLayers();
            
            if (_loadingProgressBar != null)
            {
                _loadingProgressBar.value = 0;
            }
            
            Debug.Log($"[UIManager] UI initialized");
        }

        #endregion

        #region Button Setup

        private void SetupButtons()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveAllListeners();
                _startButton.onClick.AddListener(HandleStartButtonClick);
            }

            if (_playAgainButton != null)
            {
                _playAgainButton.onClick.RemoveAllListeners();
                _playAgainButton.onClick.AddListener(HandlePlayAgainButtonClick);
            }
            
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveAllListeners();
                _mainMenuButton.onClick.AddListener(HandleMainMenuButtonClick);
            }
        }

        #endregion

        #region Event Management

        private void SubscribeToEvents()
        {
            if (_gameStateService != null)
            {
                _gameStateService.OnGameStateChanged += HandleGameStateChanged;
                _gameStateService.OnLoadingProgress += HandleLoadingProgress;
            }
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

        public void ShowMainMenu(bool show)
        {
            if (_mainMenuLayer != null)
            {
                _mainMenuLayer.SetActive(show);
                Debug.Log($"[UIManager] Main menu {(show ? "shown" : "hidden")}");
            }
        }

        public void ToggleLoadingScreen(bool show)
        {
            if (_loadingLayer != null)
            {
                _loadingLayer.SetActive(show);

                Debug.Log($"[UIManager] Loading screen {(show ? "shown" : "hidden")}");
            }
        }

        public void UpdateLoadingProgress(float progress)
        {
            if (_loadingProgressBar != null)
            {
                _loadingProgressBar.value = progress;
            }
            
            if (_loadingPercentage != null)
            {
                _loadingPercentage.text = $"{(int)(progress * 100)}%";
            }
        }

        public void ShowGameUI(bool show)
        {
            if (_gameLayer != null)
            {
                _gameLayer.SetActive(show);
                Debug.Log($"[UIManager] Game UI {(show ? "shown" : "hidden")}");
            }
        }

        public void ToggleGameOverScreen(bool show, bool playerWon)
        {
            if (_gameOverLayer != null)
            {
                _gameOverLayer.SetActive(show);
                
                if (show && _gameOverMessage != null)
                {
                    _gameOverMessage.text = playerWon ? "You Win!" : "You Lose!";
                }
                
                Debug.Log($"[UIManager] Game over screen {(show ? "shown" : "hidden")}");
            }
        }

        public void RegisterResetCallback(Action callback)
        {
            _resetCallback = callback;
            Debug.Log($"[UIManager] Reset callback registered");
        }

        private void HideAllLayers()
        {
            ShowMainMenu(false);
            ToggleLoadingScreen(false);
            ShowGameUI(false);
            ToggleGameOverScreen(false, false);
        }

        private void SetUIState(UIState newState)
        {
            var previousState = _currentUIState;
            _currentUIState = newState;
            OnUIStateChanged?.Invoke(newState);
            
            if (previousState != newState)
            {
                Debug.Log($"[UIManager] UI state changed: {previousState} -> {newState}");
            }
        }

        #endregion

        #region Event Handlers

        private void HandleStartButtonClick()
        {
            Debug.Log($"[UIManager] Start button clicked");
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            _gameStateService?.ChangeState(GameState.LoadingGame);
        }

        private void HandlePlayAgainButtonClick()
        {
            Debug.Log($"[UIManager] Play again button clicked");
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            _resetCallback?.Invoke();
            _gameStateService?.ChangeState(GameState.LoadingGame);
        }

        private void HandleMainMenuButtonClick()
        {
            Debug.Log($"[UIManager] Main menu button clicked");
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            _gameStateService?.ChangeState(GameState.ReturnToMenu);
        }

        private void HandleGameStateChanged(GameState newState, GameState previousState)
        {
            Debug.Log($"[UIManager] Handling state change: {previousState} -> {newState}");
            
            switch (newState)
            {
                case GameState.MainMenu:
                    HideAllLayers();
                    ShowMainMenu(true);
                    break;
                    
                case GameState.LoadingGame:
                    ShowMainMenu(false);
                    ToggleLoadingScreen(true);
                    ShowGameUI(false);
                    ToggleGameOverScreen(false, false);
                    break;
                    
                case GameState.Playing:
                    ToggleLoadingScreen(false);
                    ShowGameUI(true);
                    break;
                    
                case GameState.GameEnded:
                    ToggleGameOverScreen(true, false);
                    break;
                    
                case GameState.ReturnToMenu:
                    HideAllLayers();
                    ShowMainMenu(true);
                    break;
            }
        }

        private void HandleLoadingProgress(float progress)
        {
            UpdateLoadingProgress(progress);
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            UnsubscribeFromEvents();
            Debug.Log($"[UIManager] Disposed");
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion
    }
}