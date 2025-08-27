using System;
using System.Threading;
using CardWar.Core.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using CardWar.Core.Enums;
using CardWar.Core.Events;

namespace CardWar.Gameplay.Players
{
    /// <summary>
    /// Controller for the local human player
    /// </summary>
    public class LocalPlayerController : PlayerController
    {
        private Button _drawCardButton;
        private bool _isWaitingForInput;
        private CardData _nextCardToPlay;
        private UniTaskCompletionSource<CardData> _cardPlayTcs;
        
        public override bool IsLocalPlayer => true;
        
        public override async UniTask InitializeAsync(PlayerConfiguration config)
        {
            await base.InitializeAsync(config);
            
            // Subscribe to input signals
            _signalBus.Subscribe<PlayerInputSignal>(OnPlayerInput);
            
            // Find or create draw button
            await CreateDrawButtonAsync();
        }
        
        private async UniTask CreateDrawButtonAsync()
        {
            try
            {
                // TODO: Get button reference from UI or create it
                var buttonPath = "Prefabs/UI/DrawCardButton";
                var buttonGO = await _assetManager.InstantiateAsync(
                    buttonPath,
                    _config.CardSlotTransform,
                    _cancellationTokenSource.Token);
                
                _drawCardButton = buttonGO.GetComponent<Button>();
                if (_drawCardButton != null)
                {
                    _drawCardButton.onClick.AddListener(OnDrawButtonClicked);
                    _drawCardButton.interactable = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalPlayerController] Failed to create draw button: {ex.Message}");
                // TODO: Implement fallback input method (tap on deck?)
            }
        }
        
        public override async UniTask<CardData> PlayCardAsync()
        {
            if (!IsMyTurn)
            {
                Debug.LogWarning("[LocalPlayerController] Cannot play card - not my turn");
                return null;
            }
            
            if (!HasCards)
            {
                Debug.LogWarning("[LocalPlayerController] Cannot play card - no cards left");
                return null;
            }
            
            Debug.Log("[LocalPlayerController] Waiting for player to draw card...");
            
            // Enable input
            _isWaitingForInput = true;
            SetInteractable(true);
            
            // Create completion source for async waiting
            _cardPlayTcs = new UniTaskCompletionSource<CardData>();
            
            try
            {
                // Wait for player input with timeout
                var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
                    _cancellationTokenSource.Token, 
                    timeoutToken.Token);
                
                var card = await _cardPlayTcs.Task.AttachExternalCancellation(linkedToken.Token);
                
                _isWaitingForInput = false;
                SetInteractable(false);
                
                // Play the card
                _cardCount--;
                UpdateCardCountDisplay(_cardCount);
                InvokeOnCardPlayed(card);
                
                return card;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[LocalPlayerController] Card play cancelled or timed out");
                _isWaitingForInput = false;
                SetInteractable(false);
                throw;
            }
        }
        
        public override async UniTask<CardData> PlaceFightingCardAsync()
        {
            // For war, automatically play the next card without user input
            if (!HasCards)
            {
                Debug.LogWarning("[LocalPlayerController] Cannot place fighting card - no cards left");
                return null;
            }
            
            // Simulate drawing from deck (server would provide actual card)
            var fightingCard = new CardData(CardSuit.Hearts, CardRank.Ace); // Placeholder
            
            _cardCount--;
            UpdateCardCountDisplay(_cardCount);
            
            // Create and show card
            var cardView = _cardPool.Spawn();
            cardView.Setup(fightingCard);
            cardView.transform.position = _deck.DrawPosition;
            _activeCards.Add(cardView);
            
            // Animate to slot
            await _cardSlot.PlaceCardAsync(cardView, animate: true);
            
            // Flip face up
            await cardView.FlipToFront(fightingCard);
            
            InvokeOnCardPlayed(fightingCard);
            
            return fightingCard;
        }
        
        private void OnDrawButtonClicked()
        {
            if (!_isWaitingForInput || !IsMyTurn)
                return;
            
            Debug.Log("[LocalPlayerController] Draw button clicked");
            
            // Disable button to prevent double-clicks
            if (_drawCardButton != null)
                _drawCardButton.interactable = false;
            
            // Simulate drawing a card (server would provide actual card)
            var drawnCard = new CardData(CardSuit.Spades, CardRank.King); // Placeholder
            
            // Complete the async task
            _cardPlayTcs?.TrySetResult(drawnCard);
        }
        
        private void OnPlayerInput(PlayerInputSignal signal)
        {
            if (!_isWaitingForInput || !IsMyTurn)
                return;
            
            // Handle alternative input methods (tap on deck, gesture, etc.)
            if (signal.InputType == PlayerInputType.TapDeck)
            {
                OnDrawButtonClicked();
            }
        }
        
        public override void SetInteractable(bool interactable)
        {
            base.SetInteractable(interactable);
            
            if (_drawCardButton != null)
            {
                _drawCardButton.interactable = interactable && HasCards;
            }
            
            // Update visual feedback for player
            if (interactable)
            {
                ShowInputHint();
            }
            else
            {
                HideInputHint();
            }
        }
        
        private void ShowInputHint()
        {
            // TODO: Show visual hint to player (pulsing button, arrow, etc.)
            Debug.Log("[LocalPlayerController] Your turn - tap to draw!");
        }
        
        private void HideInputHint()
        {
            // TODO: Hide visual hints
        }
        
        public override void Dispose()
        {
            _signalBus?.TryUnsubscribe<PlayerInputSignal>(OnPlayerInput);
            
            if (_drawCardButton != null)
            {
                _drawCardButton.onClick.RemoveAllListeners();
            }
            
            _cardPlayTcs?.TrySetCanceled();
            
            base.Dispose();
        }
    }
    
    // Input signal for alternative input methods
    public class PlayerInputSignal
    {
        public PlayerInputType InputType { get; }
        public Vector3 Position { get; }
        
        public PlayerInputSignal(PlayerInputType type, Vector3 position = default)
        {
            InputType = type;
            Position = position;
        }
    }
    

}