using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using CardWar.Services.Assets;
using CardWar.Services.Game;
using CardWar.UI.Cards;
using CardWar.Infrastructure.Events;
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
        private IGameService _gameService;
        private SignalBus _signalBus;
        private CardViewController.Pool _cardPool;
        
        private readonly List<CardViewController> _activeCards = new List<CardViewController>();
        private readonly Queue<CardViewController> _cardAnimationQueue = new Queue<CardViewController>();
        private bool _isAnimating = false;
        
        [Inject]
        public void Construct(IAssetService assetService, IGameService gameService, SignalBus signalBus, CardViewController.Pool cardPool)
        {
            _assetService = assetService;
            _gameService = gameService;
            _signalBus = signalBus;
            _cardPool = cardPool;
        }
        
        public void Initialize()
        {
            Debug.Log("[CardAnimationController] Initializing and subscribing to events");
            SubscribeToEvents();
            PreloadAssetsAsync().Forget();
        }
        
        private void SubscribeToEvents()
        {
            if (_signalBus != null)
            {
                _signalBus.Subscribe<RoundStartEvent>(OnRoundStart);
                _signalBus.Subscribe<RoundCompleteEvent>(OnRoundComplete);
                _signalBus.Subscribe<WarStartEvent>(OnWarStart);
                _signalBus.Subscribe<GameStartEvent>(OnGameStart);
                _signalBus.Subscribe<GameEndEvent>(OnGameEnd);
                Debug.Log("[CardAnimationController] Subscribed to game events");
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_signalBus != null)
            {
                _signalBus.TryUnsubscribe<RoundStartEvent>(OnRoundStart);
                _signalBus.TryUnsubscribe<RoundCompleteEvent>(OnRoundComplete);
                _signalBus.TryUnsubscribe<WarStartEvent>(OnWarStart);
                _signalBus.TryUnsubscribe<GameStartEvent>(OnGameStart);
                _signalBus.TryUnsubscribe<GameEndEvent>(OnGameEnd);
                Debug.Log("[CardAnimationController] Unsubscribed from game events");
            }
        }
        
        private void OnRoundStart(RoundStartEvent eventData)
        {
            Debug.Log("[CardAnimationController] Round started - preparing to deal cards");
            DealRoundCardsAsync().Forget();
        }
        
        private void OnRoundComplete(RoundCompleteEvent eventData)
        {
            Debug.Log($"[CardAnimationController] Round complete - Player: {eventData.PlayerScore}, Opponent: {eventData.OpponentScore}");
            HandleRoundComplete(eventData).Forget();
        }
        
        private void OnWarStart(WarStartEvent eventData)
        {
            Debug.Log("[CardAnimationController] War started!");
            HandleWarAnimation(eventData).Forget();
        }
        
        private void OnGameStart(GameStartEvent eventData)
        {
            Debug.Log("[CardAnimationController] Game started - clearing all cards");
            ClearAllCards();
        }
        
        private void OnGameEnd(GameEndEvent eventData)
        {
            Debug.Log("[CardAnimationController] Game ended - cleaning up");
            ClearAllCards();
        }
        
        private async UniTaskVoid DealRoundCardsAsync()
        {
            if (_gameService?.GameStateData?.LastRound != null)
            {
                var roundData = _gameService.GameStateData.LastRound;
                
                Debug.Log($"[CardAnimationController] Dealing cards - Player: {roundData.PlayerCard?.ToString()}, Opponent: {roundData.OpponentCard?.ToString()}");
                
                await DealCardsToPositions(roundData.PlayerCard, roundData.OpponentCard);
                
                await UniTask.Delay(1000);
                
                ShowRoundResult(roundData.Result == GameResult.PlayerWin);
            }
        }
        
        private async UniTaskVoid HandleRoundComplete(RoundCompleteEvent eventData)
        {
            await UniTask.Delay(2000);
            ClearActiveCards();
        }
        
        private async UniTaskVoid HandleWarAnimation(WarStartEvent eventData)
        {
            if (eventData.WarData != null)
            {
                await AnimateWarSequence(eventData.WarData);
            }
        }
        
        private void ClearAllCards()
        {
            Debug.Log($"[CardAnimationController] Clearing {_activeCards.Count} active cards");
            
            var cardsToReturn = new List<CardViewController>(_activeCards);
            foreach (var card in cardsToReturn)
            {
                ReturnCard(card);
            }
            _activeCards.Clear();
        }
        
        private void ClearActiveCards()
        {
            foreach (var card in _activeCards)
            {
                if (card != null)
                {
                    card.transform.DOMove(_deckPosition.position, 0.5f).OnComplete(() =>
                    {
                        ReturnCard(card);
                    });
                }
            }
        }
        
        private void ShowRoundResult(bool playerWins)
        {
            Debug.Log($"[CardAnimationController] Round result - Player wins: {playerWins}");
            
            var winnerCard = playerWins ? GetPlayerCard() : GetOpponentCard();
            if (winnerCard != null)
            {
                winnerCard.transform.DOScale(1.2f, 0.3f).SetLoops(2, LoopType.Yoyo);
            }
        }
        
        private CardViewController GetPlayerCard()
        {
            return _activeCards.Find(c => Vector3.Distance(c.transform.position, _playerCardPosition.position) < 0.1f);
        }
        
        private CardViewController GetOpponentCard()
        {
            return _activeCards.Find(c => Vector3.Distance(c.transform.position, _opponentCardPosition.position) < 0.1f);
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
            Debug.Log($"[CardAnimationController] Created card: {cardData} (Total active: {_activeCards.Count})");
            return cardView;
        }
        
        public void ReturnCard(CardViewController cardView)
        {
            if (cardView == null) return;
            
            _activeCards.Remove(cardView);
            cardView.transform.SetParent(_deckPosition, false);
            _cardPool?.Despawn(cardView);
            Debug.Log($"[CardAnimationController] Returned card (Total active: {_activeCards.Count})");
        }
        
        public async UniTask DealCardsToPositions(CardData playerCard, CardData opponentCard)
        {
            Debug.Log("[CardAnimationController] Starting card deal animation");
            
            var playerCardView = CreateCard(playerCard, _deckPosition);
            var opponentCardView = CreateCard(opponentCard, _deckPosition);
            
            if (playerCardView != null && _playerCardPosition != null)
            {
                await AnimateCardToPosition(playerCardView, _playerCardPosition, true);
            }
            
            await UniTask.Delay((int)(_dealDelay * 1000));
            
            if (opponentCardView != null && _opponentCardPosition != null)
            {
                await AnimateCardToPosition(opponentCardView, _opponentCardPosition, true);
            }
            
            Debug.Log("[CardAnimationController] Card deal animation complete");
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
        
        private async UniTask AnimateWarSequence(WarData warData)
        {
            Debug.Log("[CardAnimationController] Starting war sequence animation");
            
            var playerCards = new List<CardViewController>();
            var opponentCards = new List<CardViewController>();
            
            for (int i = 0; i < warData.CardsPerSide; i++)
            {
                await UniTask.Delay(200);
                
                var playerWarCard = CreateCard(warData.PlayerWarCards[i], _deckPosition);
                var opponentWarCard = CreateCard(warData.OpponentWarCards[i], _deckPosition);
                
                if (playerWarCard != null)
                {
                    await AnimateCardToPosition(playerWarCard, _warPilePosition, false);
                    playerCards.Add(playerWarCard);
                }
                
                if (opponentWarCard != null)
                {
                    await AnimateCardToPosition(opponentWarCard, _warPilePosition, false);
                    opponentCards.Add(opponentWarCard);
                }
            }
            
            await UniTask.Delay(500);
            
            var finalPlayerCard = CreateCard(warData.FinalPlayerCard, _warPilePosition);
            var finalOpponentCard = CreateCard(warData.FinalOpponentCard, _warPilePosition);
            
            if (finalPlayerCard != null)
            {
                await AnimateCardToPosition(finalPlayerCard, _playerCardPosition, true);
            }
            
            if (finalOpponentCard != null)
            {
                await AnimateCardToPosition(finalOpponentCard, _opponentCardPosition, true);
            }
            
            ShowRoundResult(warData.PlayerWins);
            
            Debug.Log("[CardAnimationController] War sequence animation complete");
        }
        
        public void Dispose()
        {
            Debug.Log("[CardAnimationController] Disposing");
            UnsubscribeFromEvents();
            ClearAllCards();
            
            DOTween.Kill(this);
        }
    }
}