using System;
using UnityEngine;
using UnityEngine.UI;
using CardWar.Services;
using CardWar.Common;
using CardWar.Game.UI;
using TMPro;

namespace CardWar.Managers
{
    public class UIManager : MonoBehaviour, IUIService
    {
        [Header("UI Layers")]
        [SerializeField] private GameObject _loadingLayer;
        [SerializeField] private GameObject _menuLayer;
        [SerializeField] private GameObject _gameLayer;
        [SerializeField] private GameObject _gameOverLayer;
        
        [Header("Game Area Parent")]
        [SerializeField] private GameObject _gameAreaParent;

        [Header("Menu Elements")]
        [SerializeField] private Button _startButton;
        
        [Header("Game Over Elements")]
        [SerializeField] private TMP_Text _gameOverText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;
        
        [Header("Loading Elements")]
        [SerializeField] private Slider _loadingSlider;
        [SerializeField] private TMP_Text _loadingText;
        
        [Header("Game Area UI Elements")]
        [SerializeField] private GameUIView _gameUIView;
        
        private Action _resetCallback;
        private IGameStateService _gameStateService;

        public event Action StartButtonPressedEvent;
        public event Action RestartButtonPressedEvent;
        public event Action MenuButtonPressedEvent;
        public event Action ResumeButtonPressedEvent;
        public event Action PauseButtonPressedEvent;

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            SetupUIElements();
            HideAllLayers();
            SubscribeToEvents();

            Debug.Log("[UIManager] Initialized");
        }
        
        private void SubscribeToEvents()
        {
            _gameStateService = ServiceLocator.Instance.Get<IGameStateService>();
            
            if (_gameStateService != null)
            {
                Debug.Log("[UIManager] Subscribed to events");
                _gameStateService.GameStateChanged += HandleGameStateChanged;
                _gameStateService.OnLoadingProgress += HandleLoadingProgress;
            }
        }

        private void SetupUIElements()
        {
            _startButton.onClick.RemoveAllListeners();
            _startButton.onClick.AddListener(OnStartButtonClicked);

            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(OnRestartButtonClicked);

            _menuButton.onClick.RemoveAllListeners();
            _menuButton.onClick.AddListener(OnMenuButtonClicked);
            
            _gameUIView.OnPauseButtonPressed += OnPauseButtonClicked;
            _gameUIView.OnResumeButtonPressed += OnResumeButtonPressed;
            _gameUIView.OnBackToMainMenuButtonPressed += OnBackToMainMenuButtonPressed;
        }

        #endregion Unity Lifecycle

        
        #region IUIService Implementation

        public GameObject GetGameAreaParent()
        {
            return _gameAreaParent;
        }
        
        #endregion

        
        #region Public Methods
        
        private void ToggleLoadingScreen(bool show)
        {
            if (_loadingLayer != null)
            {
                _loadingLayer.SetActive(show);
                
                if (show)
                {
                    ResetLoadingProgress();
                }
            }
        }

        private void ToggleMainMenu(bool show)
        {
            if (_menuLayer != null)
            {
                _menuLayer.SetActive(show);
            }
        }

        private void ToggleGameUI(bool show)
        {
            if (_gameLayer != null)
            {
                _gameLayer.SetActive(show);
            }
        }

        private void TogglePauseMenu(bool show)
        {
            _gameUIView.TogglePauseMenu(show);
        }

        private void ToggleGameOverScreen(bool show)
        {
            if (_gameOverLayer != null)
            {
                _gameOverLayer.SetActive(show);
                
                if (show && _gameOverText != null)
                {
                    _gameOverText.text = _gameStateService.MatchStatus == GameStatus.PlayerWon ? "You Win!" : "You Lose!";
                }
            }
        }
        
        #endregion

        
        #region Private Methods

        private void HideAllLayers()
        {
            ToggleLoadingScreen(false);
            ToggleMainMenu(false);
            ToggleGameUI(false);
            ToggleGameOverScreen(false);
        }
        
        private void ResetLoadingProgress()
        {
            if (_loadingSlider != null)
                _loadingSlider.value = 0;
                
            if (_loadingText != null)
                _loadingText.text = "Loading... 0%";
        }

        #endregion Private Methods


        #region UI Input Handling

        private void OnPauseButtonClicked()
        {
            Debug.Log("[UIManager] Pause button clicked");
            PauseButtonPressedEvent?.Invoke();
        }
        
        private void OnStartButtonClicked()
        {
            Debug.Log("[UIManager] Start button clicked");
            StartButtonPressedEvent?.Invoke();
        }

        private void OnBackToMainMenuButtonPressed()
        {
            Debug.Log("[UIManager] Back button clicked");
            MenuButtonPressedEvent?.Invoke();
        }

        private void OnResumeButtonPressed()
        {
            Debug.Log("[UIManager] Resume button clicked");
            ResumeButtonPressedEvent?.Invoke();
        }
        
        private void OnRestartButtonClicked()
        {
            Debug.Log("[UIManager] Restart button clicked");
            _resetCallback?.Invoke();
            RestartButtonPressedEvent?.Invoke();
        }

        private void OnMenuButtonClicked()
        {
            Debug.Log("[UIManager] Menu button clicked");
            MenuButtonPressedEvent?.Invoke();
        }

        #endregion UI Input Handling
        
        
        #region Event Handlers

        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.FirstLoad:
                    HideAllLayers();
                    break;

                case GameState.LoadingGame:
                    ToggleMainMenu(false);
                    ToggleGameUI(false);
                    ToggleGameOverScreen(false);
                    ToggleLoadingScreen(true);
                    TogglePauseMenu(false);
                    break;
                
                case GameState.MainMenu:
                    ToggleLoadingScreen(false);
                    ToggleGameUI(false);
                    ToggleGameOverScreen(false);
                    ToggleMainMenu(true);
                    TogglePauseMenu(false);
                    break;

                case GameState.Playing:
                    ToggleLoadingScreen(false);
                    ToggleMainMenu(false);
                    ToggleGameOverScreen(false);
                    ToggleGameUI(true);
                    TogglePauseMenu(false);
                    break;

                case GameState.Paused:
                    TogglePauseMenu(true);
                    break;

                case GameState.GameEnded:
                    ToggleGameOverScreen(false);
                    break;
            }
        }

        private void HandleLoadingProgress(float progress)
        {
            if (_loadingSlider != null)
                _loadingSlider.value = progress;
                
            if (_loadingText != null)
                _loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
        }
        
        #endregion

        
        #region Cleanup

        private void OnDestroy()
        {
            if (_startButton != null)
                _startButton.onClick.RemoveAllListeners();
                
            if (_restartButton != null)
                _restartButton.onClick.RemoveAllListeners();
                
            if (_menuButton != null)
                _menuButton.onClick.RemoveAllListeners();
                
            if (_gameStateService != null)
            {
                _gameStateService.GameStateChanged -= HandleGameStateChanged;
                _gameStateService.OnLoadingProgress -= HandleLoadingProgress;
            }

            if (_gameUIView != null)
            {
                _gameUIView.OnPauseButtonPressed -= OnPauseButtonClicked;
                _gameUIView.OnResumeButtonPressed -= OnResumeButtonPressed;
                _gameUIView.OnBackToMainMenuButtonPressed -= OnBackToMainMenuButtonPressed;
            }
            
            StartButtonPressedEvent = null;
            RestartButtonPressedEvent = null;
            MenuButtonPressedEvent = null;
        }

        #endregion
    }
}