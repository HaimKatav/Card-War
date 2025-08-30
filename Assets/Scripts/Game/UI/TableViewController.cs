using System;
using System.Collections.Generic;
using CardWar.Common;
using UnityEngine;
using UnityEngine.UI;
using CardWar.Game.Logic;
using CardWar.Services;
using CardWar.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace CardWar.Game.UI
{
    public class TableViewController : MonoBehaviour
    {
        [Header("Deck Positions")]
        [SerializeField] private Transform _playerDeckPosition;
        [SerializeField] private Transform _opponentDeckPosition;
        
        [Header("Battle Positions")]
        [SerializeField] private Transform _playerBattlePosition;
        [SerializeField] private Transform _opponentBattlePosition;
        
        [Header("War Positions")]
        [SerializeField] private Transform[] _playerWarPositions;
        [SerializeField] private Transform[] _opponentWarPositions;
        
        [Header("Card Prefab")]
        [SerializeField] private GameObject _cardPrefab;
        
        [Header("UI References")]
        [SerializeField] private GameUIView _gameUIView;
        [SerializeField] private Button _drawButton;
        
        [Header("Animation Settings")]
        [SerializeField] private float _cardMoveSpeed = 0.5f;
        [SerializeField] private float _cardFlipDelay = 0.3f;
        [SerializeField] private float _roundEndDelay = 1f;
        
        private IGameControllerService _gameController;
        private IAssetService _assetService;
        private GameSettings _gameSettings;
        
        private CardPool _cardPool;
        private CardView _playerBattleCard;
        private CardView _opponentBattleCard;
        private List<CardView> _warCards;
        private bool _isAnimating;

        #region Initialization

        public void Initialize(IGameControllerService gameController)
        {
            _gameController = gameController;
            _assetService = ServiceLocator.Instance.Get<IAssetService>();
            _gameSettings = ServiceLocator.Instance.Get<GameSettings>();
            
            SetupCardPool();
            SetupUI();
            
            _warCards = new List<CardView>();
            
            Debug.Log("[TableViewController] Initialized");
        }

        private void SetupCardPool()
        {
            _cardPool = gameObject.GetComponent<CardPool>();
            if (_cardPool == null)
            {
                _cardPool = gameObject.AddComponent<CardPool>();
            }
            
            if (_cardPrefab != null)
            {
                _cardPool.Initialize(_cardPrefab, 20);
            }
            else
            {
                Debug.LogWarning("[TableViewController] Card prefab not assigned, creating default");
                CreateDefaultCardPrefab();
            }
        }

        private void CreateDefaultCardPrefab()
        {
            _cardPrefab = new GameObject("Card");
            _cardPrefab.AddComponent<RectTransform>();
            _cardPrefab.AddComponent<Image>();
            _cardPrefab.AddComponent<CardView>();
            _cardPrefab.SetActive(false);
            
            _cardPool.Initialize(_cardPrefab, 20);
        }

        private void SetupUI()
        {
            if (_gameUIView == null)
            {
                _gameUIView = FindObjectOfType<GameUIView>();
            }
            
            if (_drawButton != null)
            {
                _drawButton.onClick.RemoveAllListeners();
                _drawButton.onClick.AddListener(OnDrawButtonClicked);
            }
        }

        public void SetupInitialState(int playerCards, int opponentCards)
        {
            UpdateCardCounts(playerCards, opponentCards);
            UpdateRoundNumber(0);
            SetDrawButtonEnabled(true);
        }

        #endregion

        #region Game Flow

        public async UniTask PlayRound(RoundData roundData)
        {
            _isAnimating = true;
            SetDrawButtonEnabled(false);
            
            ClearWarCards();
            
            _playerBattleCard = _cardPool.GetCard();
            _opponentBattleCard = _cardPool.GetCard();
            
            SetupCard(_playerBattleCard, roundData.PlayerCard, _playerDeckPosition.position);
            SetupCard(_opponentBattleCard, roundData.OpponentCard, _opponentDeckPosition.position);
            
            await AnimateCardsToCenter();
            await FlipCards();
            
            if (!roundData.IsWar)
            {
                await UniTask.Delay((int)(_roundEndDelay * 1000));
                await CollectCards(roundData.Result);
            }
            
            _isAnimating = false;
            
            if (!roundData.IsWar)
            {
                SetDrawButtonEnabled(true);
            }
        }

        public async UniTask PlayWarSequence(RoundData warRound)
        {
            _isAnimating = true;
            SetDrawButtonEnabled(false);
            
            await DisplayWarCards(warRound);
            await UniTask.Delay((int)(_roundEndDelay * 1000));
            
            if (!warRound.HasChainedWar)
            {
                await CollectWarCards(warRound.Result);
                ClearWarCards();
                SetDrawButtonEnabled(true);
            }
            
            _isAnimating = false;
        }

        private async UniTask DisplayWarCards(RoundData warRound)
        {
            ClearWarCards();
            
            for (int i = 0; i < warRound.PlayerWarCards.Count && i < 4; i++)
            {
                var card = _cardPool.GetCard();
                _warCards.Add(card);
                
                var position = i < _playerWarPositions.Length ? _playerWarPositions[i] : _playerBattlePosition;
                SetupCard(card, warRound.PlayerWarCards[i], position.position);
                
                if (i == warRound.PlayerWarCards.Count - 1)
                {
                    card.FlipCard(true);
                }
            }
            
            for (int i = 0; i < warRound.OpponentWarCards.Count && i < 4; i++)
            {
                var card = _cardPool.GetCard();
                _warCards.Add(card);
                
                var position = i < _opponentWarPositions.Length ? _opponentWarPositions[i] : _opponentBattlePosition;
                SetupCard(card, warRound.OpponentWarCards[i], position.position);
                
                if (i == warRound.OpponentWarCards.Count - 1)
                {
                    card.FlipCard(true);
                }
            }
            
            await UniTask.Delay(500);
        }

        #endregion

        #region Card Animation

        private void SetupCard(CardView card, CardData cardData, Vector3 position)
        {
            card.transform.position = position;
            card.SetCardData(cardData);
            card.FlipCard(false);
            
            if (_assetService != null && cardData != null)
            {
                var sprite = _assetService.GetCardSprite(cardData.GetCardKey());
                if (sprite != null)
                {
                    card.SetCardSprite(sprite);
                }
            }
        }

        private async UniTask AnimateCardsToCenter()
        {
            var playerMove = _playerBattleCard.transform.DOMove(_playerBattlePosition.position, _cardMoveSpeed);
            var opponentMove = _opponentBattleCard.transform.DOMove(_opponentBattlePosition.position, _cardMoveSpeed);
            
            await UniTask.WhenAll(
                playerMove.AsyncWaitForCompletion().AsUniTask(),
                opponentMove.AsyncWaitForCompletion().AsUniTask()
            );
        }

        private async UniTask FlipCards()
        {
            await UniTask.Delay((int)(_cardFlipDelay * 1000));
            
            _playerBattleCard.FlipCard(true);
            _opponentBattleCard.FlipCard(true);
            
            await UniTask.Delay(300);
        }

        private async UniTask CollectCards(RoundResult result)
        {
            Vector3 targetPosition = result == RoundResult.PlayerWins ? 
                _playerDeckPosition.position : _opponentDeckPosition.position;
            
            var playerMove = _playerBattleCard.transform.DOMove(targetPosition, _cardMoveSpeed);
            var opponentMove = _opponentBattleCard.transform.DOMove(targetPosition, _cardMoveSpeed);
            
            await UniTask.WhenAll(
                playerMove.AsyncWaitForCompletion().AsUniTask(),
                opponentMove.AsyncWaitForCompletion().AsUniTask()
            );
            
            _cardPool.ReturnCard(_playerBattleCard);
            _cardPool.ReturnCard(_opponentBattleCard);
            
            _playerBattleCard = null;
            _opponentBattleCard = null;
        }

        private async UniTask CollectWarCards(RoundResult result)
        {
            Vector3 targetPosition = result == RoundResult.PlayerWins ? 
                _playerDeckPosition.position : _opponentDeckPosition.position;
            
            List<UniTask> tasks = new List<UniTask>();
            
            if (_playerBattleCard != null)
            {
                tasks.Add(_playerBattleCard.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
            }
            
            if (_opponentBattleCard != null)
            {
                tasks.Add(_opponentBattleCard.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
            }
            
            foreach (var card in _warCards)
            {
                tasks.Add(card.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
            }
            
            await UniTask.WhenAll(tasks);
        }

        private void ClearWarCards()
        {
            foreach (var card in _warCards)
            {
                _cardPool.ReturnCard(card);
            }
            _warCards.Clear();
        }

        #endregion

        #region UI Updates

        public void UpdateCardCounts(int playerCount, int opponentCount)
        {
            if (_gameUIView != null)
            {
                _gameUIView.UpdateCardCounts(playerCount, opponentCount);
            }
        }

        public void UpdateRoundNumber(int round)
        {
            if (_gameUIView != null)
            {
                _gameUIView.UpdateRoundNumber(round);
            }
        }

        private void SetDrawButtonEnabled(bool enabled)
        {
            if (_drawButton != null)
            {
                _drawButton.interactable = enabled && !_isAnimating;
            }
        }

        private void OnDrawButtonClicked()
        {
            if (!_isAnimating && _gameController != null)
            {
                _gameController.DrawNextCards();
            }
        }

        #endregion

        #region Pause Handling

        public void HandlePause()
        {
            DOTween.PauseAll();
            SetDrawButtonEnabled(false);
        }

        public void HandleResume()
        {
            DOTween.PlayAll();
            
            if (!_isAnimating)
            {
                SetDrawButtonEnabled(true);
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            DOTween.KillAll();
            
            if (_drawButton != null)
            {
                _drawButton.onClick.RemoveAllListeners();
            }
        }

        #endregion
    }
}