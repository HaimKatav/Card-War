using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardWar.Services;
using CardWar.Core;
using CardWar.Game.UI;
using Cysharp.Threading.Tasks;

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
        [SerializeField] private TextMeshProUGUI _gameOverTitle;
        [SerializeField] private TextMeshProUGUI _gameOverMessage;
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _mainMenuButton;
        
        [Inject] private IDIService _diService;
        [Inject] private IGameStateService _gameStateService;
        [Inject] private GameSettings _gameSettings;
        
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

        [Initialize]
        public void Initialize()
        {
            if (_isInitialized) return;
            
            SetUIState(UIState.FirstEntry);
            
            GetServices();
            FindOrLoadUICanvas();
            SetupButtons();
            SubscribeToEvents();
            InitializeUI();
            
            _isInitialized = true;
            SetUIState(UIState.Idle);
            
            Debug.Log($"[{GetType().Name}] Initialized successfully");
        }
        
        private void GetServices()
        {
            _audioService = _diService.GetService<IAudioService>();
            _assetService = _diService.GetService<IAssetService>();
            _gameController = _diService.GetService<IGameControllerService>();
            
            Debug.Log($"[{GetType().Name}] Services retrieved - Audio: {_audioService != null}, Asset: {_assetService != null}, GameController: {_gameController != null}");
        }

        private void FindOrLoadUICanvas()
        {
            if (_mainMenuLayer != null && _loadingLayer != null && _gameLayer != null && _gameOverLayer != null)
            {
                Debug.Log($"[{GetType().Name}] UI layers already assigned via Inspector");
                return;
            }
            
            var gameCanvas = GameObject.Find("GameCanvas");
            if (gameCanvas == null)
            {
                Debug.LogWarning($"[{GetType().Name}] GameCanvas not found, attempting to load from Resources");
                LoadGameCanvasFromResources();
            }
            else
            {
                FindUIElementsFromCanvas(gameCanvas);
            }
        }

        private void LoadGameCanvasFromResources()
        {
            var canvasPrefab = Resources.Load<GameObject>("Prefabs/GameCanvas");
            if (canvasPrefab == null)
            {
                Debug.LogError($"[{GetType().Name}] Failed to load GameCanvas from Resources/Prefabs/GameCanvas");
                return;
            }
            
            var canvasInstance = Instantiate(canvasPrefab);
            canvasInstance.name = "GameCanvas";
            DontDestroyOnLoad(canvasInstance);
            
            FindUIElementsFromCanvas(canvasInstance);
        }

        private void FindUIElementsFromCanvas(GameObject gameCanvas)
        {
            var transform = gameCanvas.transform;
            
            if (_mainMenuLayer == null)
                _mainMenuLayer = transform.Find("MainMenuLayer")?.gameObject;
            if (_loadingLayer == null)
                _loadingLayer = transform.Find("LoadingLayer")?.gameObject;
            if (_gameLayer == null)
                _gameLayer = transform.Find("GameLayer")?.gameObject;
            if (_gameOverLayer == null)
                _gameOverLayer = transform.Find("GameOverLayer")?.gameObject;
            
            if (_mainMenuLayer != null && _startButton == null)
            {
                _startButton = _mainMenuLayer.GetComponentInChildren<Button>();
            }
            
            if (_loadingLayer != null)
            {
                if (_loadingProgressBar == null)
                    _loadingProgressBar = _loadingLayer.GetComponentInChildren<Slider>();
                
                var texts = _loadingLayer.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var text in texts)
                {
                    if (_loadingText == null && text.name.Contains("Loading") && !text.name.Contains("Percentage"))
                        _loadingText = text;
                    else if (_loadingPercentage == null && text.name.Contains("Percentage"))
                        _loadingPercentage = text;
                }
            }
            
            if (_gameLayer != null)
            {
                if (_playAreaParent == null)
                {
                    _playAreaParent = _gameLayer.transform.Find("PlayAreaParent");
                    if (_playAreaParent == null)
                    {
                        var parentGO = new GameObject("PlayAreaParent");
                        parentGO.transform.SetParent(_gameLayer.transform, false);
                        _playAreaParent = parentGO.transform;
                        
                        var rectTransform = parentGO.AddComponent<RectTransform>();
                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.sizeDelta = Vector2.zero;
                        rectTransform.anchoredPosition = Vector2.zero;
                    }
                }
                
                _gameUIView = _gameLayer.GetComponentInChildren<GameUIView>();
            }
            
            if (_gameOverLayer != null)
            {
                var texts = _gameOverLayer.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var text in texts)
                {
                    if (_gameOverTitle == null && text.name.Contains("Title"))
                        _gameOverTitle = text;
                    else if (_gameOverMessage == null && text.name.Contains("Message"))
                        _gameOverMessage = text;
                }
                
                var buttons = _gameOverLayer.GetComponentsInChildren<Button>();
                foreach (var button in buttons)
                {
                    if (_playAgainButton == null && button.name.Contains("PlayAgain"))
                        _playAgainButton = button;
                    else if (_mainMenuButton == null && button.name.Contains("MainMenu"))
                        _mainMenuButton = button;
                }
            }
            
            Debug.Log($"[{GetType().Name}] UI Elements found - Menu: {_mainMenuLayer != null}, Loading: {_loadingLayer != null}, Game: {_gameLayer != null}, GameOver: {_gameOverLayer != null}");
        }

        #endregion

        #region Button Setup

        private void SetupButtons()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveAllListeners();
                _startButton.onClick.AddListener(HandleStartButtonClick);
                Debug.Log($"[{GetType().Name}] Start button configured");
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] Start button not assigned");
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
                Debug.Log($"[{GetType().Name}] Subscribed to game state events");
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] Cannot subscribe to events - GameStateService is null");
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

        private void InitializeUI()
        {
            HideAllLayers();
            ShowMainMenu(true);
            Debug.Log($"[{GetType().Name}] Initial UI state set to Main Menu");
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
            if (_loadingLayer != null)
            {
                _loadingLayer.SetActive(show);
                if (show)
                {
                    SetUIState(UIState.Loading);
                    ResetLoadingProgress();
                }
                Debug.Log($"[{GetType().Name}] Loading screen: {show}");
            }
        }

        public void ShowMainMenu(bool show)
        {
            if (_mainMenuLayer != null)
            {
                _mainMenuLayer.SetActive(show);
                Debug.Log($"[{GetType().Name}] Main menu: {show}");
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

        public void ShowGameOverScreen(bool show, bool playerWon)
        {
            if (_gameOverLayer != null)
            {
                _gameOverLayer.SetActive(show);
                
                if (show && _gameOverTitle != null && _gameOverMessage != null)
                {
                    _gameOverTitle.text = playerWon ? "Victory!" : "Defeat!";
                    _gameOverMessage.text = playerWon 
                        ? "Congratulations! You've won the war!" 
                        : "Better luck next time!";
                }
                
                Debug.Log($"[{GetType().Name}] Game over screen: {show}, Player won: {playerWon}");
            }
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
            ShowGameOverScreen(false, false);
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
                    ShowGameOverScreen(false, false);
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
            ShowGameOverScreen(true, playerWon);
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