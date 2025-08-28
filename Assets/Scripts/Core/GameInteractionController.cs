using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using CardWar.Services.Game;
using CardWar.Core.Enums;
using Cysharp.Threading.Tasks;

namespace CardWar.Core
{
    public class GameInteractionController : MonoBehaviour, IInitializable, IDisposable
    {
        [Header("Input Settings")]
        [SerializeField] private bool _enableTapToPlay = true;
        [SerializeField] private bool _enableKeyboardInput = true;
        [SerializeField] private KeyCode _drawCardKey = KeyCode.Space;
        
        [Header("UI References - Assign in Inspector")]
        [SerializeField] private Button _drawCardButton;
        
        private IGameService _gameService;
        private SignalBus _signalBus;
        private Camera _gameCamera;
        private Canvas _gameCanvas;
        private bool _gameStarted = false;
        
        [Inject]
        public void Construct(IGameService gameService, SignalBus signalBus, Camera gameCamera, Canvas gameCanvas)
        {
            _gameService = gameService;
            _signalBus = signalBus;
            _gameCamera = gameCamera;
            _gameCanvas = gameCanvas;
        }
        
        public void Initialize()
        {
            SetupUI();
            SubscribeToEvents();
        }
        
        private void SetupUI()
        {
            if (_drawCardButton != null)
            {
                _drawCardButton.onClick.AddListener(OnDrawCardClicked);
            }
            else
            {
                Debug.LogWarning("[GameInteractionController] Draw Card Button not assigned! Please assign it in the Inspector or use the Scene Setup Tool to create UI properly.");
            }
        }
        
        private void SubscribeToEvents()
        {
            if (_gameService != null)
            {
                _gameService.OnGameStateChanged += OnGameStateChanged;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_gameService != null)
            {
                _gameService.OnGameStateChanged -= OnGameStateChanged;
            }
        }
        
        private void Update()
        {
            if (!_gameStarted || _gameService?.CurrentGameState != GameState.Playing)
                return;
                
            HandleInput();
        }
        
        private void HandleInput()
        {
            bool inputDetected = false;
            
            if (_enableTapToPlay)
            {
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    inputDetected = true;
                }
            }
            
            if (_enableKeyboardInput && Input.GetKeyDown(_drawCardKey))
            {
                inputDetected = true;
            }
            
            if (inputDetected)
            {
                OnDrawCardClicked();
            }
        }
        
        private void OnDrawCardClicked()
        {
            if (_gameService == null) return;
            
            if (_gameService.CurrentGameState == GameState.Idle)
            {
                StartGame();
            }
            else if (_gameService.CurrentGameState == GameState.Playing)
            {
                PlayNextRound();
            }
        }
        
        private void StartGame()
        {
            if (_gameService != null)
            {
                _gameService.StartNewGame().Forget();
                _gameStarted = true;
                Debug.Log("[GameInteractionController] Game started via user input");
            }
        }
        
        private void PlayNextRound()
        {
            if (_gameService != null)
            {
                _gameService.PlayNextRound().Forget();
            }
        }
        
        private void OnGameStateChanged(GameState newState)
        {
            UpdateButtonText(newState);
            
            if (newState == GameState.GameOver)
            {
                _gameStarted = false;
            }
        }
        
        private void UpdateButtonText(GameState gameState)
        {
            if (_drawCardButton?.GetComponentInChildren<TMPro.TextMeshProUGUI>() is var buttonText && buttonText != null)
            {
                string text = gameState switch
                {
                    GameState.Idle => "TAP TO START",
                    GameState.Initializing => "STARTING...",
                    GameState.Playing => "TAP TO DRAW",
                    GameState.War => "WAR IN PROGRESS",
                    GameState.RoundComplete => "PROCESSING...",
                    GameState.GameOver => "GAME OVER - TAP TO RESTART",
                    GameState.Paused => "PAUSED",
                    _ => "TAP TO PLAY"
                };
                
                buttonText.text = text;
            }
        }
        
        public void Dispose()
        {
            Debug.Log("[GameInteractionController] Disposing");
            
            UnsubscribeFromEvents();
            
            if (_drawCardButton != null)
            {
                _drawCardButton.onClick.RemoveListener(OnDrawCardClicked);
            }
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
    }
}