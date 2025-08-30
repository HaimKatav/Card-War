using System;
using CardWar.Common;
using UnityEngine;
using UnityEngine.UI;
using CardWar.Services;
using CardWar.Core;

namespace CardWar.Managers
{
    public class UIManager : MonoBehaviour, IUIService
    {
        [Header("UI Layers")]
        [SerializeField] private GameObject _loadingLayer;
        [SerializeField] private GameObject _menuLayer;
        [SerializeField] private GameObject _gameLayer;
        [SerializeField] private GameObject _gameOverLayer;
        
        [Header("Menu Elements")]
        [SerializeField] private Button _startButton;
        
        [Header("Game Over Elements")]
        [SerializeField] private Text _gameOverText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;
        
        private UIState _currentUIState = UIState.FirstEntry;
        private Action _resetCallback;
        private IGameStateService _gameStateService;

        public UIState CurrentUIState => _currentUIState;
        
        public event Action<string> OnAnimationStarted;
        public event Action<string> OnAnimationCompleted;
        public event Action<UIState> OnUIStateChanged;

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            _gameStateService = ServiceLocator.Instance.Get<IGameStateService>();
                
            SetupUIElements();
            HideAllLayers();
            
            Debug.Log("[UIManager] Initialized");
        }

        private void SetupUIElements()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveAllListeners();
                _startButton.onClick.AddListener(OnStartButtonClicked);
            }
            
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveAllListeners();
                _restartButton.onClick.AddListener(OnRestartButtonClicked);
            }
            
            if (_menuButton != null)
            {
                _menuButton.onClick.RemoveAllListeners();
                _menuButton.onClick.AddListener(OnMenuButtonClicked);
            }
        }

        #endregion

        #region IUIService Implementation

        public void ToggleLoadingScreen(bool show)
        {
            if (_loadingLayer != null)
                _loadingLayer.SetActive(show);
                
            if (show)
                SetUIState(UIState.Loading);
        }

        public void ShowMainMenu(bool show)
        {
            if (_menuLayer != null)
                _menuLayer.SetActive(show);
                
            if (show)
                SetUIState(UIState.Idle);
        }

        public void ShowGameUI(bool show)
        {
            if (_gameLayer != null)
                _gameLayer.SetActive(show);
                
            if (show)
                SetUIState(UIState.Idle);
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

        public void SetUIState(UIState state)
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
            ShowMainMenu(false);
            ShowGameUI(false);
            ToggleGameOverScreen(false, false);
        }

        private void OnStartButtonClicked()
        {
            Debug.Log("[UIManager] Start button clicked");
            _gameStateService?.ChangeState(GameState.LoadingGame);
        }

        private void OnRestartButtonClicked()
        {
            Debug.Log("[UIManager] Restart button clicked");
            _resetCallback?.Invoke();
            _gameStateService?.ChangeState(GameState.LoadingGame);
        }

        private void OnMenuButtonClicked()
        {
            Debug.Log("[UIManager] Menu button clicked");
            _gameStateService?.ChangeState(GameState.ReturnToMenu);
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
        }

        #endregion
    }
}