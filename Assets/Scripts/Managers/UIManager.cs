using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardWar.Services;
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
        
        [Header("Main Menu")]
        [SerializeField] private Button _startButton;
        
        [Header("Loading Screen")]
        [SerializeField] private Slider _loadingProgressBar;
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private TextMeshProUGUI _loadingPercentage;
        
        [Header("Game Over Screen")]
        [SerializeField] private TextMeshProUGUI _gameOverTitle;
        [SerializeField] private TextMeshProUGUI _gameOverMessage;
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _mainMenuButton;
        
        [Header("Game UI")]
        [SerializeField] private GameUIView _gameUIView;
        
        private IDIService _diService;
        private IGameStateService _gameStateService;
        private IGameControllerService _gameController;
        private IAudioService _audioService;
        
        private UIState _currentUIState;
        private Action _resetCallback;
        private float _targetLoadingProgress;
        private float _currentLoadingProgress;

        public UIState CurrentUIState => _currentUIState;

        public event Action<string> OnAnimationStarted;
        public event Action<string> OnAnimationCompleted;
        public event Action<UIState> OnUIStateChanged;

        [Inject]
        public void Initialize(IDIService diService, IGameStateService gameStateService)
        {
            _diService = diService;
            _gameStateService = gameStateService;
            _diService.RegisterService<IUIService>(this);
            
            SetUIState(UIState.FirstEntry);
            
            GetServices();
            SetupButtons();
            SubscribeToEvents();
            InitializeUI();
            
            SetUIState(UIState.Idle);
        }

        private void GetServices()
        {
            _audioService = _diService.GetService<IAudioService>();
        }

        private void SetupButtons()
        {
            _startButton.onClick.RemoveAllListeners();
            _startButton.onClick.AddListener(HandleStartButtonClick);
            
            _playAgainButton.onClick.RemoveAllListeners();
            _playAgainButton.onClick.AddListener(HandlePlayAgainButtonClick);
            
            _mainMenuButton.onClick.RemoveAllListeners();
            _mainMenuButton.onClick.AddListener(HandleMainMenuButtonClick);
        }

        private void SubscribeToEvents()
        {
            if (_gameStateService != null)
            {
                _gameStateService.OnGameStateChanged += HandleGameStateChanged;
                _gameStateService.OnLoadingProgress += HandleLoadingProgress;
            }
        }

        private void InitializeUI()
        {
            HideAllLayers();
            ShowMainMenu(true);
        }

        private void HandleStartButtonClick()
        {
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            _gameStateService?.ChangeState(GameState.LoadingGame);
        }

        private void HandleSettingsButtonClick()
        {
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            Debug.Log("[UIManager] Settings not implemented yet");
        }

        private void HandleQuitButtonClick()
        {
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void HandlePlayAgainButtonClick()
        {
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            _resetCallback?.Invoke();
            _gameStateService?.ChangeState(GameState.LoadingGame);
        }

        private void HandleMainMenuButtonClick()
        {
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            _gameStateService?.ChangeState(GameState.ReturnToMenu);
        }

        private void HandleGameStateChanged(GameState newState, GameState previousState)
        {
            Debug.Log($"[UIManager] UI handling state change: {previousState} -> {newState}");
            
            switch (newState)
            {
                case GameState.MainMenu:
                    ShowMainMenu(true);
                    ShowLoadingScreen(false);
                    ShowGameUI(false);
                    ShowGameOverScreen(false, false);
                    break;
                    
                case GameState.LoadingGame:
                    ShowMainMenu(false);
                    ShowLoadingScreen(true);
                    ResetLoadingProgress();
                    break;
                    
                case GameState.Playing:
                    ShowLoadingScreen(false);
                    ShowGameUI(true);
                    break;
                    
                case GameState.GameEnded:
                    DetermineWinnerAndShowGameOver();
                    break;
            }
        }

        private void DetermineWinnerAndShowGameOver()
        {
            var playerWon = UnityEngine.Random.value > 0.5f;
            ShowGameOverScreen(true, playerWon);
        }

        private void HandleLoadingProgress(float progress)
        {
            _targetLoadingProgress = progress;
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
                    var loadingDots = new string('.', (int)(Time.time * 2) % 4);
                    _loadingText.text = $"Loading{loadingDots}";
                }
            }
        }

        private void ResetLoadingProgress()
        {
            _currentLoadingProgress = 0f;
            _targetLoadingProgress = 0f;
            
            if (_loadingProgressBar != null)
                _loadingProgressBar.value = 0f;
        }

        private void HideAllLayers()
        {
            if (_mainMenuLayer != null) _mainMenuLayer.SetActive(false);
            if (_loadingLayer != null) _loadingLayer.SetActive(false);
            if (_gameLayer != null) _gameLayer.SetActive(false);
            if (_gameOverLayer != null) _gameOverLayer.SetActive(false);
        }

        public void ShowLoadingScreen(bool show)
        {
            _loadingLayer.SetActive(show);
            if (show)
            {
                SetUIState(UIState.Loading);
            }
        }

        public void ShowMainMenu(bool show)
        {
            _mainMenuLayer.SetActive(show);
            if (show)
            {
                SetUIState(UIState.Idle);
            }
        }

        public void ShowGameUI(bool show)
        {
            if (_gameLayer != null)
            {
                _gameLayer.SetActive(show);
                if (show)
                {
                    SetUIState(UIState.Idle);
                    _gameUIView?.ResetUI();
                }
            }
        }

        public void ShowGameOverScreen(bool show, bool playerWon)
        {
            if (_gameOverLayer != null)
            {
                _gameOverLayer.SetActive(show);
                
                if (show)
                {
                    SetUIState(UIState.Idle);
                    
                    if (_gameOverTitle != null)
                    {
                        _gameOverTitle.text = playerWon ? "Victory!" : "Defeat";
                        _gameOverTitle.color = playerWon ? Color.green : Color.red;
                    }
                    
                    if (_gameOverMessage != null)
                    {
                        _gameOverMessage.text = playerWon ? 
                            "Congratulations! You've won the war!" : 
                            "Better luck next time!";
                    }
                    
                    _audioService?.PlaySound(playerWon ? SoundEffect.Victory : SoundEffect.Defeat);
                }
            }
        }

        public void SetUIState(UIState state)
        {
            if (_currentUIState != state)
            {
                var previousState = _currentUIState;
                _currentUIState = state;
                Debug.Log($"[UIManager] UI State: {previousState} -> {_currentUIState}");
                OnUIStateChanged?.Invoke(_currentUIState);
            }
        }

        public void RegisterResetCallback(Action callback)
        {
            _resetCallback = callback;
        }

        public void NotifyAnimationStarted(string animationId)
        {
            SetUIState(UIState.Animating);
            OnAnimationStarted?.Invoke(animationId);
        }

        public void NotifyAnimationCompleted(string animationId)
        {
            SetUIState(UIState.Idle);
            OnAnimationCompleted?.Invoke(animationId);
        }

        public void Dispose()
        {
            if (_gameStateService != null)
            {
                _gameStateService.OnGameStateChanged -= HandleGameStateChanged;
                _gameStateService.OnLoadingProgress -= HandleLoadingProgress;
            }
            
            OnAnimationStarted = null;
            OnAnimationCompleted = null;
            OnUIStateChanged = null;
            _resetCallback = null;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}