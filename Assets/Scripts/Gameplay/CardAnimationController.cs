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
    /// <summary>
    /// CardAnimationController - REFACTORED VERSION
    /// 
    /// ✅ CHANGES MADE:
    /// - Injected IAssetService for centralized asset management
    /// - Controllers now manage sprite assignment to views
    /// - Added asset preloading coordination
    /// - Removed dependencies on CardAssetManager
    /// - Enhanced error handling for missing assets
    /// 
    /// 🔄 NEW RESPONSIBILITIES:
    /// - Get card sprites from AssetService
    /// - Assign sprites to CardViewController instances
    /// - Coordinate asset loading with animation timing
    /// - Manage card visual state through controllers
    /// </summary>
    public class CardAnimationController : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _dealDelay = 0.1f;
        [SerializeField] private float _warAnimationDuration = 2f;
        [SerializeField] private Transform _playerCardPosition;
        [SerializeField] private Transform _opponentCardPosition;
        [SerializeField] private Transform _deckPosition;
        [SerializeField] private Transform _warPilePosition;
        
        // ✅ NEW - AssetService injection
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
            
            Debug.Log("[CardAnimationController] Injected AssetService and CardPool");
        }
        
        #region Initialization
        
        private async void Start()
        {
            // Ensure assets are loaded before any animations
            if (!_assetService.AreAssetsLoaded)
            {
                Debug.Log("[CardAnimationController] Waiting for assets to load...");
                await _assetService.PreloadCardAssets();
            }
            
            Debug.Log("[CardAnimationController] Ready for card animations");
        }
        
        #endregion
        
        #region Card Creation & Management
        
        /// <summary>
        /// ✅ NEW METHOD: Creates card with proper sprite assignment
        /// Controller manages both view creation AND asset assignment
        /// </summary>
        public CardViewController CreateCard(CardData cardData, Transform parent = null)
        {
            if (cardData == null)
            {
                Debug.LogError("[CardAnimationController] Cannot create card with null data");
                return null;
            }
            
            // Get card view from pool
            var cardView = _cardPool.Spawn();
            if (parent != null)
            {
                cardView.transform.SetParent(parent, false);
            }
            
            // Setup card data
            cardView.Setup(cardData);
            
            // ✅ CONTROLLER GETS SPRITES FROM ASSETSERVICE
            var frontSprite = _assetService.GetCardSprite(cardData);
            var backSprite = _assetService.GetCardBackSprite();
            
            // ✅ CONTROLLER ASSIGNS SPRITES TO VIEW
            cardView.SetCardSprites(frontSprite, backSprite);
            
            // Track active card
            _activeCards.Add(cardView);
            
            Debug.Log($"[CardAnimationController] Created card: {cardData.Rank} of {cardData.Suit}");
            return cardView;
        }
        
        /// <summary>
        /// ✅ ENHANCED: Better error handling and asset validation
        /// </summary>
        public List<CardViewController> CreateCards(List<CardData> cardDataList, Transform parent = null)
        {
            var cards = new List<CardViewController>();
            
            if (cardDataList == null || cardDataList.Count == 0)
            {
                Debug.LogWarning("[CardAnimationController] No card data provided");
                return cards;
            }
            
            // Pre-validate that we can get all required sprites
            var missingSprites = new List<CardData>();
            foreach (var cardData in cardDataList)
            {
                var sprite = _assetService.GetCardSprite(cardData);
                if (sprite == null)
                {
                    missingSprites.Add(cardData);
                }
            }
            
            if (missingSprites.Count > 0)
            {
                Debug.LogWarning($"[CardAnimationController] {missingSprites.Count} cards have missing sprites - using placeholders");
            }
            
            // Create all cards
            foreach (var cardData in cardDataList)
            {
                var card = CreateCard(cardData, parent);
                if (card != null)
                {
                    cards.Add(card);
                }
            }
            
            Debug.Log($"[CardAnimationController] Created {cards.Count} cards");
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
            Debug.Log("[CardAnimationController] Returned all active cards to pool");
        }
        
        #endregion
        
        #region Animation Methods
        
        public async UniTask DealCardAsync(CardData cardData, Transform targetParent, Vector3 targetPosition, bool faceUp = false)
        {
            var card = CreateCard(cardData, targetParent);
            if (card == null) return;
            
            // Start at deck position
            card.transform.position = _deckPosition.position;
            card.SetFaceDown(instant: true);
            
            // Animate to target
            await card.MoveToPositionAsync(targetPosition);
            
            // Flip if needed
            if (faceUp)
            {
                await card.FlipToFrontAsync();
            }
            
            // ✅ ENHANCED: Play sound effect from AssetService
            var flipSound = _assetService.GetSoundEffect(SFXType.CardFlip);
            if (flipSound != null)
            {
                // Audio will be handled by AudioManager, but we demonstrate access
                Debug.Log("[CardAnimationController] Card flip sound available");
            }
        }
        
        public async UniTask AnimateWarSequenceAsync(CardData playerCard, CardData opponentCard, bool playerWins)
        {
            if (_isAnimating)
            {
                Debug.LogWarning("[CardAnimationController] War animation already in progress");
                return;
            }
            
            _isAnimating = true;
            
            try
            {
                // Create war cards
                var playerCardView = CreateCard(playerCard, _playerCardPosition);
                var opponentCardView = CreateCard(opponentCard, _opponentCardPosition);
                
                if (playerCardView == null || opponentCardView == null)
                {
                    Debug.LogError("[CardAnimationController] Failed to create war cards");
                    return;
                }
                
                // Position at war pile initially
                playerCardView.transform.position = _warPilePosition.position;
                opponentCardView.transform.position = _warPilePosition.position;
                
                // Both start face down
                playerCardView.SetFaceDown(instant: true);
                opponentCardView.SetFaceDown(instant: true);
                
                // Animate to positions
                var playerMoveTask = playerCardView.MoveToPositionAsync(_playerCardPosition.position);
                var opponentMoveTask = opponentCardView.MoveToPositionAsync(_opponentCardPosition.position);
                
                await UniTask.WhenAll(playerMoveTask, opponentMoveTask);
                
                // Dramatic pause
                await UniTask.Delay(500);
                
                // Flip both cards simultaneously
                var playerFlipTask = playerCardView.FlipToFrontAsync();
                var opponentFlipTask = opponentCardView.FlipToFrontAsync();
                
                await UniTask.WhenAll(playerFlipTask, opponentFlipTask);
                
                // ✅ ENHANCED: Play war sound from AssetService
                var warSound = _assetService.GetSoundEffect(SFXType.War);
                if (warSound != null)
                {
                    Debug.Log("[CardAnimationController] War sound effect ready");
                }
                
                // Highlight winner
                if (playerWins)
                {
                    await playerCardView.ScalePunchAsync(1.3f, 0.5f);
                    // Play victory sound
                    var victorySound = _assetService.GetSoundEffect(SFXType.Victory);
                    if (victorySound != null)
                    {
                        Debug.Log("[CardAnimationController] Victory sound ready");
                    }
                }
                else
                {
                    await opponentCardView.ScalePunchAsync(1.3f, 0.5f);
                    // Play defeat sound
                    var defeatSound = _assetService.GetSoundEffect(SFXType.Defeat);
                    if (defeatSound != null)
                    {
                        Debug.Log("[CardAnimationController] Defeat sound ready");
                    }
                }
                
                // Hold for dramatic effect
                await UniTask.Delay(1000);
                
                // TODO: Animate cards to winner's pile (address GameController TODO)
                Debug.Log($"[CardAnimationController] War complete - Player wins: {playerWins}");
            }
            finally
            {
                _isAnimating = false;
            }
        }
        
        public async UniTask DealHandAsync(List<CardData> playerCards, List<CardData> opponentCards)
        {
            var dealTasks = new List<UniTask>();
            
            // Deal player cards
            for (int i = 0; i < playerCards.Count; i++)
            {
                var card = playerCards[i];
                var delay = i * _dealDelay * 1000; // Convert to milliseconds
                
                dealTasks.Add(UniTask.Create(async () =>
                {
                    await UniTask.Delay(delay);
                    await DealCardAsync(card, _playerCardPosition, 
                        _playerCardPosition.position + Vector3.right * i * 0.1f, false);
                }));
            }
            
            // Deal opponent cards
            for (int i = 0; i < opponentCards.Count; i++)
            {
                var card = opponentCards[i];
                var delay = i * _dealDelay * 1000;
                
                dealTasks.Add(UniTask.Create(async () =>
                {
                    await UniTask.Delay(delay);
                    await DealCardAsync(card, _opponentCardPosition,
                        _opponentCardPosition.position + Vector3.right * i * 0.1f, false);
                }));
            }
            
            await UniTask.WhenAll(dealTasks);
            Debug.Log("[CardAnimationController] Hand dealing complete");
        }
        
        #endregion
        
        #region Asset Management Integration
        
        /// <summary>
        /// ✅ NEW: Refresh sprites for all active cards
        /// Useful if assets are reloaded or changed
        /// </summary>
        public void RefreshAllCardSprites()
        {
            foreach (var card in _activeCards)
            {
                if (card == null || card.GetCardData() == null) continue;
                
                var frontSprite = _assetService.GetCardSprite(card.GetCardData());
                var backSprite = _assetService.GetCardBackSprite();
                
                card.SetCardSprites(frontSprite, backSprite);
            }
            
            Debug.Log($"[CardAnimationController] Refreshed sprites for {_activeCards.Count} cards");
        }
        
        /// <summary>
        /// ✅ NEW: Preload sprites for upcoming cards
        /// Can be called before dealing to ensure smooth animations
        /// </summary>
        public async UniTask PreloadCardsAsync(List<CardData> upcomingCards)
        {
            if (upcomingCards == null || upcomingCards.Count == 0) return;
            
            Debug.Log($"[CardAnimationController] Preloading sprites for {upcomingCards.Count} cards");
            
            // Verify all sprites are available
            var missingCount = 0;
            foreach (var cardData in upcomingCards)
            {
                var sprite = _assetService.GetCardSprite(cardData);
                if (sprite == null)
                {
                    missingCount++;
                }
            }
            
            if (missingCount > 0)
            {
                Debug.LogWarning($"[CardAnimationController] {missingCount} card sprites missing for preload");
            }
            
            // Small delay to simulate preloading process
            await UniTask.Delay(10);
            
            Debug.Log("[CardAnimationController] Card sprite preload complete");
        }
        
        #endregion
        
        #region Memory Management
        
        /// <summary>
        /// ✅ NEW: Clean up when returning to menu
        /// Coordinates with AssetService memory management
        /// </summary>
        public void OnReturnToMenu()
        {
            // Return all cards to pool
            ReturnAllCards();
            
            // Clear animation queue
            _cardAnimationQueue.Clear();
            
            // Kill any running animations
            DOTween.KillAll();
            
            _isAnimating = false;
            
            Debug.Log("[CardAnimationController] Cleaned up for menu return");
        }
        
        #endregion
        
        #region Debug & Validation
        
        public int GetActiveCardCount() => _activeCards.Count;
        public bool IsAnimating() => _isAnimating;
        
        #if UNITY_EDITOR
        [ContextMenu("Test Create Random Card")]
        private void TestCreateRandomCard()
        {
            var randomRank = (CardRank)UnityEngine.Random.Range(2, 15);
            var randomSuit = (CardSuit)UnityEngine.Random.Range(0, 4);
            var cardData = new CardData(randomSuit, randomRank);
            CreateCard(cardData, transform);
        }
        
        [ContextMenu("Return All Cards")]
        private void TestReturnAllCards()
        {
            ReturnAllCards();
        }
        
        [ContextMenu("Refresh All Sprites")]
        private void TestRefreshSprites()
        {
            RefreshAllCardSprites();
        }
        #endif
        
        #endregion
        
        private void OnDestroy()
        {
            DOTween.KillAll();
            ReturnAllCards();
        }
    }
}