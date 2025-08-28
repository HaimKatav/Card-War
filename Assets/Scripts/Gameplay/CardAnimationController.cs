using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using CardWar.Services.Assets;
using CardWar.UI.Cards;
using Zenject;

namespace CardWar.Gameplay.Controllers
{
    public class CardAnimationController : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _dealDelay = 0.1f;
        [SerializeField] private float _warAnimationDuration = 2f;
        [SerializeField] private Transform _playerCardPosition;
        [SerializeField] private Transform _opponentCardPosition;
        [SerializeField] private Transform _deckPosition;
        [SerializeField] private Transform _warPilePosition;
        
        private IAssetService _assetService;
        private CardViewController.Pool _cardPool;
        
        private readonly List<CardViewController> _activeCards = new List<CardViewController>();
        private readonly Queue<CardViewController> _cardAnimationQueue = new Queue<CardViewController>();
        private bool _isAnimating = false;
        
        [Inject]
        public void Construct(IAssetService assetService, CardViewController.Pool cardPool)
        {
            _assetService = assetService;
            _cardPool = cardPool;
        }
        
        private async void Start()
        {
            if (!_assetService.AreAssetsLoaded)
            {
                await _assetService.PreloadCardAssets();
            }
        }
        
        public CardViewController CreateCard(CardData cardData, Transform parent = null)
        {
            if (cardData == null) return null;
            
            var cardView = _cardPool.Spawn();
            if (parent != null)
            {
                cardView.transform.SetParent(parent, false);
            }
            
            cardView.Setup(cardData);
            
            var frontSprite = _assetService.GetCardSprite(cardData);
            var backSprite = _assetService.GetCardBackSprite();
            cardView.SetCardSprites(frontSprite, backSprite);
            
            _activeCards.Add(cardView);
            return cardView;
        }
        
        public List<CardViewController> CreateCards(List<CardData> cardDataList, Transform parent = null)
        {
            var cards = new List<CardViewController>();
            
            if (cardDataList == null || cardDataList.Count == 0)
                return cards;
            
            foreach (var cardData in cardDataList)
            {
                var card = CreateCard(cardData, parent);
                if (card != null)
                {
                    cards.Add(card);
                }
            }
            
            return cards;
        }
        
        public void ReturnCard(CardViewController card)
        {
            if (card == null) return;
            
            _activeCards.Remove(card);
            _cardPool.Despawn(card);
        }
        
        public void ReturnAllCards()
        {
            foreach (var card in _activeCards)
            {
                if (card != null)
                {
                    _cardPool.Despawn(card);
                }
            }
            
            _activeCards.Clear();
        }
        
        public async UniTask DealCardAsync(CardData cardData, Transform targetParent, Vector3 targetPosition, bool faceUp = false)
        {
            var card = CreateCard(cardData, targetParent);
            if (card == null) return;
            
            card.transform.position = _deckPosition.position;
            card.SetFaceDown(instant: true);
            
            await card.MoveToPositionAsync(targetPosition);
            
            if (faceUp)
            {
                await card.FlipToFrontAsync();
            }
        }
        
        public async UniTask AnimateWarSequenceAsync(CardData playerCard, CardData opponentCard, bool playerWins)
        {
            if (_isAnimating) return;
            
            _isAnimating = true;
            
            try
            {
                var playerCardView = CreateCard(playerCard, _playerCardPosition);
                var opponentCardView = CreateCard(opponentCard, _opponentCardPosition);
                
                if (playerCardView == null || opponentCardView == null) return;
                
                playerCardView.transform.position = _warPilePosition.position;
                opponentCardView.transform.position = _warPilePosition.position;
                
                playerCardView.SetFaceDown(instant: true);
                opponentCardView.SetFaceDown(instant: true);
                
                var playerMoveTask = playerCardView.MoveToPositionAsync(_playerCardPosition.position);
                var opponentMoveTask = opponentCardView.MoveToPositionAsync(_opponentCardPosition.position);
                
                await UniTask.WhenAll(playerMoveTask, opponentMoveTask);
                await UniTask.Delay(500);
                
                var playerFlipTask = playerCardView.FlipToFrontAsync();
                var opponentFlipTask = opponentCardView.FlipToFrontAsync();
                
                await UniTask.WhenAll(playerFlipTask, opponentFlipTask);
                
                if (playerWins)
                {
                    await playerCardView.ScalePunchAsync(1.3f, 0.5f);
                }
                else
                {
                    await opponentCardView.ScalePunchAsync(1.3f, 0.5f);
                }
                
                await UniTask.Delay(1000);
            }
            finally
            {
                _isAnimating = false;
            }
        }
        
        public async UniTask DealHandAsync(List<CardData> playerCards, List<CardData> opponentCards)
        {
            var dealTasks = new List<UniTask>();
            
            for (int i = 0; i < playerCards.Count; i++)
            {
                var card = playerCards[i];
                var delayMs = Mathf.RoundToInt(i * _dealDelay * 1000f);
                
                dealTasks.Add(UniTask.Create(async () =>
                {
                    await UniTask.Delay(delayMs);
                    await DealCardAsync(card, _playerCardPosition, 
                        _playerCardPosition.position + Vector3.right * i * 0.1f, false);
                }));
            }
            
            for (int i = 0; i < opponentCards.Count; i++)
            {
                var card = opponentCards[i];
                var delayMs = Mathf.RoundToInt(i * _dealDelay * 1000f);
                
                dealTasks.Add(UniTask.Create(async () =>
                {
                    await UniTask.Delay(delayMs);
                    await DealCardAsync(card, _opponentCardPosition,
                        _opponentCardPosition.position + Vector3.right * i * 0.1f, false);
                }));
            }
            
            await UniTask.WhenAll(dealTasks);
        }
        
        public void RefreshAllCardSprites()
        {
            foreach (var card in _activeCards)
            {
                if (card == null || card.GetCardData() == null) continue;
                
                var frontSprite = _assetService.GetCardSprite(card.GetCardData());
                var backSprite = _assetService.GetCardBackSprite();
                
                card.SetCardSprites(frontSprite, backSprite);
            }
        }
        
        public async UniTask PreloadCardsAsync(List<CardData> upcomingCards)
        {
            if (upcomingCards == null || upcomingCards.Count == 0) return;
            
            foreach (var cardData in upcomingCards)
            {
                _assetService.GetCardSprite(cardData);
            }
            
            await UniTask.Delay(10);
        }
        
        public void OnReturnToMenu()
        {
            ReturnAllCards();
            _cardAnimationQueue.Clear();
            DOTween.KillAll();
            _isAnimating = false;
        }
        
        public int GetActiveCardCount() => _activeCards.Count;
        public bool IsAnimating() => _isAnimating;
        
        private void OnDestroy()
        {
            DOTween.KillAll();
            ReturnAllCards();
        }
    }
}