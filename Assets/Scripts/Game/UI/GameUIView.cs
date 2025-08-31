using System;
using UnityEngine;
using UnityEngine.UI;
using CardWar.Services;
using CardWar.Core;
using CardWar.Common;
using CardWar.Game.Logic;
using TMPro;

namespace CardWar.Game.UI
{
    public class GameUIView : MonoBehaviour
    {
        [Header("Game Info")]
        [SerializeField] private GameObject _pauseMenuPanel;
        
        [Header("Game Info")]
        [SerializeField] private TMP_Text _roundText;
        [SerializeField] private TMP_Text _playerCardCountText;
        [SerializeField] private TMP_Text _opponentCardCountText;
        
        [Header("Buttons")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _backToMainMenuButton;
        
        [Header("War Indicator")]
        [SerializeField] private GameObject _warIndicator;
        [SerializeField] private TMP_Text _warText;
        
        public event Action OnPauseButtonPressed;
        public event Action OnResumeButtonPressed;
        public event Action OnBackToMainMenuButtonPressed;
        
        private IGameControllerService _gameControllerService;
        private IGameStateService _gameStateService;
        
        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _gameControllerService = ServiceLocator.Instance.Get<IGameControllerService>();
            _gameStateService = ServiceLocator.Instance.Get<IGameStateService>();
            
            SetupButtons();
            RegisterEvents();
            
            ResetDisplay();
            
            Debug.Log("[GameUIView] Initialized");
        }

        private void RegisterEvents()
        {
            _gameStateService.GameStateChanged += HandleStateChange;
        }

        private void HandleStateChange(GameState obj)
        {
            switch (obj)
            {
                case GameState.Playing:
                    RegisterGameControllerEvents();
                    break;
                case GameState.MainMenu:
                    UnregisterBoardEvents();
                    break;
            }
        }
        
        private void RegisterGameControllerEvents()
        {
            _gameControllerService.RoundStartedEvent += HandleRoundStarted;
            _gameControllerService.WarStartedEvent += HandleWarStarted;
            _gameControllerService.WarCompletedEvent += HandleWarCompleted;
        }

        private void SetupButtons()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
                _pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }  
            
            if (_backToMainMenuButton != null)
            {
                _backToMainMenuButton.onClick.RemoveAllListeners();
                _backToMainMenuButton.onClick.AddListener(OnBackToMainMenuClicked);
            }
            
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveAllListeners();
                _resumeButton.onClick.AddListener(OnResumeButtonClicked);
            }
        }

        #region Public Methods

        public void TogglePauseMenu(bool show)
        {
            _pauseMenuPanel.SetActive(show);
        }

        #endregion Public Methods
        
        
        #region UI Updates

        private void OnPauseButtonClicked()
        {
            OnPauseButtonPressed?.Invoke();  
        } 
        
        private void OnResumeButtonClicked()
        {
            OnResumeButtonPressed?.Invoke();
        }

        private void OnBackToMainMenuClicked()
        {
            OnBackToMainMenuButtonPressed?.Invoke();
        }


        private void UpdateRoundNumber(int round)
        {
            _roundText.text = $"Round: {round}";
        }

        private void UpdateCardCounts(int playerCount, int opponentCount)
        {
            _playerCardCountText.text = $"Player: {playerCount}";
            _opponentCardCountText.text = $"Opponent: {opponentCount}";
        }

        private void ShowWarIndicator(bool show)
        {
            _warIndicator.SetActive(show);
        }

        private void ResetDisplay()
        {
            UpdateRoundNumber(0);
            UpdateCardCounts(26, 26);
            ShowWarIndicator(false);
        }

        #endregion UI Updates

        
        #region Event Handlers

        private void HandleRoundStarted(RoundData roundData)
        {
            UpdateCardCounts(roundData.PlayerCardsRemaining, roundData.OpponentCardsRemaining);
            UpdateRoundNumber(roundData.RoundNumber);
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

        #endregion Event Handlers
        
        
        #region Cleanup
        
        private void UnregisterBoardEvents()
        {
            if (_gameControllerService != null)
            {
                _gameControllerService.RoundStartedEvent += HandleRoundStarted;
                _gameControllerService.WarStartedEvent += HandleWarStarted;
                _gameControllerService.WarCompletedEvent += HandleWarCompleted;   
            }
        }

        private void OnDestroy()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
            }

            _pauseButton = null;
            
            UnregisterBoardEvents();
        }

        #endregion Cleanup
    }
}