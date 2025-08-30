using System;
using UnityEngine;
using UnityEngine.UI;
using CardWar.Services;
using CardWar.Core;
using CardWar.Common;
using TMPro;

namespace CardWar.Managers
{
    public enum UIState
    {
        FirstEntry,
        Idle,
        Animating,
        Loading
    }
    
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
        
        private UIState _currentUIState = UIState.FirstEntry;
        private Action _resetCallback;
        private IGameStateService _gameStateService;

        public UIState CurrentUIState => _currentUIState;
        
        public event Action<string> OnAnimationStarted;
        public event Action<string> OnAnimationCompleted;
        public event Action<UIState> OnUIStateChanged;
        
        public event Action OnStartButtonPressed;
        public event Action OnRestartButtonPressed;
        public event Action OnMenuButtonPressed;

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            SetupUIElements();
            HideAllLayers();
            
            Debug.Log("[UIManager] Initialized");
        }
        
        private void Start()
        {
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            _gameStateService = ServiceLocator.Instance.Get<IGameStateService>();
            
            if (_gameStateService != null)
            {
                _gameStateService.OnGameStateChanged += HandleGameStateChanged;
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
        }

        #endregion

        #region IUIService Implementation

        public GameObject GetGameAreaParent()
        {
            return _gameAreaParent;
        }
        
        #endregion

        #region Public Methods
        
        public void ToggleLoadingScreen(bool show)
        {
            if (_loadingLayer != null)
            {
                _loadingLayer.SetActive(show);
                
                if (show)
                {
                    ResetLoadingProgress();
                    SetUIState(UIState.Loading);
                }
            }
        }

        public void ToggleMainMenu(bool show)
        {
            if (_menuLayer != null)
            {
                _menuLayer.SetActive(show);
                
                if (show)
                    SetUIState(UIState.Idle);
            }
        }

        public void ToggleGameUI(bool show)
        {
            if (_gameLayer != null)
            {
                _gameLayer.SetActive(show);
                
                if (show)
                    SetUIState(UIState.Idle);
            }
        }

        public void ToggleGameOverScreen(bool show, bool playerWon)
        {
            if (_gameOverLayer != null)
            {
                _gameOverLayer.SetActive(show);
                
                if (show && _gameOverText != null)
                {
                    _gameOverText.text = playerWon ? "You Win!" : "You Lose!";
                }
            }
        }

        private void SetUIState(UIState state)
        {
            if (_currentUIState != state)
            {
                _currentUIState = state;
                OnUIStateChanged?.Invoke(state);
            }
        }

        public void RegisterResetCallback(Action callback)
        {
            _resetCallback = callback;
        }

        #endregion

        #region Private Methods

        private void HideAllLayers()
        {
            ToggleLoadingScreen(false);
            ToggleMainMenu(false);
            ToggleGameUI(false);
            ToggleGameOverScreen(false, false);
        }
        
        private void ResetLoadingProgress()
        {
            if (_loadingSlider != null)
                _loadingSlider.value = 0;
                
            if (_loadingText != null)
                _loadingText.text = "Loading... 0%";
        }

        private void OnStartButtonClicked()
        {
            Debug.Log("[UIManager] Start button clicked");
            OnStartButtonPressed?.Invoke();
        }

        private void OnRestartButtonClicked()
        {
            Debug.Log("[UIManager] Restart button clicked");
            _resetCallback?.Invoke();
            OnRestartButtonPressed?.Invoke();
        }

        private void OnMenuButtonClicked()
        {
            Debug.Log("[UIManager] Menu button clicked");
            OnMenuButtonPressed?.Invoke();
        }

        #endregion
        
        #region Event Handlers
        
        private void HandleGameStateChanged(GameState newState, GameState previousState)
        {
            Debug.Log($"[UIManager] Game state changed: {previousState} -> {newState}");

            switch (newState)
            {
                case GameState.FirstLoad:
                    HideAllLayers();
                    break;
                    
                case GameState.LoadingGame:
                    ToggleMainMenu(false);
                    ToggleGameUI(false);
                    ToggleGameOverScreen(false, false);
                    ToggleLoadingScreen(true);
                    break;
                    
                case GameState.MainMenu:
                    ToggleLoadingScreen(false);
                    ToggleGameUI(false);
                    ToggleGameOverScreen(false, false);
                    ToggleMainMenu(true);
                    break;
                    
                case GameState.Playing:
                    ToggleLoadingScreen(false);
                    ToggleMainMenu(false);
                    ToggleGameOverScreen(false, false);
                    ToggleGameUI(true);
                    break;
                    
                case GameState.Paused:
                    break;
                    
                case GameState.GameEnded:
                    ToggleGameUI(false);
                    break;
                    
                case GameState.ReturnToMenu:
                    ToggleGameUI(false);
                    ToggleGameOverScreen(false, false);
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
                _gameStateService.OnGameStateChanged -= HandleGameStateChanged;
                _gameStateService.OnLoadingProgress -= HandleLoadingProgress;
            }
            
            OnStartButtonPressed = null;
            OnRestartButtonPressed = null;
            OnMenuButtonPressed = null;
        }

        #endregion
    }
}