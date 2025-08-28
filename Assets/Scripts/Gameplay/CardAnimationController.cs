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
            }
        }
        
        private void OnRoundStart(RoundStartEvent eventData)
        {
            Debug.Log("[CardAnimationController] Round started");
        }
        
        private void OnRoundComplete(RoundCompleteEvent eventData)
        {
            var result = eventData.Result;
            int playerScore = _gameService.PlayerCardCount;
            int opponentScore = _gameService.OpponentCardCount;
            
            Debug.Log($"[CardAnimationController] Round complete - Result: {result.Result}, Player: {playerScore}, Opponent: {opponentScore}");
            
            AnimateRoundResult(result).Forget();
        }
        
        private async UniTaskVoid AnimateRoundResult(GameRoundResultData result)
        {
            var playerCard = SpawnCard(result.PlayerCard, _playerCardPosition.position, true);
            var opponentCard = SpawnCard(result.OpponentCard, _opponentCardPosition.position, false);
            
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            
            await playerCard.FlipToFrontAsync();
            await opponentCard.FlipToFrontAsync();
            
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            
            Transform winnerPosition = result.Result == GameResult.PlayerWin ? _playerCardPosition : _opponentCardPosition;
            
            int playerCardCount = _gameService.PlayerCardCount;
            int opponentCardCount = _gameService.OpponentCardCount;
            
            if (playerCardCount > 0 && opponentCardCount > 0)
            {
                await AnimateCardCollection(playerCard, opponentCard, winnerPosition);
            }
            
            CleanupCard(playerCard);
            CleanupCard(opponentCard);
        }
        
        private void OnWarStart(WarStartEvent eventData)
        {
            Debug.Log($"[CardAnimationController] War started with {eventData.WarData.AllWarRounds.Count} rounds");
            AnimateWar(eventData.WarData).Forget();
        }
        
        private void OnGameStart(GameStartEvent eventData)
        {
            Debug.Log("[CardAnimationController] Game started");
            ClearAllCards();
        }
        
        private void OnGameEnd(GameEndEvent eventData)
        {
            Debug.Log($"[CardAnimationController] Game ended - Winner: Player {eventData.WinnerPlayerNumber}");
            ClearAllCards();
        }
        
        private async UniTaskVoid PreloadAssetsAsync()
        {
            try
            {
                if (_assetService != null)
                {
                    await _assetService.PreloadCardAssets();
                    Debug.Log("[CardAnimationController] Card assets preloaded");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardAnimationController] Failed to preload assets: {e.Message}");
            }
        }
        
        private CardViewController SpawnCard(CardData cardData, Vector3 position, bool isPlayerCard)
        {
            var card = _cardPool.Spawn();
            card.transform.position = position;
            card.Setup(cardData);
            
            var frontSprite = _assetService.GetCardSprite(cardData);
            var backSprite = _assetService.GetCardBackSprite();
            card.SetCardSprites(frontSprite, backSprite);
            
            card.SetFaceDown(true);
            
            _activeCards.Add(card);
            return card;
        }
        
        private void CleanupCard(CardViewController card)
        {
            if (card != null)
            {
                _activeCards.Remove(card);
                _cardPool.Despawn(card);
            }
        }
        
        private void ClearAllCards()
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
        
        private async UniTask AnimateCardCollection(CardViewController playerCard, CardViewController opponentCard, Transform winnerPosition)
        {
            var sequence = DOTween.Sequence();
            
            sequence.Append(playerCard.transform.DOMove(winnerPosition.position, 0.5f));
            sequence.Join(opponentCard.transform.DOMove(winnerPosition.position, 0.5f));
            sequence.Join(playerCard.transform.DOScale(0, 0.5f));
            sequence.Join(opponentCard.transform.DOScale(0, 0.5f));
            
            await sequence.AsyncWaitForCompletion();
        }
        
        private async UniTaskVoid AnimateWar(WarData warData)
        {
            Debug.Log($"[CardAnimationController] Animating war with {warData.AllWarRounds.Count} rounds");
            
            var warCards = new List<CardViewController>();
            
            try
            {
                foreach (var warRound in warData.AllWarRounds)
                {
                    await AnimateWarRound(warRound, warCards);
                }
                
                await UniTask.Delay(TimeSpan.FromSeconds(1f));
                
                bool playerWins = warData.WinningPlayerNumber == 1;
                Transform winnerPosition = playerWins ? _playerCardPosition : _opponentCardPosition;
                
                var sequence = DOTween.Sequence();
                foreach (var card in warCards)
                {
                    sequence.Join(card.transform.DOMove(winnerPosition.position, 0.5f));
                    sequence.Join(card.transform.DOScale(0, 0.5f));
                }
                
                await sequence.AsyncWaitForCompletion();
                
                foreach (var card in warCards)
                {
                    CleanupCard(card);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardAnimationController] War animation failed: {e.Message}");
                foreach (var card in warCards)
                {
                    CleanupCard(card);
                }
            }
        }
        
        private async UniTask AnimateWarRound(WarRound warRound, List<CardViewController> warCards)
        {
            int cardsPerSide = Math.Max(warRound.PlayerCards.Count, warRound.OpponentCards.Count);
            
            for (int i = 0; i < cardsPerSide; i++)
            {
                if (i < warRound.PlayerCards.Count)
                {
                    var playerCard = SpawnCard(warRound.PlayerCards[i], _deckPosition.position, true);
                    warCards.Add(playerCard);
                    
                    Vector3 targetPos = _warPilePosition.position + new Vector3(i * 0.5f, 0, 0);
                    await playerCard.transform.DOMove(targetPos, 0.3f).AsyncWaitForCompletion();
                }
                
                if (i < warRound.OpponentCards.Count)
                {
                    var opponentCard = SpawnCard(warRound.OpponentCards[i], _deckPosition.position, false);
                    warCards.Add(opponentCard);
                    
                    Vector3 targetPos = _warPilePosition.position + new Vector3(-i * 0.5f, 0, 0);
                    await opponentCard.transform.DOMove(targetPos, 0.3f).AsyncWaitForCompletion();
                }
                
                await UniTask.Delay(TimeSpan.FromSeconds(_dealDelay));
            }
            
            if (warRound.PlayerFightingCard != null && warRound.OpponentFightingCard != null)
            {
                var playerFightCard = SpawnCard(warRound.PlayerFightingCard, _deckPosition.position, true);
                var opponentFightCard = SpawnCard(warRound.OpponentFightingCard, _deckPosition.position, false);
                
                warCards.Add(playerFightCard);
                warCards.Add(opponentFightCard);
                
                await playerFightCard.transform.DOMove(_playerCardPosition.position, 0.5f).AsyncWaitForCompletion();
                await opponentFightCard.transform.DOMove(_opponentCardPosition.position, 0.5f).AsyncWaitForCompletion();
                
                await playerFightCard.FlipToFrontAsync();
                await opponentFightCard.FlipToFrontAsync();
                
                await UniTask.Delay(TimeSpan.FromSeconds(1f));
            }
        }
        
        public void Dispose()
        {
            Debug.Log("[CardAnimationController] Disposing");
            UnsubscribeFromEvents();
            ClearAllCards();
        }
    }
}