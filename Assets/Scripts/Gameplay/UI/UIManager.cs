using System;
using UnityEngine;
using TMPro;
using DG.Tweening;
using Zenject;
using Cysharp.Threading.Tasks;
using CardWar.Core.Enums;
using CardWar.Infrastructure.Events;
using CardWar.Services.Game;

namespace CardWar.UI
{
    public class UIManager : MonoBehaviour, IInitializable, IDisposable
    {
        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI _playerScoreText;
        [SerializeField] private TextMeshProUGUI _opponentScoreText;
        [SerializeField] private TextMeshProUGUI _roundText;
        [SerializeField] private TextMeshProUGUI _gameStateText;
        
        [Header("Popup Elements")]
        [SerializeField] private GameObject _warIndicator;
        [SerializeField] private TextMeshProUGUI _warText;
        [SerializeField] private GameObject _gameOverScreen;
        [SerializeField] private TextMeshProUGUI _gameOverText;
        [SerializeField] private TextMeshProUGUI _winnerText;
        
        [Header("Animation Settings")]
        [SerializeField] private float _scoreUpdateDuration = 0.5f;
        [SerializeField] private float _popupFadeDuration = 0.3f;
        [SerializeField] private float _warDisplayDuration = 2f;
        
        private IGameService _gameService;
        private SignalBus _eventBus;
        
        private int _displayedPlayerScore = 26;
        private int _displayedOpponentScore = 26;
        private int _currentRound = 0;
        private Sequence _currentAnimation;
        
        [Inject]
        public void Construct(IGameService gameService, SignalBus signalBus)
        {
            _gameService = gameService;
            _eventBus = signalBus;
        }
        
        public void Initialize()
        {
            Debug.Log("[UIManager] Initializing");
            
            // Find UI elements if not assigned
            FindUIElements();
            
            // Subscribe to events
            SubscribeToEvents();
            
            // Initialize UI
            ResetUI();
            
            // Hide popups
            if (_warIndicator != null)
                _warIndicator.SetActive(false);
                
            if (_gameOverScreen != null)
                _gameOverScreen.SetActive(false);
        }
        
        private void FindUIElements()
        {
            // Try to find UI elements if not assigned in inspector
            if (_playerScoreText == null)
            {
                var playerScore = GameObject.Find("PlayerScore");
                if (playerScore != null)
                    _playerScoreText = playerScore.GetComponent<TextMeshProUGUI>();
            }
            
            if (_opponentScoreText == null)
            {
                var opponentScore = GameObject.Find("OpponentScore");
                if (opponentScore != null)
                    _opponentScoreText = opponentScore.GetComponent<TextMeshProUGUI>();
            }
            
            if (_roundText == null)
            {
                var roundCounter = GameObject.Find("RoundText");
                if (roundCounter != null)
                    _roundText = roundCounter.GetComponent<TextMeshProUGUI>();
            }
            
            if (_gameStateText == null)
            {
                var stateText = GameObject.Find("StateText");
                if (stateText != null)
                    _gameStateText = stateText.GetComponent<TextMeshProUGUI>();
            }
            
            if (_warIndicator == null)
                _warIndicator = GameObject.Find("WarIndicator");
            
            if (_warText == null && _warIndicator != null)
            {
                var warTextObj = _warIndicator.transform.Find("WarText");
                if (warTextObj != null)
                    _warText = warTextObj.GetComponent<TextMeshProUGUI>();
            }
            
            if (_gameOverScreen == null)
                _gameOverScreen = GameObject.Find("GameOverScreen");
            
            if (_gameOverText == null && _gameOverScreen != null)
            {
                var gameOverTextObj = _gameOverScreen.transform.Find("GameOverText");
                if (gameOverTextObj != null)
                    _gameOverText = gameOverTextObj.GetComponent<TextMeshProUGUI>();
            }
            
            if (_winnerText == null && _gameOverScreen != null)
            {
                var winnerTextObj = _gameOverScreen.transform.Find("WinnerText");
                if (winnerTextObj != null)
                    _winnerText = winnerTextObj.GetComponent<TextMeshProUGUI>();
            }
        }
        
        private void SubscribeToEvents()
        {
            _gameService.OnGameStateChanged += OnGameStateChanged;
            
            _eventBus.Subscribe<GameStartEvent>(OnGameStart);
            _eventBus.Subscribe<GameEndEvent>(OnGameEnd);
            _eventBus.Subscribe<RoundCompleteEvent>(OnRoundComplete);
            _eventBus.Subscribe<WarStartEvent>(OnWarStart);
            _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChangedEvent);
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_gameService != null)
                _gameService.OnGameStateChanged -= OnGameStateChanged;
            
            _eventBus.TryUnsubscribe<GameStartEvent>(OnGameStart);
            _eventBus.TryUnsubscribe<GameEndEvent>(OnGameEnd);
            _eventBus.TryUnsubscribe<RoundCompleteEvent>(OnRoundComplete);
            _eventBus.TryUnsubscribe<WarStartEvent>(OnWarStart);
            _eventBus.TryUnsubscribe<GameStateChangedEvent>(OnGameStateChangedEvent);
        }
        
        private void ResetUI()
        {
            _displayedPlayerScore = 26;
            _displayedOpponentScore = 26;
            _currentRound = 0;
            
            UpdateScoreDisplay();
            UpdateRoundDisplay();
            UpdateGameStateDisplay("Initializing...");
        }
        
        private void UpdateScoreDisplay()
        {
            if (_playerScoreText != null)
                _playerScoreText.text = $"Player: {_displayedPlayerScore}";
                
            if (_opponentScoreText != null)
                _opponentScoreText.text = $"Opponent: {_displayedOpponentScore}";
        }
        
        private void UpdateRoundDisplay()
        {
            if (_roundText != null)
                _roundText.text = $"Round: {_currentRound}";
        }
        
        private void UpdateGameStateDisplay(string state)
        {
            if (_gameStateText != null)
                _gameStateText.text = state;
        }
        
        private void AnimateScoreUpdate(int newPlayerScore, int newOpponentScore)
        {
            KillCurrentAnimation();
            
            // Animate score changes
            if (_playerScoreText != null && newPlayerScore != _displayedPlayerScore)
            {
                int startScore = _displayedPlayerScore;
                DOTween.To(() => startScore, x =>
                {
                    _displayedPlayerScore = x;
                    _playerScoreText.text = $"Player: {x}";
                }, newPlayerScore, _scoreUpdateDuration);
                
                // Punch scale for emphasis
                _playerScoreText.transform.DOPunchScale(Vector3.one * 0.2f, _scoreUpdateDuration);
            }
            
            if (_opponentScoreText != null && newOpponentScore != _displayedOpponentScore)
            {
                int startScore = _displayedOpponentScore;
                DOTween.To(() => startScore, x =>
                {
                    _displayedOpponentScore = x;
                    _opponentScoreText.text = $"Opponent: {x}";
                }, newOpponentScore, _scoreUpdateDuration);
                
                // Punch scale for emphasis
                _opponentScoreText.transform.DOPunchScale(Vector3.one * 0.2f, _scoreUpdateDuration);
            }
        }
        
        private async UniTask ShowWarIndicator()
        {
            if (_warIndicator == null) return;
            
            _warIndicator.SetActive(true);
            _warIndicator.transform.localScale = Vector3.zero;
            
            // Dramatic entrance
            await _warIndicator.transform.DOScale(1.2f, 0.3f)
                .SetEase(Ease.OutBack)
                .AsyncWaitForCompletion();
            
            // Pulsing effect
            _warIndicator.transform.DOScale(1f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
            
            // Auto-hide after duration
            await UniTask.Delay(TimeSpan.FromSeconds(_warDisplayDuration));
            
            await HideWarIndicator();
        }
        
        private async UniTask HideWarIndicator()
        {
            if (_warIndicator == null) return;
            
            DOTween.Kill(_warIndicator.transform);
            
            await _warIndicator.transform.DOScale(0, 0.2f)
                .SetEase(Ease.InBack)
                .AsyncWaitForCompletion();
            
            _warIndicator.SetActive(false);
        }
        
        private void ShowGameOverScreen(int winnerPlayerNumber)
        {
            if (_gameOverScreen == null) return;
            
            _gameOverScreen.SetActive(true);
            
            if (_winnerText != null)
            {
                string winner = winnerPlayerNumber == 1 ? "PLAYER WINS!" : "OPPONENT WINS!";
                _winnerText.text = winner;
                _winnerText.color = winnerPlayerNumber == 1 ? Color.green : Color.red;
            }
            
            // Fade in animation
            CanvasGroup canvasGroup = _gameOverScreen.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = _gameOverScreen.AddComponent<CanvasGroup>();
            
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, _popupFadeDuration);
            
            // Scale animation
            _gameOverScreen.transform.localScale = Vector3.zero;
            _gameOverScreen.transform.DOScale(1, _popupFadeDuration)
                .SetEase(Ease.OutBack);
        }
        
        private void HideGameOverScreen()
        {
            if (_gameOverScreen == null) return;
            
            CanvasGroup canvasGroup = _gameOverScreen.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0, _popupFadeDuration)
                    .OnComplete(() => _gameOverScreen.SetActive(false));
            }
            else
            {
                _gameOverScreen.SetActive(false);
            }
        }
        
        // Event Handlers
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
        
        private void OnGameStart()
        {
            Debug.Log("[UIManager] Game started");
            ResetUI();
            HideGameOverScreen();
            UpdateGameStateDisplay("Tap to Draw");
        }
        
        private void OnGameEnd(GameEndEvent eventData)
        {
            Debug.Log($"[UIManager] Game ended. Winner: Player {eventData.WinnerPlayerNumber}");
            ShowGameOverScreen(eventData.WinnerPlayerNumber);
        }
        
        private void OnRoundComplete(RoundCompleteEvent eventData)
        {
            _currentRound++;
            UpdateRoundDisplay();
            
            // Update scores based on game service
            AnimateScoreUpdate(_gameService.PlayerCardCount, _gameService.OpponentCardCount);
            
            // Show round result feedback
            string resultText = eventData.Result.Result switch
            {
                GameResult.PlayerWin => "You Win!",
                GameResult.OpponentWin => "Opponent Wins!",
                GameResult.War => "WAR!",
                _ => ""
            };
            
            // Brief feedback
            if (_gameStateText != null && !string.IsNullOrEmpty(resultText))
            {
                _gameStateText.text = resultText;
                _gameStateText.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f);
            }
        }
        
        private void OnWarStart(WarStartEvent eventData)
        {
            Debug.Log($"[UIManager] War started with {eventData.WarData.AllWarRounds.Count} rounds");
            ShowWarIndicator().Forget();
        }
        
        private void OnGameStateChangedEvent(GameStateChangedEvent eventData)
        {
            // Additional state change handling if needed
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
            Debug.Log("[UIManager] Disposing");
            
            UnsubscribeFromEvents();
            KillCurrentAnimation();
            
            DOTween.Kill(_playerScoreText?.transform);
            DOTween.Kill(_opponentScoreText?.transform);
            DOTween.Kill(_warIndicator?.transform);
            DOTween.Kill(_gameOverScreen?.transform);
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
    }
}