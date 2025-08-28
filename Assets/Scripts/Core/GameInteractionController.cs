using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using CardWar.Services.Game;
using CardWar.Core.Enums;

namespace CardWar.Core
{
    public class GameInteractionController : MonoBehaviour, IInitializable, IDisposable
    {
        [Header("Input Settings")]
        [SerializeField] private bool _enableTapToPlay = true;
        [SerializeField] private bool _enableKeyboardInput = true;
        [SerializeField] private KeyCode _drawCardKey = KeyCode.Space;
        
        [Header("UI References")]
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
            if (_drawCardButton == null && _gameCanvas != null)
            {
                CreateDrawCardButton();
            }
            
            if (_drawCardButton != null)
            {
                _drawCardButton.onClick.AddListener(OnDrawCardClicked);
            }
        }
        
        private void CreateDrawCardButton()
        {
            var buttonObj = new GameObject("DrawCardButton");
            buttonObj.transform.SetParent(_gameCanvas.transform, false);
            
            var rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.2f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.2f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(200, 60);
            
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            
            var button = buttonObj.AddComponent<Button>();
            
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            var text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.text = "TAP TO DRAW";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            
            _drawCardButton = button;
        }
        
        private void SubscribeToEvents()
        {
            _gameService.OnGameStateChanged += OnGameStateChanged;
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_gameService != null)
                _gameService.OnGameStateChanged -= OnGameStateChanged;
        }
        
        private void Update()
        {
            if (_enableTapToPlay && UnityEngine.Input.GetMouseButtonDown(0))
            {
                HandleScreenTap();
            }
            
            if (_enableKeyboardInput && UnityEngine.Input.GetKeyDown(_drawCardKey))
            {
                OnDrawCardClicked();
            }
        }
        
        private void HandleScreenTap()
        {
            if (CanProcessInput())
            {
                OnDrawCardClicked();
            }
        }
        
        private bool CanProcessInput()
        {
            if (_gameService == null) return false;
            
            var currentState = _gameService.CurrentGameState;
            
            return currentState == GameState.Playing || 
                   currentState == GameState.Idle ||
                   (!_gameStarted && currentState == GameState.Initializing);
        }
        
        private void OnDrawCardClicked()
        {
            if (_gameService == null) return;
            
            try
            {
                if (!_gameStarted)
                {
                    _gameService.StartNewGame();
                    _gameStarted = true;
                }
                else if (CanProcessInput())
                {
                    _gameService.PlayNextRound();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameInteractionController] Error during game action: {ex.Message}");
            }
        }
        
        private void OnGameStateChanged(GameState newState)
        {
            UpdateButtonState(newState);
            UpdateButtonText(newState);
        }
        
        private void UpdateButtonState(GameState gameState)
        {
            if (_drawCardButton == null) return;
            
            bool isInteractable = gameState switch
            {
                GameState.Playing => true,
                GameState.Idle => true,
                GameState.Initializing => !_gameStarted,
                GameState.RoundComplete => false,
                GameState.War => false,
                GameState.GameOver => false,
                GameState.Paused => false,
                _ => false
            };
            
            _drawCardButton.interactable = isInteractable;
            
            var buttonImage = _drawCardButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = isInteractable ? 
                    new Color(0.2f, 0.6f, 1f, 0.8f) : 
                    new Color(0.5f, 0.5f, 0.5f, 0.8f);
            }
        }
        
        private void UpdateButtonText(GameState gameState)
        {
            if (_drawCardButton == null) return;
            
            var buttonText = _drawCardButton.GetComponentInChildren<UnityEngine.UI.Text>();
            if (buttonText == null) return;
            
            string text = gameState switch
            {
                GameState.Initializing when !_gameStarted => "START GAME",
                GameState.Playing => "DRAW CARD",
                GameState.Idle => "DRAW CARD",
                GameState.RoundComplete => "PROCESSING...",
                GameState.War => "WAR!",
                GameState.GameOver => "GAME OVER",
                GameState.Paused => "PAUSED",
                _ => "WAITING..."
            };
            
            buttonText.text = text;
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
        
        [ContextMenu("Force Start Game")]
        private void ForceStartGame()
        {
            if (_gameService != null)
            {
                _gameService.StartNewGame();
                _gameStarted = true;
            }
        }
        
        [ContextMenu("Force Play Round")]
        private void ForcePlayRound()
        {
            if (_gameService != null)
            {
                _gameService.PlayNextRound();
            }
        }
    }
}