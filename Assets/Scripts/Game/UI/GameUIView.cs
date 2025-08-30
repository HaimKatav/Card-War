using UnityEngine;
using UnityEngine.UI;
using CardWar.Services;
using CardWar.Core;
using CardWar.Common;
using CardWar.Game.Logic;

namespace CardWar.Game.UI
{
    public class GameUIView : MonoBehaviour
    {
        [Header("Game Info")]
        [SerializeField] private Text _roundText;
        [SerializeField] private Text _playerCardCountText;
        [SerializeField] private Text _opponentCardCountText;
        
        [Header("Buttons")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _drawButton;
        
        [Header("War Indicator")]
        [SerializeField] private GameObject _warIndicator;
        [SerializeField] private Text _warText;
        
        private IGameStateService _gameStateService;
        private IGameControllerService _gameControllerService;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _gameStateService = ServiceLocator.Instance.Get<IGameStateService>();
            _gameControllerService = ServiceLocator.Instance.Get<IGameControllerService>();
            
            SetupButtons();
            SubscribeToEvents();
            
            ResetDisplay();
            
            Debug.Log("[GameUIView] Initialized");
        }

        private void SetupButtons()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
                _pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }
            
            if (_drawButton != null)
            {
                _drawButton.onClick.RemoveAllListeners();
                _drawButton.onClick.AddListener(OnDrawButtonClicked);
            }
        }

        private void SubscribeToEvents()
        {
            if (_gameControllerService != null)
            {
                _gameControllerService.OnRoundStarted += HandleRoundStarted;
                _gameControllerService.OnWarStarted += HandleWarStarted;
                _gameControllerService.OnWarCompleted += HandleWarCompleted;
                _gameControllerService.OnGamePaused += HandleGamePaused;
                _gameControllerService.OnGameResumed += HandleGameResumed;
            }
            
            if (_gameStateService != null)
            {
                _gameStateService.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        #region UI Updates

        public void UpdateRoundNumber(int round)
        {
            if (_roundText != null)
            {
                _roundText.text = $"Round: {round}";
            }
        }

        public void UpdateCardCounts(int playerCount, int opponentCount)
        {
            if (_playerCardCountText != null)
            {
                _playerCardCountText.text = $"Player: {playerCount}";
            }
            
            if (_opponentCardCountText != null)
            {
                _opponentCardCountText.text = $"Opponent: {opponentCount}";
            }
        }

        public void ShowWarIndicator(bool show)
        {
            if (_warIndicator != null)
            {
                _warIndicator.SetActive(show);
            }
        }

        public void SetDrawButtonEnabled(bool enabled)
        {
            if (_drawButton != null)
            {
                _drawButton.interactable = enabled;
            }
        }

        private void ResetDisplay()
        {
            UpdateRoundNumber(0);
            UpdateCardCounts(26, 26);
            ShowWarIndicator(false);
            SetDrawButtonEnabled(false);
        }

        #endregion

        #region Event Handlers

        private void HandleGameStateChanged(GameState newState, GameState previousState)
        {
            switch (newState)
            {
                case GameState.Playing:
                    gameObject.SetActive(true);
                    SetDrawButtonEnabled(true);
                    break;
                    
                case GameState.Paused:
                    SetDrawButtonEnabled(false);
                    break;
                    
                case GameState.GameEnded:
                    SetDrawButtonEnabled(false);
                    break;
                    
                default:
                    gameObject.SetActive(false);
                    break;
            }
        }

        private void HandleRoundStarted(RoundData roundData)
        {
            if (roundData.IsWar)
            {
                ShowWarIndicator(true);
            }
        }

        private void HandleWarStarted(int warDepth)
        {
            ShowWarIndicator(true);
            
            if (_warText != null)
            {
                _warText.text = warDepth > 1 ? $"WAR x{warDepth}!" : "WAR!";
            }
        }

        private void HandleWarCompleted()
        {
            ShowWarIndicator(false);
        }

        private void HandleGamePaused()
        {
            SetDrawButtonEnabled(false);
            
            if (_pauseButton != null)
            {
                Text buttonText = _pauseButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = "Resume";
                }
            }
        }

        private void HandleGameResumed()
        {
            SetDrawButtonEnabled(true);
            
            if (_pauseButton != null)
            {
                Text buttonText = _pauseButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = "Pause";
                }
            }
        }

        #endregion

        #region Button Handlers

        private void OnPauseButtonClicked()
        {
            if (_gameStateService == null) return;
            
            if (_gameStateService.CurrentState == GameState.Playing)
            {
                _gameStateService.ChangeState(GameState.Paused);
            }
            else if (_gameStateService.CurrentState == GameState.Paused)
            {
                _gameStateService.ChangeState(GameState.Playing);
            }
        }

        private void OnDrawButtonClicked()
        {
            if (_gameControllerService != null)
            {
                _gameControllerService.DrawNextCards();
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
            }
            
            if (_drawButton != null)
            {
                _drawButton.onClick.RemoveAllListeners();
            }
            
            if (_gameControllerService != null)
            {
                _gameControllerService.OnRoundStarted -= HandleRoundStarted;
                _gameControllerService.OnWarStarted -= HandleWarStarted;
                _gameControllerService.OnWarCompleted -= HandleWarCompleted;
                _gameControllerService.OnGamePaused -= HandleGamePaused;
                _gameControllerService.OnGameResumed -= HandleGameResumed;
            }
            
            if (_gameStateService != null)
            {
                _gameStateService.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        #endregion
    }
}