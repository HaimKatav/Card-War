using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;
using CardWar.Services.Assets;
using CardWar.Core.Data;
using CardWar.Gameplay.Cards;

namespace CardWar.Gameplay.Players
{
    /// <summary>
    /// Base implementation of player controller with shared functionality
    /// </summary>
    public abstract class PlayerController : MonoBehaviour, IPlayerController
    {
        // Dependencies
        protected IAssetManager _assetManager;
        protected SignalBus _signalBus;
        protected CardView.Pool _cardPool;
        
        // Configuration
        protected PlayerConfiguration _config;
        protected CancellationTokenSource _cancellationTokenSource;
        
        // Components
        protected ICardDeck _deck;
        protected ICardSlot _cardSlot;
        protected PlayerCardCountUI _cardCountUI;
        
        // State
        protected bool _isMyTurn;
        protected int _cardCount;
        protected bool _isInitialized;
        protected List<CardView> _activeCards;
        
        // Properties
        public int PlayerNumber => _config?.PlayerNumber ?? 0;
        public string PlayerName => _config?.PlayerName ?? "Unknown";
        public abstract bool IsLocalPlayer { get; }
        public bool IsMyTurn => _isMyTurn;
        public int CardCount => _cardCount;
        public bool HasCards => _cardCount > 0;
        public ICardDeck Deck => _deck;
        public ICardSlot CardSlot => _cardSlot;
        public Transform DeckTransform => _deck?.Transform;
        public Transform CardSlotTransform => _cardSlot?.Transform;
        
        // Events
        public event Action<IPlayerController> OnTurnStarted;
        public event Action<IPlayerController> OnTurnEnded;
        public event Action<CardData> OnCardPlayed;
        public event Action<int> OnCardCountChanged;
        
        // Protected methods for inheritors to invoke events
        protected virtual void InvokeOnTurnStarted() => OnTurnStarted?.Invoke(this);
        protected virtual void InvokeOnTurnEnded() => OnTurnEnded?.Invoke(this);
        protected virtual void InvokeOnCardPlayed(CardData card) => OnCardPlayed?.Invoke(card);
        protected virtual void InvokeOnCardCountChanged(int newCount) => OnCardCountChanged?.Invoke(newCount);
        
        [Inject]
        public virtual void Construct(
            IAssetManager assetManager,
            SignalBus signalBus,
            CardView.Pool cardPool)
        {
            _assetManager = assetManager;
            _signalBus = signalBus;
            _cardPool = cardPool;
            _cancellationTokenSource = new CancellationTokenSource();
            _activeCards = new List<CardView>();
        }
        
        public virtual async UniTask InitializeAsync(PlayerConfiguration config)
        {
            if (_isInitialized)
            {
                Debug.LogWarning($"[PlayerController] {PlayerName} already initialized");
                return;
            }
            
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            Debug.Log($"[PlayerController] Initializing {PlayerName} (Player {PlayerNumber})");
            
            try
            {
                // Create deck component
                await CreateDeckAsync();
                
                // Create card slot component
                await CreateCardSlotAsync();
                
                // Create UI components
                await CreateUIComponentsAsync();
                
                // Setup initial state
                _cardCount = 26; // Starting card count
                UpdateCardCountDisplay(_cardCount);
                
                _isInitialized = true;
                Debug.Log($"[PlayerController] {PlayerName} initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerController] Failed to initialize {PlayerName}: {ex.Message}");
                throw;
            }
        }
        
        protected virtual async UniTask CreateDeckAsync()
        {
            var deckPath = _config.CustomDeckPrefabPath ?? "Prefabs/Game/CardDeck";
            var deckGO = await _assetManager.InstantiateAsync(
                deckPath,
                _config.DeckTransform,
                _cancellationTokenSource.Token);
            
            _deck = deckGO.GetComponent<ICardDeck>();
            if (_deck == null)
            {
                var simpleDeck = deckGO.AddComponent<SimpleCardDeck>();
                _deck = simpleDeck;
            }
            
            _deck.SetCardCount(_cardCount);
        }
        
        protected virtual async UniTask CreateCardSlotAsync()
        {
            var slotPath = _config.CustomCardSlotPrefabPath ?? "Prefabs/Game/CardSlot";
            var slotGO = await _assetManager.InstantiateAsync(
                slotPath,
                _config.CardSlotTransform,
                _cancellationTokenSource.Token);
            
            _cardSlot = slotGO.GetComponent<ICardSlot>();
            if (_cardSlot == null)
            {
                var simpleSlot = slotGO.AddComponent<SimpleCardSlot>();
                _cardSlot = simpleSlot;
            }
        }
        
        protected virtual async UniTask CreateUIComponentsAsync()
        {
            if (_config.CardCountUITransform != null)
            {
                var uiPath = "Prefabs/UI/PlayerCardCountUI";
                _cardCountUI = await _assetManager.InstantiateAsync<PlayerCardCountUI>(
                    uiPath,
                    _config.CardCountUITransform,
                    _cancellationTokenSource.Token);
                
                _cardCountUI.SetPlayerName(PlayerName);
                _cardCountUI.UpdateCount(_cardCount);
            }
        }
        
        public abstract UniTask<CardData> PlayCardAsync();
        
        public virtual async UniTask ShowCardAsync(CardData card)
        {
            if (card == null)
            {
                Debug.LogError($"[PlayerController] {PlayerName} cannot show null card");
                return;
            }
            
            // Get card from pool
            var cardView = _cardPool.Spawn();
            cardView.Setup(card);
            cardView.transform.position = _deck.DrawPosition;
            _activeCards.Add(cardView);
            
            // Animate to slot
            await _cardSlot.PlaceCardAsync(cardView, animate: true);
            
            // Flip card face up
            await cardView.FlipToFront(card);
            
            Debug.Log($"[PlayerController] {PlayerName} showed card: {card}");
        }
        
        public virtual async UniTask CollectCardsAsync(List<CardData> wonCards)
        {
            if (wonCards == null || wonCards.Count == 0)
                return;
            
            _cardCount += wonCards.Count;
            UpdateCardCountDisplay(_cardCount);
            
            // Animate cards flying to deck
            var tasks = new List<UniTask>();
            foreach (var cardView in _cardSlot.GetAllCards())
            {
                tasks.Add(AnimateCardToDeckAsync(cardView));
            }
            
            await UniTask.WhenAll(tasks);
            
            // Clear active cards
            ClearActiveCards();
            
            InvokeOnCardCountChanged(_cardCount);
            Debug.Log($"[PlayerController] {PlayerName} collected {wonCards.Count} cards. Total: {_cardCount}");
        }
        
        public virtual async UniTask ReturnCardsAsync(List<CardData> cards)
        {
            if (cards == null || cards.Count == 0)
                return;
            
            // In case of draw, cards are returned and deck is shuffled
            _cardCount += cards.Count;
            UpdateCardCountDisplay(_cardCount);
            
            await _deck.AnimateShuffleAsync();
            
            InvokeOnCardCountChanged(_cardCount);
            Debug.Log($"[PlayerController] {PlayerName} got back {cards.Count} cards after draw");
        }
        
        public virtual async UniTask<List<CardData>> PlaceConcealedCardsAsync(int count)
        {
            var concealedCards = new List<CardData>();
            
            for (int i = 0; i < count; i++)
            {
                var cardView = _cardPool.Spawn();
                cardView.SetFaceUp(false, immediate: true);
                cardView.transform.position = _deck.DrawPosition;
                _activeCards.Add(cardView);
                
                await _cardSlot.PlaceConcealedCardAsync(cardView, i);
                
                // Note: In real implementation, card data would come from server
                // For now, we're just creating placeholder views
            }
            
            _cardCount -= count;
            UpdateCardCountDisplay(_cardCount);
            InvokeOnCardCountChanged(_cardCount);
            
            Debug.Log($"[PlayerController] {PlayerName} placed {count} concealed cards");
            return concealedCards;
        }
        
        public abstract UniTask<CardData> PlaceFightingCardAsync();
        
        public virtual async UniTask ShowWarResultAsync(bool won, List<CardData> cardsWon)
        {
            if (won)
            {
                // Winning animation
                _cardSlot.SetHighlight(true);
                await UniTask.Delay(500);
                _cardSlot.SetHighlight(false);
                
                if (cardsWon != null)
                {
                    await CollectCardsAsync(cardsWon);
                }
            }
            else
            {
                // Losing animation - cards fly to opponent
                await UniTask.Delay(500);
                ClearActiveCards();
            }
        }
        
        public virtual void StartTurn()
        {
            _isMyTurn = true;
            SetInteractable(true);
            HighlightPlayer(true);
            InvokeOnTurnStarted();
            
            Debug.Log($"[PlayerController] {PlayerName}'s turn started");
        }
        
        public virtual void EndTurn()
        {
            _isMyTurn = false;
            SetInteractable(false);
            HighlightPlayer(false);
            InvokeOnTurnEnded();
            
            Debug.Log($"[PlayerController] {PlayerName}'s turn ended");
        }
        
        public virtual void UpdateCardCountDisplay(int count)
        {
            _cardCount = count;
            _deck?.SetCardCount(count);
            _cardCountUI?.UpdateCount(count);
        }
        
        public virtual void SetInteractable(bool interactable)
        {
            // Override in LocalPlayerController to enable/disable input
        }
        
        public virtual void HighlightPlayer(bool highlight)
        {
            _deck?.SetHighlight(highlight);
            _cardSlot?.SetHighlight(highlight);
            _cardCountUI?.SetHighlight(highlight);
        }
        
        protected virtual async UniTask AnimateCardToDeckAsync(CardView card)
        {
            if (card == null) return;
            
            await _deck.AnimateReturnAsync(card);
            _cardPool.Despawn(card);
        }
        
        protected virtual void ClearActiveCards()
        {
            foreach (var card in _activeCards)
            {
                if (card != null)
                {
                    _cardPool.Despawn(card);
                }
            }
            _activeCards.Clear();
            _cardSlot?.ClearWarCards();
        }
        
        public virtual void Reset()
        {
            Debug.Log($"[PlayerController] Resetting {PlayerName}");
            
            _isMyTurn = false;
            _cardCount = 26;
            ClearActiveCards();
            UpdateCardCountDisplay(_cardCount);
            SetInteractable(false);
            HighlightPlayer(false);
        }
        
        protected virtual void OnDestroy()
        {
            Dispose();
        }
        
        public virtual void Dispose()
        {
            Debug.Log($"[PlayerController] Disposing {PlayerName}");
            
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            ClearActiveCards();
            
            // Release assets
            if (_deck?.Transform?.gameObject != null)
            {
                _assetManager.ReleaseInstance(_deck.Transform.gameObject, true);
            }
            
            if (_cardSlot?.Transform?.gameObject != null)
            {
                _assetManager.ReleaseInstance(_cardSlot.Transform.gameObject, true);
            }
            
            if (_cardCountUI?.gameObject != null)
            {
                _assetManager.ReleaseInstance(_cardCountUI.gameObject, true);
            }
            
            // Clear events
            OnTurnStarted = null;
            OnTurnEnded = null;
            OnCardPlayed = null;
            OnCardCountChanged = null;
        }
    }
}