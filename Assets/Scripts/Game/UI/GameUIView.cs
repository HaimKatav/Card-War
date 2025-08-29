using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardWar.Services;
using UnityEngine.Events;
using Zenject;

namespace CardWar.Game.UI
{
    public class GameUIView : MonoBehaviour
    {
        [Header("Card Counts")]
        [SerializeField] private TextMeshProUGUI _playerCardCount;
        [SerializeField] private TextMeshProUGUI _opponentCardCount;
        
        [Header("Round Info")]
        [SerializeField] private TextMeshProUGUI _roundNumber;
        [SerializeField] private TextMeshProUGUI _warIndicator;
        [SerializeField] private TextMeshProUGUI _resultText;
        
        [Header("Control Buttons")]
        [SerializeField] private Button _drawButton;
        [SerializeField] private Button _pauseButton;
        
        [Header("Panels")]
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _quitButton;
        
        private IGameControllerService _gameController;
        private IGameStateService _gameStateService;
        private IAudioService _audioService;
        
        private int _currentRound = 0;

        [Inject]
        public void Initialize(IGameControllerService gameController, IGameStateService gameStateService, IAudioService audioService)
        {
            _gameController = gameController;
            _gameStateService = gameStateService;
            _audioService = audioService;
            
            SetupButtons();
            SubscribeToEvents();
            ResetUI();
        }

        private void SetupButtons()
        {
            AddButtonListener(_drawButton, HandleDrawButtonClick);
            AddButtonListener(_pauseButton, HandlePauseButtonClick);
            AddButtonListener(_resumeButton, HandleResumeButtonClick);
            AddButtonListener(_quitButton, HandleQuitButtonClick);
        }

        private void AddButtonListener(Button button, UnityAction onClick)
        {
            if (button == null)
            {
                Debug.LogError("[GameUIView] Button is null - cannot add listener.");
                return;
            }
            
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }

        private void SubscribeToEvents()
        {
            if (_gameController == null)
            {
                Debug.LogError("[GameUIView] Game controller is null - cannot subscribe to events.");
                return;
            }
            
            _gameController.OnRoundStarted += HandleRoundStarted;
            _gameController.OnWarStarted += HandleWarStarted;
            _gameController.OnGamePaused += HandleGamePaused;
            _gameController.OnGameResumed += HandleGameResumed;
        }

        private void HandleDrawButtonClick()
        {
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            _gameController?.DrawNextCards();
            SetDrawButtonInteractable(false);
        }

        private void HandlePauseButtonClick()
        {
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            _gameStateService?.ChangeState(GameState.Paused);
        }

        private void HandleResumeButtonClick()
        {
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            _gameStateService?.ChangeState(GameState.Playing);
        }

        private void HandleQuitButtonClick()
        {
            _audioService?.PlaySound(SoundEffect.ButtonClick);
            _gameStateService?.ChangeState(GameState.ReturnToMenu);
        }

        private void HandleRoundStarted(Game.Logic.RoundData roundData)
        {
            _currentRound++;
            UpdateRoundNumber(_currentRound);
            UpdateCardCounts(roundData.PlayerCardsRemaining, roundData.OpponentCardsRemaining);
            
            if (roundData.IsWar)
            {
                ShowWarIndicator(true, roundData.WarDepth);
            }
            else
            {
                ShowWarIndicator(false, 0);
            }
        }

        private void HandleWarStarted(int warDepth)
        {
            ShowWarIndicator(true, warDepth);
            _audioService?.PlaySound(SoundEffect.WarStart);
        }

        private void HandleGamePaused()
        {
            ShowPausePanel(true);
        }

        private void HandleGameResumed()
        {
            ShowPausePanel(false);
        }

        public void UpdateCardCounts(int playerCards, int opponentCards)
        {
            if (_playerCardCount != null)
                _playerCardCount.text = playerCards.ToString();
            
            if (_opponentCardCount != null)
                _opponentCardCount.text = opponentCards.ToString();
        }

        public void UpdateRoundNumber(int round)
        {
            if (_roundNumber != null)
                _roundNumber.text = $"Round {round}";
        }

        public void ShowWarIndicator(bool show, int depth = 0)
        {
            if (_warIndicator != null)
            {
                _warIndicator.gameObject.SetActive(show);
                if (show)
                {
                    _warIndicator.text = depth > 1 ? $"WAR x{depth}!" : "WAR!";
                }
            }
        }

        public void ShowResultText(string text, float duration = 2f)
        {
            if (_resultText != null)
            {
                _resultText.text = text;
                _resultText.gameObject.SetActive(true);
                CancelInvoke(nameof(HideResultText));
                Invoke(nameof(HideResultText), duration);
            }
        }

        private void HideResultText()
        {
            if (_resultText != null)
                _resultText.gameObject.SetActive(false);
        }

        public void ShowPausePanel(bool show)
        {
            if (_pausePanel != null)
                _pausePanel.SetActive(show);
        }

        public void SetDrawButtonInteractable(bool interactable)
        {
            if (_drawButton != null)
                _drawButton.interactable = interactable;
        }

        public void ResetUI()
        {
            _currentRound = 0;
            UpdateRoundNumber(0);
            UpdateCardCounts(26, 26);
            ShowWarIndicator(false);
            ShowPausePanel(false);
            SetDrawButtonInteractable(true);
            HideResultText();
        }

        private void OnDestroy()
        {
            if (_gameController != null)
            {
                _gameController.OnRoundStarted -= HandleRoundStarted;
                _gameController.OnWarStarted -= HandleWarStarted;
                _gameController.OnGamePaused -= HandleGamePaused;
                _gameController.OnGameResumed -= HandleGameResumed;
            }
        }
    }
}