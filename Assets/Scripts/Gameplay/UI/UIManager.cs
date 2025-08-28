using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Zenject;
using CardWar.Core.Enums;
using CardWar.Infrastructure.Events;
using CardWar.Services.Game;

namespace CardWar.Core.UI
{
    public class UIManager : MonoBehaviour, IInitializable, IDisposable
    {
        [Header("Score UI")]
        [SerializeField] private TextMeshProUGUI _playerScoreText;
        [SerializeField] private TextMeshProUGUI _opponentScoreText;
        [SerializeField] private TextMeshProUGUI _roundCounterText;
        
        [Header("Game State UI")]
        [SerializeField] private TextMeshProUGUI _gameStateText;
        [SerializeField] private GameObject _warIndicator;
        [SerializeField] private GameObject _gameOverScreen;
        
        [Header("Game Over UI")]
        [SerializeField] private TextMeshProUGUI _winnerText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;
        
        private IGameService _gameService;
        private SignalBus _signalBus;
        private Tween _currentAnimation;
        private bool _isInitialized = false;
        
        private int _currentRound = 0;
        private int _displayedPlayerScore = 26;
        private int _displayedOpponentScore = 26;
        
        [Inject]
        public void Construct(IGameService gameService, SignalBus signalBus)
        {
            _gameService = gameService;
            _signalBus = signalBus;
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            
            InitializeUI();
            SubscribeToEvents();
            _isInitialized = true;
            
            Debug.Log("[UIManager] Initialized successfully");
        }
        
        private void InitializeUI()
        {
            ResetScoreDisplay();
            ResetRoundDisplay();
            UpdateGameStateDisplay("Tap to Start");
            HideAllPopups();
        }
        
        private void ResetScoreDisplay()
        {
            _displayedPlayerScore = 26;
            _displayedOpponentScore = 26;
            
            if (_playerScoreText != null)
                _playerScoreText.text = $"Player: {_displayedPlayerScore}";
            
            if (_opponentScoreText != null)
                _opponentScoreText.text = $"Opponent: {_displayedOpponentScore}";
        }
        
        private void ResetRoundDisplay()
        {
            _currentRound = 0;
            
            if (_roundCounterText != null)
                _roundCounterText.text = $"Round: {_currentRound}";
        }
        
        private void UpdateGameStateDisplay(string message)
        {
            if (_gameStateText != null)
                _gameStateText.text = message;
        }
        
        private void HideAllPopups()
        {
            if (_warIndicator != null)
                _warIndicator.SetActive(false);
            
            if (_gameOverScreen != null)
                _gameOverScreen.SetActive(false);
        }
        
        private void SubscribeToEvents()
        {
            if (_signalBus == null) 
            {
                Debug.LogError("[UIManager] SignalBus is null, cannot subscribe to events");
                return;
            }
            
            try
            {
                _signalBus.Subscribe<GameStartEvent>(OnGameStart);
                _signalBus.Subscribe<RoundCompleteEvent>(OnRoundComplete);
                _signalBus.Subscribe<GameEndEvent>(OnGameEnd);
                _signalBus.Subscribe<WarStartEvent>(OnWarStart);
                _signalBus.Subscribe<GameStateChangedEvent>(OnGameStateChangedEvent);
                
                // Subscribe to game service events
                if (_gameService != null)
                {
                    _gameService.OnGameStateChanged += OnGameStateChanged;
                }
                
                Debug.Log("[UIManager] Events subscribed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UIManager] Failed to subscribe to events: {ex.Message}");
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_signalBus == null) return;
            
            try
            {
                _signalBus.TryUnsubscribe<GameStartEvent>(OnGameStart);
                _signalBus.TryUnsubscribe<RoundCompleteEvent>(OnRoundComplete);
                _signalBus.TryUnsubscribe<GameEndEvent>(OnGameEnd);
                _signalBus.TryUnsubscribe<WarStartEvent>(OnWarStart);
                _signalBus.TryUnsubscribe<GameStateChangedEvent>(OnGameStateChangedEvent);
                
                // Unsubscribe from game service events
                if (_gameService != null)
                {
                    _gameService.OnGameStateChanged -= OnGameStateChanged;
                }
                
                Debug.Log("[UIManager] Events unsubscribed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UIManager] Error unsubscribing events: {ex.Message}");
            }
        }
        
        private void OnGameStart(GameStartEvent eventData)
        {
            Debug.Log("[UIManager] Game started");
            
            ResetScoreDisplay();
            ResetRoundDisplay();
            UpdateGameStateDisplay("Game Started!");
            HideAllPopups();
        }
        
        private void OnRoundComplete(RoundCompleteEvent eventData)
        {
            _currentRound++;
            UpdateRoundDisplay();
            
            // Get current scores from game service (this is the source of truth)
            int playerScore = _gameService?.PlayerCardCount ?? 0;
            int opponentScore = _gameService?.OpponentCardCount ?? 0;
            
            UpdateScoreDisplay(playerScore, opponentScore);
            DisplayRoundResult(eventData.Result.Result);
            
            Debug.Log($"[UIManager] Round {_currentRound} complete - Player: {playerScore}, Opponent: {opponentScore}");
        }
        
        private void OnGameEnd(GameEndEvent eventData)
        {
            Debug.Log($"[UIManager] Game ended - Winner: Player {eventData.WinnerPlayerNumber}");
            ShowGameOverScreen(eventData.WinnerPlayerNumber);
        }
        
        private void OnWarStart(WarStartEvent eventData)
        {
            Debug.Log($"[UIManager] War started");
            ShowWarIndicator();
        }
        
        private void OnGameStateChangedEvent(GameStateChangedEvent eventData)
        {
            Debug.Log($"[UIManager] Game state changed to: {eventData.NewState}");
        }
        
        private void OnGameStateChanged(GameState newState)
        {
            string stateText = newState switch
            {
                GameState.Idle => "Waiting...",
                GameState.Initializing => "Starting Game...",
                GameState.Playing => "Tap to Draw",
                GameState.War => "WAR!",
                GameState.RoundComplete => "Processing...",
                GameState.GameOver => "Game Over",
                GameState.Paused => "Paused",
                _ => newState.ToString()
            };
            
            UpdateGameStateDisplay(stateText);
        }
        
        private void UpdateRoundDisplay()
        {
            if (_roundCounterText != null)
                _roundCounterText.text = $"Round: {_currentRound}";
        }
        
        private void UpdateScoreDisplay(int playerScore, int opponentScore)
        {
            // Animate score changes
            if (_playerScoreText != null)
            {
                AnimateScoreChange(_playerScoreText, _displayedPlayerScore, playerScore, "Player");
                _displayedPlayerScore = playerScore;
            }
            
            if (_opponentScoreText != null)
            {
                AnimateScoreChange(_opponentScoreText, _displayedOpponentScore, opponentScore, "Opponent");
                _displayedOpponentScore = opponentScore;
            }
        }
        
        private void AnimateScoreChange(TextMeshProUGUI scoreText, int oldScore, int newScore, string label)
        {
            if (scoreText == null) return;
            
            // Update text immediately
            scoreText.text = $"{label}: {newScore}";
            
            // Add punch animation if score changed
            if (oldScore != newScore)
            {
                scoreText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
            }
        }
        
        private void DisplayRoundResult(GameResult result)
        {
            if (_gameStateText == null) return;
            
            string resultText = result switch
            {
                GameResult.PlayerWin => "Player Wins Round!",
                GameResult.OpponentWin => "Opponent Wins Round!",
                GameResult.War => "WAR!",
                _ => ""
            };
            
            if (!string.IsNullOrEmpty(resultText))
            {
                UpdateGameStateDisplay(resultText);
                
                // Add emphasis animation
                _gameStateText.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f);
            }
        }
        
        private void ShowWarIndicator()
        {
            if (_warIndicator == null) return;
            
            _warIndicator.SetActive(true);
            
            // Animate war indicator
            _warIndicator.transform.localScale = Vector3.zero;
            _warIndicator.transform.DOScale(1f, 0.5f)
                .SetEase(Ease.OutBounce)
                .OnComplete(() => {
                    // Auto-hide after 2 seconds
                    _warIndicator.transform.DOScale(0f, 0.3f)
                        .SetDelay(2f)
                        .OnComplete(() => _warIndicator.SetActive(false));
                });
        }
        
        private void ShowGameOverScreen(int winnerPlayerNumber)
        {
            if (_gameOverScreen == null) return;
            
            _gameOverScreen.SetActive(true);
            
            // Update winner text
            if (_winnerText != null)
            {
                _winnerText.text = winnerPlayerNumber == 1 ? "You Win!" : "Opponent Wins!";
            }
            
            // Animate game over screen
            _gameOverScreen.transform.localScale = Vector3.zero;
            _gameOverScreen.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBounce);
            
            // Setup buttons if they exist
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveAllListeners();
                _restartButton.onClick.AddListener(() => {
                    Debug.Log("[UIManager] Restart button clicked");
                    // TODO: Implement restart logic
                });
            }
            
            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveAllListeners();
                _quitButton.onClick.AddListener(() => {
                    Debug.Log("[UIManager] Quit button clicked");
                    // TODO: Implement quit logic
                });
            }
        }
        
        private void KillCurrentAnimation()
        {
            if (_currentAnimation != null && _currentAnimation.IsActive())
            {
                _currentAnimation.Kill();
                _currentAnimation = null;
            }
        }
        
        public void Dispose()
        {
            if (!_isInitialized) return;
    
            Debug.Log("[UIManager] Disposing");
    
            UnsubscribeFromEvents();
            KillCurrentAnimation();
    
            try
            {
                if (_playerScoreText != null && _playerScoreText.gameObject != null)
                    DOTween.Kill(_playerScoreText.transform);
            }
            catch (Exception) {}
    
            try
            {
                if (_opponentScoreText != null && _opponentScoreText.gameObject != null)
                    DOTween.Kill(_opponentScoreText.transform);
            }
            catch (Exception) {}
    
            try
            {
                if (_gameStateText != null && _gameStateText.gameObject != null)
                    DOTween.Kill(_gameStateText.transform);
            }
            catch (Exception) {}
    
            try
            {
                if (_warIndicator != null && _warIndicator != null)
                    DOTween.Kill(_warIndicator.transform);
            }
            catch (Exception) {}
    
            try
            {
                if (_gameOverScreen != null && _gameOverScreen != null)
                    DOTween.Kill(_gameOverScreen.transform);
            }
            catch (Exception) {}
    
            _isInitialized = false;
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
    }
}