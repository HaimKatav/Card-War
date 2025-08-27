using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using CardWar.Core.Events;
using CardWar.Core.GameLogic;
using CardWar.Gameplay.Cards;
using CardWar.Services.UI;

namespace CardWar.UI.Screens
{
    public class GameUIController : MonoBehaviour, IGameUIController
    {
        [Header("Gameplay UI")]
        [SerializeField] private GameObject _gameplayScreen;
        [SerializeField] private RectTransform _gameplayScreenRect;
        [SerializeField] private Transform _playerCardSlot;
        [SerializeField] private Transform _opponentCardSlot;
        [SerializeField] private TextMeshProUGUI _playerCardCountText;
        [SerializeField] private TextMeshProUGUI _opponentCardCountText;
        [SerializeField] private TextMeshProUGUI _roundResultText;
        [SerializeField] private Button _playCardButton;
        
        [Header("Game End UI")]
        [SerializeField] private GameObject _gameEndPanel;
        [SerializeField] private TextMeshProUGUI _gameEndText;
        [SerializeField] private Button _newGameButton;

        private SignalBus _signalBus;
        private IGameService _gameService;
        private CardView _currentPlayerCard;
        private CardView _currentOpponentCard;

        [Inject]
        public void Construct(SignalBus signalBus, IGameService gameService)
        {
            _signalBus = signalBus;
            _gameService = gameService;
            
            SetupButtons();
            ResetGameplayUI();
        }

        public void Initialize(SignalBus signalBus)
        {
            _signalBus = signalBus;
            
            SetupButtons();
            ResetGameplayUI();
        }

        private void SetupButtons()
        {
            if (_playCardButton != null)
            {
                _playCardButton.onClick.AddListener(OnPlayCardButtonClicked);
            }

            if (_newGameButton != null)
            {
                _newGameButton.onClick.AddListener(() => _signalBus.Fire<StartGameEvent>());
            }
        }

        private void OnPlayCardButtonClicked()
        {
            if (_gameService.IsGameActive)
            {
                _gameService.PlayRound().Forget(); // Fire and forget for async operation
            }
        }

        public RectTransform GetRectTransform()
        {
            return _gameplayScreenRect;
        }

        public void Show()
        {
            Debug.Log("GameUIController: Showing gameplay screen");
            
            transform.ResetTransform();
            _gameplayScreen.transform.ResetTransform();
            _gameEndPanel.transform.ResetTransform();
            
            SetScreenActive(true);
            ResetGameplayUI();
        }

        public void Hide()
        {
            SetScreenActive(false);
        }

        public void UpdatePlayerCard(CardData card)
        {
            if (_currentPlayerCard != null)
            {
                DestroyCardView(_currentPlayerCard);
            }
            
            _currentPlayerCard = CreateCardView(card, _playerCardSlot);
            Debug.Log($"GameUI: Updated player card to {card}");
        }

        public void UpdateOpponentCard(CardData card)
        {
            if (_currentOpponentCard != null)
            {
                DestroyCardView(_currentOpponentCard);
            }
            
            _currentOpponentCard = CreateCardView(card, _opponentCardSlot);
            Debug.Log($"GameUI: Updated opponent card to {card}");
        }

        public void ShowRoundResult(GameRoundResultData resultData)
        {
            if (_roundResultText == null) return;

            string resultText = resultData.Result switch
            {
                GameResult.PlayerWins => $"You win this round! (+{resultData.CardsWon} cards)",
                GameResult.OpponentWins => $"Opponent wins this round! (-{resultData.CardsWon} cards)",
                GameResult.War => "WAR! Cards are tied!",
                _ => "Round completed"
            };

            _roundResultText.text = resultText;
            Debug.Log($"GameUI: Showing round result - {resultText}");
        }

        public void ShowGameResult(GameEndResultData resultData)
        {
            if (_gameEndPanel != null)
                _gameEndPanel.SetActive(true);
            
            if (_gameEndText != null)
            {
                string winnerText = resultData.PlayerWon ? "YOU WIN!" : "OPPONENT WINS!";
                _gameEndText.text = $"{winnerText}\nRounds played: {resultData.TotalRounds}";
            }
            
            Debug.Log($"GameUI: Game ended - {resultData.WinnerName} won after {resultData.TotalRounds} rounds");
        }

        public void SetInteractable(bool interactable)
        {
            if (_playCardButton != null)
                _playCardButton.interactable = interactable;
        }
        
        public Transform GetPlayerArea()
        {
            return _playerCardSlot;
        }
        
        public Transform GetOpponentArea()
        {
            return _opponentCardSlot;
        }
        
        public void SetDrawButtonInteractable(bool interactable)
        {
            if (_playCardButton != null)
                _playCardButton.interactable = interactable;
        }

        private void ResetGameplayUI()
        {
            if (_roundResultText != null)
                _roundResultText.text = "Tap to draw cards!";
            
            if (_gameEndPanel != null)
                _gameEndPanel.SetActive(false);
        }

        private CardView CreateCardView(CardData cardData, Transform parent)
        {
            // For now, create a simple card representation
            // This would be replaced with proper card factory in full implementation
            var cardObject = new GameObject($"Card_{cardData}");
            cardObject.transform.SetParent(parent, false);
            
            var cardView = cardObject.AddComponent<CardView>();
            cardView.Setup(cardData);
            
            return cardView;
        }

        private void DestroyCardView(CardView cardView)
        {
            if (cardView != null && cardView.gameObject != null)
            {
                Destroy(cardView.gameObject);
            }
        }

        private void SetScreenActive(bool active)
        {
            _gameplayScreen.SetActive(active);
        }

        private void OnDestroy()
        {
            if (_playCardButton != null)
                _playCardButton.onClick.RemoveAllListeners();
            
            if (_newGameButton != null)
                _newGameButton.onClick.RemoveAllListeners();
        }
    }
}
