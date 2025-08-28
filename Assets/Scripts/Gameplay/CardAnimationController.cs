using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using CardWar.Core.Data;
using CardWar.Services.Assets;
using CardWar.UI.Cards;
using Zenject;

namespace CardWar.Gameplay.Controllers
{
    public class CardAnimationController : MonoBehaviour, IInitializable, IDisposable
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
        
        public void Initialize()
        {
            PreloadAssetsAsync().Forget();
        }
        
        private async UniTaskVoid PreloadAssetsAsync()
        {
            if (_assetService != null && !_assetService.AreAssetsLoaded)
            {
                await _assetService.PreloadCardAssets();
            }
        }
        
        public CardViewController CreateCard(CardData cardData, Transform parent = null)
        {
            if (cardData == null || _cardPool == null) return null;
            
            var cardView = _cardPool.Spawn();
            if (parent != null)
            {
                cardView.transform.SetParent(parent, false);
            }
            
            cardView.Setup(cardData);
            
            if (_assetService != null)
            {
                var frontSprite = _assetService.GetCardSprite(cardData);
                var backSprite = _assetService.GetCardBackSprite();
                cardView.SetCardSprites(frontSprite, backSprite);
            }
            
            _activeCards.Add(cardView);
            return cardView;
        }
        
        public void ReturnCard(CardViewController cardView)
        {
            if (cardView == null) return;
            
            _activeCards.Remove(cardView);
            cardView.transform.SetParent(_deckPosition, false);
            _cardPool?.Despawn(cardView);
        }
        
        public async UniTask DealCardsToPositions(CardData playerCard, CardData opponentCard)
        {
            var playerCardView = CreateCard(playerCard, _deckPosition);
            var opponentCardView = CreateCard(opponentCard, _deckPosition);
            
            if (playerCardView != null && _playerCardPosition != null)
            {
                await AnimateCardToPosition(playerCardView, _playerCardPosition, true);
            }
            
            if (opponentCardView != null && _opponentCardPosition != null)
            {
                await AnimateCardToPosition(opponentCardView, _opponentCardPosition, true);
            }
        }
        
        public async UniTask AnimateCardToPosition(CardViewController cardView, Transform targetPosition, bool faceUp)
        {
            if (cardView == null || targetPosition == null) return;
            
            _isAnimating = true;
            
            var sequence = DOTween.Sequence();
            sequence.Append(cardView.transform.DOMove(targetPosition.position, 0.5f).SetEase(Ease.OutCubic));
            sequence.Append(cardView.transform.DORotate(targetPosition.rotation.eulerAngles, 0.2f));
            
            if (faceUp)
            {
                sequence.AppendCallback(() => cardView.SetFaceUp());
            }
            
            await sequence.AsyncWaitForCompletion();
            _isAnimating = false;
        }
        
        public async UniTask AnimateWarCards(List<CardData> playerWarCards, List<CardData> opponentWarCards)
        {
            if (playerWarCards == null || opponentWarCards == null) return;
            
            _isAnimating = true;
            
            var sequence = DOTween.Sequence();
            
            for (int i = 0; i < playerWarCards.Count; i++)
            {
                var playerCard = CreateCard(playerWarCards[i], _deckPosition);
                var opponentCard = CreateCard(opponentWarCards[i], _deckPosition);
                
                if (playerCard != null && _warPilePosition != null)
                {
                    sequence.Append(playerCard.transform.DOMove(_warPilePosition.position + Vector3.left * 0.5f, 0.3f));
                }
                
                if (opponentCard != null && _warPilePosition != null)
                {
                    sequence.Join(opponentCard.transform.DOMove(_warPilePosition.position + Vector3.right * 0.5f, 0.3f));
                }
                
                sequence.AppendInterval(_dealDelay);
            }
            
            await sequence.AsyncWaitForCompletion();
            _isAnimating = false;
        }
        
        public void ClearAllCards()
        {
            foreach (var card in _activeCards.ToArray())
            {
                ReturnCard(card);
            }
            _activeCards.Clear();
        }
        
        public void Dispose()
        {
            Debug.Log("[CardAnimationController] Disposing");
            
            DOTween.Kill(this);
            ClearAllCards();
            _cardAnimationQueue.Clear();
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
    }
}