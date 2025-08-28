using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using CardWar.Infrastructure.Events;
using CardWar.UI.Cards;

namespace CardWar.Gameplay.Animation
{
    public class CardAnimationController : MonoBehaviour, IInitializable
    {
        [Header("Card Positions")]
        [SerializeField] private Transform _playerDeck;
        [SerializeField] private Transform _opponentDeck;
        [SerializeField] private Transform _playerPlayArea;
        [SerializeField] private Transform _opponentPlayArea;
        [SerializeField] private Transform _warPile;
        
        [Header("Animation Timings")]
        [SerializeField] private float _drawCardDuration = 0.5f;
        [SerializeField] private float _flipCardDuration = 0.3f;
        [SerializeField] private float _collectCardDuration = 0.6f;
        [SerializeField] private float _warCardSpacing = 0.2f;
        [SerializeField] private float _delayBetweenActions = 0.2f;
        
        private SignalBus _eventBus;
        private CardViewController.Pool _cardPool;
        
        private CardViewController _currentPlayerCard;
        private CardViewController _currentOpponentCard;
        private List<CardViewController> _warCards = new List<CardViewController>();
        
        [Inject]
        public void Construct(SignalBus eventBus, CardViewController.Pool cardPool)
        {
            _eventBus = eventBus;
            _cardPool = cardPool;
        }
        
        public void Initialize()
        {
            Debug.Log("[CardAnimationController] Initializing");
            
            // Find positions if not assigned
            FindCardPositions();
            
            // Subscribe to Events
            _eventBus.Subscribe<RoundCompleteEvent>(OnRoundComplete);
            _eventBus.Subscribe<WarStartEvent>(OnWarStart);
            _eventBus.Subscribe<GameStartEvent>(OnGameStart);
            _eventBus.Subscribe<GameEndEvent>(OnGameEnd);
        }
        
        private void FindCardPositions()
        {
            if (_playerDeck == null)
                _playerDeck = GameObject.Find("PlayerDeck")?.transform;
                
            if (_opponentDeck == null)
                _opponentDeck = GameObject.Find("OpponentDeck")?.transform;
                
            if (_playerPlayArea == null)
                _playerPlayArea = GameObject.Find("PlayerPlayArea")?.transform;
                
            if (_opponentPlayArea == null)
                _opponentPlayArea = GameObject.Find("OpponentPlayArea")?.transform;
                
            if (_warPile == null)
                _warPile = GameObject.Find("WarPile")?.transform;
                
            // Log warnings for missing references
            if (_playerDeck == null) Debug.LogWarning("[CardAnimationController] PlayerDeck not found");
            if (_opponentDeck == null) Debug.LogWarning("[CardAnimationController] OpponentDeck not found");
            if (_playerPlayArea == null) Debug.LogWarning("[CardAnimationController] PlayerPlayArea not found");
            if (_opponentPlayArea == null) Debug.LogWarning("[CardAnimationController] OpponentPlayArea not found");
            if (_warPile == null) Debug.LogWarning("[CardAnimationController] WarPile not found");
        }
        
        private async void OnRoundComplete(RoundCompleteEvent Event)
        {
            await AnimateRoundResult(Event.Result);
        }
        
        private async UniTask AnimateRoundResult(GameRoundResultData result)
        {
            // Clean up any previous cards
            CleanupCurrentCards();
            
            // Spawn and animate player card
            if (_cardPool != null && result.PlayerCard != null)
            {
                _currentPlayerCard = _cardPool.Spawn();
                _currentPlayerCard.Setup(result.PlayerCard);
                
                if (_playerDeck != null && _playerPlayArea != null)
                {
                    await _currentPlayerCard.AnimateDeal(
                        _playerDeck.position,
                        _playerPlayArea.position,
                        0
                    );
                    
                    // Flip card to show face
                    await _currentPlayerCard.FlipToFront(result.PlayerCard);
                }
            }
            
            // Spawn and animate opponent card
            if (_cardPool != null && result.OpponentCard != null)
            {
                _currentOpponentCard = _cardPool.Spawn();
                _currentOpponentCard.Setup(result.OpponentCard);
                
                if (_opponentDeck != null && _opponentPlayArea != null)
                {
                    await _currentOpponentCard.AnimateDeal(
                        _opponentDeck.position,
                        _opponentPlayArea.position,
                        0.1f // Slight delay for visual effect
                    );
                    
                    // Flip card to show face
                    await _currentOpponentCard.FlipToFront(result.OpponentCard);
                }
            }
            
            // Pause to show result
            await UniTask.Delay((int)(_delayBetweenActions * 1000));
            
            // Handle result animation
            switch (result.Result)
            {
                case GameResult.PlayerWin:
                    await AnimatePlayerWin();
                    break;
                    
                case GameResult.OpponentWin:
                    await AnimateOpponentWin();
                    break;
                    
                case GameResult.War:
                    // War animation handled separately
                    break;
            }
            
            // Clean up cards after regular round (not war)
            if (result.Result != GameResult.War)
            {
                await UniTask.Delay(500);
                CleanupCurrentCards();
            }
        }
        
        private async UniTask AnimatePlayerWin()
        {
            if (_currentPlayerCard == null || _currentOpponentCard == null) return;
            
            // Victory effect on player card
            _currentPlayerCard.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
            
            await UniTask.Delay(300);
            
            // Collect both cards to player deck
            if (_playerDeck != null)
            {
                var tasks = new List<UniTask>();
                tasks.Add(_currentPlayerCard.AnimateWin(_playerDeck.position));
                tasks.Add(_currentOpponentCard.AnimateWin(_playerDeck.position));
                
                await UniTask.WhenAll(tasks);
            }
        }
        
        private async UniTask AnimateOpponentWin()
        {
            if (_currentPlayerCard == null || _currentOpponentCard == null) return;
            
            // Victory effect on opponent card
            _currentOpponentCard.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
            
            await UniTask.Delay(300);
            
            // Collect both cards to opponent deck
            if (_opponentDeck != null)
            {
                var tasks = new List<UniTask>();
                tasks.Add(_currentPlayerCard.AnimateWin(_opponentDeck.position));
                tasks.Add(_currentOpponentCard.AnimateWin(_opponentDeck.position));
                
                await UniTask.WhenAll(tasks);
            }
        }
        
        private async void OnWarStart(WarStartEvent Event)
        {
            await AnimateWar(Event.WarData);
        }
        
        private async UniTask AnimateWar(WarData warData)
        {
            if (warData == null) return;
            
            Debug.Log($"[CardAnimationController] Animating war with {warData.AllWarRounds.Count} rounds");
            
            // Move initial cards to war pile
            if (_currentPlayerCard != null && _currentOpponentCard != null && _warPile != null)
            {
                _currentPlayerCard.ShowWarHighlight();
                _currentOpponentCard.ShowWarHighlight();
                
                await UniTask.WhenAll(
                    _currentPlayerCard.MoveTo(_warPile.position),
                    _currentOpponentCard.MoveTo(_warPile.position + Vector3.right * 0.5f)
                );
                
                _warCards.Add(_currentPlayerCard);
                _warCards.Add(_currentOpponentCard);
            }
            
            // Animate each war round
            foreach (var round in warData.AllWarRounds)
            {
                await AnimateWarRound(round);
                await UniTask.Delay(500);
            }
            
            // Collect all war cards to winner
            await CollectWarCards(warData.WinningPlayerNumber);
            
            // Clean up
            CleanupWarCards();
        }
        
        private async UniTask AnimateWarRound(WarRound round)
        {
            var tasks = new List<UniTask>();
            
            // Place face-down cards
            float cardOffset = _warCards.Count * _warCardSpacing;
            
            // Player cards
            foreach (var cardData in round.PlayerCards)
            {
                if (_cardPool != null && _playerDeck != null && _warPile != null)
                {
                    var card = _cardPool.Spawn();
                    card.Setup(cardData);
                    card.SetFaceUp(false, true);
                    
                    Vector3 targetPos = _warPile.position + Vector3.left * cardOffset;
                    tasks.Add(card.AnimateDeal(_playerDeck.position, targetPos, 0));
                    
                    _warCards.Add(card);
                    cardOffset += _warCardSpacing;
                }
            }
            
            // Opponent cards
            foreach (var cardData in round.OpponentCards)
            {
                if (_cardPool != null && _opponentDeck != null && _warPile != null)
                {
                    var card = _cardPool.Spawn();
                    card.Setup(cardData);
                    card.SetFaceUp(false, true);
                    
                    Vector3 targetPos = _warPile.position + Vector3.right * cardOffset;
                    tasks.Add(card.AnimateDeal(_opponentDeck.position, targetPos, 0.1f));
                    
                    _warCards.Add(card);
                    cardOffset += _warCardSpacing;
                }
            }
            
            await UniTask.WhenAll(tasks);
            
            // Flip fighting cards
            if (round.PlayerFightingCard != null && round.OpponentFightingCard != null)
            {
                // Find the last cards (fighting cards)
                if (_warCards.Count >= 2)
                {
                    var playerFightCard = _warCards[_warCards.Count - 2];
                    var opponentFightCard = _warCards[_warCards.Count - 1];
                    
                    await UniTask.WhenAll(
                        playerFightCard.FlipToFront(round.PlayerFightingCard),
                        opponentFightCard.FlipToFront(round.OpponentFightingCard)
                    );
                }
            }
        }
        
        private async UniTask CollectWarCards(int winningPlayer)
        {
            Transform targetDeck = winningPlayer == 1 ? _playerDeck : _opponentDeck;
            
            if (targetDeck == null) return;
            
            // Stop all war highlights
            foreach (var card in _warCards)
            {
                card.StopWarHighlight();
            }
            
            // Collect all cards with staggered timing
            var tasks = new List<UniTask>();
            float delay = 0;
            
            foreach (var card in _warCards)
            {
                tasks.Add(CollectCardWithDelay(card, targetDeck.position, delay));
                delay += 0.05f;
            }
            
            await UniTask.WhenAll(tasks);
        }
        
        private async UniTask CollectCardWithDelay(CardViewController card, Vector3 targetPosition, float delay)
        {
            if (delay > 0)
                await UniTask.Delay((int)(delay * 1000));
                
            await card.AnimateWin(targetPosition);
        }
        
        private void OnGameStart()
        {
            CleanupAllCards();
        }
        
        private void OnGameEnd(GameEndEvent Event)
        {
            // Optional: Show final card positions or effects
        }
        
        private void CleanupCurrentCards()
        {
            if (_currentPlayerCard != null && _cardPool != null)
            {
                _cardPool.Despawn(_currentPlayerCard);
                _currentPlayerCard = null;
            }
            
            if (_currentOpponentCard != null && _cardPool != null)
            {
                _cardPool.Despawn(_currentOpponentCard);
                _currentOpponentCard = null;
            }
        }
        
        private void CleanupWarCards()
        {
            if (_cardPool != null)
            {
                foreach (var card in _warCards)
                {
                    if (card != null)
                        _cardPool.Despawn(card);
                }
            }
            
            _warCards.Clear();
            _currentPlayerCard = null;
            _currentOpponentCard = null;
        }
        
        private void CleanupAllCards()
        {
            CleanupCurrentCards();
            CleanupWarCards();
        }
        
        private void OnDestroy()
        {
            _eventBus?.TryUnsubscribe<RoundCompleteEvent>(OnRoundComplete);
            _eventBus?.TryUnsubscribe<WarStartEvent>(OnWarStart);
            _eventBus?.TryUnsubscribe<GameStartEvent>(OnGameStart);
            _eventBus?.TryUnsubscribe<GameEndEvent>(OnGameEnd);
            
            CleanupAllCards();
        }
    }
}