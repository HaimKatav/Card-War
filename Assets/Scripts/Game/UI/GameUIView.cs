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
        [SerializeField] private TMP_Text _roundText;
        [SerializeField] private TMP_Text _playerCardCountText;
        [SerializeField] private TMP_Text _opponentCardCountText;
        
        [Header("Buttons")]
        [SerializeField] private Button _pauseButton;
        
        [Header("War Indicator")]
        [SerializeField] private GameObject _warIndicator;
        [SerializeField] private TMP_Text _warText;
        
        private IGameControllerService _gameControllerService;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
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
        }

        private void SubscribeToEvents()
        {
            if (_gameControllerService != null)
            {
                _gameControllerService.OnRoundStarted += HandleRoundStarted;
                _gameControllerService.OnWarStarted += HandleWarStarted;
                _gameControllerService.OnWarCompleted += HandleWarCompleted;
            }
        }

        #region UI Updates

        private void OnPauseButtonClicked() => _gameControllerService?.PauseGame();


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

        #endregion

        #region Event Handlers

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
        

        #endregion
        
        
        #region Cleanup

        private void OnDestroy()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
            }
            
            if (_gameControllerService != null)
            {
                _gameControllerService.OnRoundStarted -= HandleRoundStarted;
                _gameControllerService.OnWarStarted -= HandleWarStarted;
                _gameControllerService.OnWarCompleted -= HandleWarCompleted;
            }
        }

        #endregion
    }
}