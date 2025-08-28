using System;
using System.Collections.Generic;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using CardWar.Infrastructure.Events;
using CardWar.Services.Assets;
using CardWar.Services.Game;
using CardWar.UI.Cards;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace CardWar.Gameplay.Controllers
{
    public class CardAnimationController : MonoBehaviour, IInitializable, IDisposable
    {
        [Header("Animation Settings")]
        [SerializeField] private float _dealDelay = 0.3f;
        [SerializeField] private float _moveToWarDuration = 0.6f;
        [SerializeField] private float _flipDuration = 0.3f;
        [SerializeField] private float _resultPauseDuration = 1.5f;
        
        [Header("Positions")]
        [SerializeField] private Transform _playerCardPosition;
        [SerializeField] private Transform _opponentCardPosition;
        [SerializeField] private Transform _warPilePosition;
        [SerializeField] private Transform _deckPosition;
        
        private Transform _playerDeckPosition;
        private Transform _opponentDeckPosition;
        
        private IAssetService _assetService;
        private IGameService _gameService;
        private SignalBus _signalBus;
        private CardViewController.Pool _cardPool;

        private List<CardViewController> _activeCards;
        private bool _isAnimatingRound;
        
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
    
            _activeCards = new List<CardViewController>();
    
            FindDeckPositions();
            SubscribeToEvents();
            PreloadAssetsAsync().Forget();
        }
        
        private void FindDeckPositions()
        {
            GameObject playerDeck = GameObject.Find("PlayerDeckPosition");
            GameObject opponentDeck = GameObject.Find("OpponentDeckPosition");
            
            if (playerDeck != null)
            {
                _playerDeckPosition = playerDeck.transform;
            }
            else if (_deckPosition != null)
            {
                _playerDeckPosition = _deckPosition;
            }
            
            if (opponentDeck != null)
            {
                _opponentDeckPosition = opponentDeck.transform;
            }
            else if (_deckPosition != null)
            {
                _opponentDeckPosition = _deckPosition;
            }
            
            Debug.Log($"[CardAnimationController] Deck positions - Player: {_playerDeckPosition != null}, Opponent: {_opponentDeckPosition != null}");
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
            Debug.Log("[CardAnimationController] Round started - preparing animation");
        }
        
        private void OnRoundComplete(RoundCompleteEvent eventData)
        {
            if (_isAnimatingRound) return;
            
            var result = eventData.Result;
            Debug.Log($"[CardAnimationController] Playing round animation - Result: {result.Result}");
            
            AnimateCompleteRound(result).Forget();
        }
        
        private async UniTaskVoid AnimateCompleteRound(GameRoundResultData result)
        {
            _isAnimatingRound = true;
            
            try
            {
                // Step 1: Spawn cards at deck positions (face down)
                Transform playerStart = _playerDeckPosition ?? _deckPosition;
                Transform opponentStart = _opponentDeckPosition ?? _deckPosition;
                
                var playerCard = SpawnCard(result.PlayerCard, playerStart.position, true);
                var opponentCard = SpawnCard(result.OpponentCard, opponentStart.position, false);
                
                playerCard.SetFaceDown(true);
                opponentCard.SetFaceDown(true);
                
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
                
                // Step 2: Move cards to war positions
                await AnimateCardsToWarPositions(playerCard, opponentCard);
                
                // Step 3: Flip cards to reveal
                await AnimateCardFlips(playerCard, opponentCard);
                
                // Step 4: Pause to show result
                await UniTask.Delay(TimeSpan.FromSeconds(_resultPauseDuration));
                
                // Step 5: Collect cards to winner
                await AnimateWinnerCollection(playerCard, opponentCard, result.Result);
                
                // Cleanup
                CleanupCard(playerCard);
                CleanupCard(opponentCard);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardAnimationController] Animation failed: {e.Message}");
            }
            finally
            {
                _isAnimatingRound = false;
            }
        }
        
        private async UniTask AnimateCardsToWarPositions(CardViewController playerCard, CardViewController opponentCard)
        {
            var playerSequence = DOTween.Sequence();
            var opponentSequence = DOTween.Sequence();
            
            // Player card moves up and slightly right
            playerSequence.Append(playerCard.transform.DOMove(_playerCardPosition.position, _moveToWarDuration)
                .SetEase(Ease.OutQuad));
            playerSequence.Join(playerCard.transform.DORotate(new Vector3(0, 0, -5f), _moveToWarDuration));
            
            // Opponent card moves down and slightly left
            opponentSequence.Append(opponentCard.transform.DOMove(_opponentCardPosition.position, _moveToWarDuration)
                .SetEase(Ease.OutQuad));
            opponentSequence.Join(opponentCard.transform.DORotate(new Vector3(0, 0, 5f), _moveToWarDuration));
            
            await UniTask.WhenAll(
                playerSequence.AsyncWaitForCompletion().AsUniTask(),
                opponentSequence.AsyncWaitForCompletion().AsUniTask()
            );
        }
        
        private async UniTask AnimateCardFlips(CardViewController playerCard, CardViewController opponentCard)
        {
            // Flip both cards simultaneously
            await UniTask.WhenAll(
                playerCard.FlipToFrontAsync(),
                opponentCard.FlipToFrontAsync()
            );
        }
        
        private async UniTask AnimateWinnerCollection(CardViewController playerCard, CardViewController opponentCard, GameResult result)
        {
            Transform winnerPosition = result == GameResult.PlayerWin ? 
                _playerDeckPosition ?? _playerCardPosition : 
                _opponentDeckPosition ?? _opponentCardPosition;
            
            var sequence = DOTween.Sequence();
            
            // Both cards move to winner's deck
            sequence.Append(playerCard.transform.DOMove(winnerPosition.position, 0.5f));
            sequence.Join(opponentCard.transform.DOMove(winnerPosition.position, 0.5f));
            sequence.Join(playerCard.transform.DOScale(0.8f, 0.5f));
            sequence.Join(opponentCard.transform.DOScale(0.8f, 0.5f));
            sequence.Join(playerCard.transform.DORotate(Vector3.zero, 0.5f));
            sequence.Join(opponentCard.transform.DORotate(Vector3.zero, 0.5f));
            
            // Fade out
            var playerCanvas = playerCard.GetComponent<CanvasGroup>();
            var opponentCanvas = opponentCard.GetComponent<CanvasGroup>();
            
            if (playerCanvas != null)
            {
                sequence.Join(DOTween.To(() => playerCanvas.alpha, x => playerCanvas.alpha = x, 0, 0.3f)
                    .SetDelay(0.2f));
            }
            
            if (opponentCanvas != null)
            {
                sequence.Join(DOTween.To(() => opponentCanvas.alpha, x => opponentCanvas.alpha = x, 0, 0.3f)
                    .SetDelay(0.2f));
            }
            
            await sequence.AsyncWaitForCompletion();
        }
        
        private void OnWarStart(WarStartEvent eventData)
        {
            Debug.Log($"[CardAnimationController] War animation starting - {eventData.WarData.AllWarRounds.Count} rounds");
            AnimateWarSequence(eventData.WarData).Forget();
        }
        
        private async UniTaskVoid AnimateWarSequence(WarData warData)
        {
            var warCards = new List<CardViewController>();
            
            try
            {
                foreach (var warRound in warData.AllWarRounds)
                {
                    await AnimateWarRound(warRound, warCards);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
                }
                
                // Collect all war cards to winner
                bool playerWins = warData.WinningPlayerNumber == 1;
                Transform winnerDeck = playerWins ? _playerDeckPosition : _opponentDeckPosition;
                
                if (winnerDeck != null)
                {
                    var sequence = DOTween.Sequence();
                    foreach (var card in warCards)
                    {
                        sequence.Join(card.transform.DOMove(winnerDeck.position, 0.8f));
                        sequence.Join(card.transform.DOScale(0.5f, 0.8f));
                        
                        var canvas = card.GetComponent<CanvasGroup>();
                        if (canvas != null)
                        {
                            sequence.Join(DOTween.To(() => canvas.alpha, x => canvas.alpha = x, 0, 0.5f)
                                .SetDelay(0.3f));
                        }
                    }
                    
                    await sequence.AsyncWaitForCompletion();
                }
                
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
            // Place face-down cards
            for (int i = 0; i < warRound.PlayerCards.Count; i++)
            {
                var playerCard = SpawnCard(warRound.PlayerCards[i], 
                    _playerDeckPosition?.position ?? _deckPosition.position, true);
                playerCard.SetFaceDown(true);
                
                Vector3 warPos = _warPilePosition.position + new Vector3(-30 + i * 15, -10, 0);
                await playerCard.transform.DOMove(warPos, 0.3f).AsyncWaitForCompletion();
                
                warCards.Add(playerCard);
            }
            
            for (int i = 0; i < warRound.OpponentCards.Count; i++)
            {
                var oppCard = SpawnCard(warRound.OpponentCards[i], 
                    _opponentDeckPosition?.position ?? _deckPosition.position, false);
                oppCard.SetFaceDown(true);
                
                Vector3 warPos = _warPilePosition.position + new Vector3(30 - i * 15, 10, 0);
                await oppCard.transform.DOMove(warPos, 0.3f).AsyncWaitForCompletion();
                
                warCards.Add(oppCard);
            }
            
            // Fighting cards
            if (warRound.PlayerFightingCard != null && warRound.OpponentFightingCard != null)
            {
                var playerFight = SpawnCard(warRound.PlayerFightingCard, 
                    _playerDeckPosition?.position ?? _deckPosition.position, true);
                var oppFight = SpawnCard(warRound.OpponentFightingCard, 
                    _opponentDeckPosition?.position ?? _deckPosition.position, false);
                
                warCards.Add(playerFight);
                warCards.Add(oppFight);
                
                playerFight.SetFaceDown(true);
                oppFight.SetFaceDown(true);
                
                await UniTask.WhenAll(
                    playerFight.transform.DOMove(_playerCardPosition.position, 0.5f).AsyncWaitForCompletion().AsUniTask(),
                    oppFight.transform.DOMove(_opponentCardPosition.position, 0.5f).AsyncWaitForCompletion().AsUniTask()
                );
                
                await UniTask.WhenAll(
                    playerFight.FlipToFrontAsync(),
                    oppFight.FlipToFrontAsync()
                );
                
                await UniTask.Delay(TimeSpan.FromSeconds(1f));
            }
        }
        
        private void OnGameStart(GameStartEvent eventData)
        {
            Debug.Log("[CardAnimationController] Game started - clearing cards");
            ClearAllCards();
        }
        
        private void OnGameEnd(GameEndEvent eventData)
        {
            Debug.Log("[CardAnimationController] Game ended");
            ClearAllCards();
        }
        
        private async UniTaskVoid PreloadAssetsAsync()
        {
            try
            {
                Debug.Log("[CardAnimationController] Starting asset preload");
        
                if (_assetService == null)
                {
                    Debug.LogError("[CardAnimationController] AssetService is null, cannot preload assets");
                    return;
                }
        
                var cardsToPreload = new List<CardData>();
        
                foreach (CardRank rank in Enum.GetValues(typeof(CardRank)))
                {
                    foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
                    {
                        cardsToPreload.Add(new CardData(suit, rank));
                    }
                }
        
                _assetService.PreloadCardSprites(cardsToPreload);
        
                await UniTask.Delay(100);
        
                Debug.Log($"[CardAnimationController] Preloaded {cardsToPreload.Count} card sprites");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardAnimationController] Failed to preload assets: {ex.Message}");
            }
        }
        
        private CardViewController SpawnCard(CardData cardData, Vector3 position, bool isPlayerCard)
        {
            var card = _cardPool.Spawn();
            card.transform.position = position;
            card.transform.localScale = Vector3.one;
            card.Setup(cardData);
            
            var frontSprite = _assetService.GetCardSprite(cardData);
            var backSprite = _assetService.GetCardBackSprite();
            card.SetCardSprites(frontSprite, backSprite);
            
            var canvasGroup = card.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
            
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
        
        public void Dispose()
        {
            Debug.Log("[CardAnimationController] Disposing");
            UnsubscribeFromEvents();
            ClearAllCards();
        }
    }
}