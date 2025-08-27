using System;
using CardWar.Core.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;
using CardWar.Core.Enums;
using CardWar.Core.Events;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Controller for the AI opponent player
    /// </summary>
    public class AIPlayerController : PlayerController
    {
        [Header("AI Settings")]
        [SerializeField] private float _minThinkTime = 0.5f;
        [SerializeField] private float _maxThinkTime = 2f;
        [SerializeField] private bool _simulateHesitation = true;
        
        public override bool IsLocalPlayer => false;
        
        public override async UniTask<CardData> PlayCardAsync()
        {
            if (!IsMyTurn)
            {
                Debug.LogWarning("[AIPlayerController] Cannot play card - not my turn");
                return null;
            }
            
            if (!HasCards)
            {
                Debug.LogWarning("[AIPlayerController] Cannot play card - no cards left");
                return null;
            }
            
            Debug.Log("[AIPlayerController] AI thinking...");
            
            // Simulate AI "thinking" time
            if (_simulateHesitation)
            {
                var thinkTime = Random.Range(_minThinkTime, _maxThinkTime);
                await UniTask.Delay(TimeSpan.FromSeconds(thinkTime), 
                    cancellationToken: _cancellationTokenSource.Token);
            }
            
            // Server will provide the actual card
            // For now, create a placeholder
            var aiCard = new CardData(
                (CardSuit)Random.Range(0, 4),
                (CardRank)Random.Range(2, 15)
            );
            
            _cardCount--;
            UpdateCardCountDisplay(_cardCount);
            InvokeOnCardPlayed(aiCard);
            
            Debug.Log($"[AIPlayerController] AI plays: {aiCard}");
            
            return aiCard;
        }
        
        public override async UniTask<CardData> PlaceFightingCardAsync()
        {
            if (!HasCards)
            {
                Debug.LogWarning("[AIPlayerController] Cannot place fighting card - no cards left");
                return null;
            }
            
            // Quick decision for war cards (no hesitation)
            await UniTask.Delay(TimeSpan.FromSeconds(0.3f), 
                cancellationToken: _cancellationTokenSource.Token);
            
            // Server provides the card
            var fightingCard = new CardData(
                (CardSuit)Random.Range(0, 4),
                (CardRank)Random.Range(2, 15)
            );
            
            _cardCount--;
            UpdateCardCountDisplay(_cardCount);
            
            // Create and show card
            var cardView = _cardPool.Spawn();
            cardView.Setup(fightingCard);
            cardView.transform.position = _deck.DrawPosition;
            _activeCards.Add(cardView);
            
            // Animate to slot
            await _cardSlot.PlaceCardAsync(cardView, animate: true);
            
            // Flip face up with dramatic pause
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f), 
                cancellationToken: _cancellationTokenSource.Token);
            
            await _animationService.AnimateCardFlip(cardView, fightingCard);
            
            InvokeOnCardPlayed(fightingCard);
            
            return fightingCard;
        }
        
        public override void SetInteractable(bool interactable)
        {
            // AI doesn't need interactable UI
            base.SetInteractable(false);
        }
        
        public override void StartTurn()
        {
            base.StartTurn();
            
            // AI automatically plays when its turn starts
            if (_isMyTurn)
            {
                AutoPlayCard().Forget();
            }
        }
        
        private async UniTaskVoid AutoPlayCard()
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), 
                    cancellationToken: _cancellationTokenSource.Token);
                
                // Signal to game service that AI is ready to play
                _signalBus.Fire(new AIReadyToPlaySignal(this));
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[AIPlayerController] Auto-play cancelled");
            }
        }
        
        public void SetAIDifficulty(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    _minThinkTime = 1f;
                    _maxThinkTime = 3f;
                    _simulateHesitation = true;
                    break;
                    
                case AIDifficulty.Normal:
                    _minThinkTime = 0.5f;
                    _maxThinkTime = 2f;
                    _simulateHesitation = true;
                    break;
                    
                case AIDifficulty.Hard:
                    _minThinkTime = 0.2f;
                    _maxThinkTime = 0.8f;
                    _simulateHesitation = false;
                    break;
                    
                case AIDifficulty.Instant:
                    _minThinkTime = 0f;
                    _maxThinkTime = 0f;
                    _simulateHesitation = false;
                    break;
            }
            
            Debug.Log($"[AIPlayerController] Difficulty set to {difficulty}");
        }
    }
    
    public class AIReadyToPlaySignal
    {
        public IPlayerController AIPlayer { get; }
        
        public AIReadyToPlaySignal(IPlayerController aiPlayer)
        {
            AIPlayer = aiPlayer;
        }
    }
}