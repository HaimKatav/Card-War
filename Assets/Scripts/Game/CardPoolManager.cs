using System.Collections.Generic;
using UnityEngine;
using CardWar.Core;
using CardWar.Game.UI;
using CardWar.Game.Logic;
using CardWar.Services;
using Cysharp.Threading.Tasks;

namespace CardWar.Game.UI
{
    public class CardPoolManager
    {
        private readonly Transform _poolContainer;
        private readonly IAssetService _assetService;
        private GenericPool<CardView> _cardPool;
        private readonly HashSet<CardView> _activeCards = new();
        private readonly Dictionary<CardView, CardData> _cardDataMap = new();
        
        public int ActiveCardCount => _activeCards.Count;
        public int PoolAvailableCount => _cardPool?.ItemsInPool ?? 0;
        
        public CardPoolManager(Transform poolContainer, IAssetService assetService)
        {
            _poolContainer = poolContainer;
            _assetService = assetService;
        }
        
        #region Pool Initialization
        
        public async UniTask InitializePool(int initialSize, int maxSize, bool prewarm)
        {
            if (_cardPool != null)
            {
                Debug.LogWarning("[CardPoolManager] Pool already initialized");
                return;
            }
            
            var prefab = await _assetService.LoadAssetAsync<CardView>(GameSettings.CARD_PREFAB_ASSET_PATH);
            
            if (prefab == null)
            {
                Debug.LogError("[CardPoolManager] Failed to load card prefab");
                return;
            }
            
            _cardPool = new GenericPool<CardView>(
                prefab,
                _poolContainer,
                initialSize
            );
            
            if (prewarm)
            {
                PrewarmPool(initialSize);
            }
            
            Debug.Log($"[CardPoolManager] Pool initialized - Initial: {initialSize}, Max: {maxSize}");
        }
        
        private void PrewarmPool(int count)
        {
            var tempCards = new List<CardView>();
            
            for (var i = 0; i < count; i++)
            {
                var card = _cardPool.Get();
                if (card != null)
                {
                    tempCards.Add(card);
                }
            }
            
            foreach (var card in tempCards)
            {
                _cardPool.Return(card);
            }
            
            Debug.Log($"[CardPoolManager] Pool prewarmed with {count} cards");
        }
        
        #endregion
        
        #region Card Spawning
        
        public CardView SpawnCard(CardData cardData, Vector3 position, bool loadSprite = true)
        {
            if (_cardPool == null)
            {
                Debug.LogError("[CardPoolManager] Pool not initialized");
                return null;
            }
            
            if (cardData == null)
            {
                Debug.LogError("[CardPoolManager] CardData is null");
                return null;
            }
            
            var cardView = _cardPool.Get();
            if (cardView == null)
            {
                Debug.LogError("[CardPoolManager] Failed to get card from pool");
                return null;
            }
            
            cardView.transform.position = position;
            cardView.transform.rotation = Quaternion.identity;
            cardView.transform.localScale = Vector3.one;
            
            _activeCards.Add(cardView);
            _cardDataMap[cardView] = cardData;
            
            cardView.SetCardData(cardData);
            
            if (loadSprite)
            {
                LoadCardSpriteAsync(cardView, cardData).Forget();
            }
            
            Debug.Log($"[CardPoolManager] Spawned card: {cardData.CardKey} at {position}");
            
            return cardView;
        }
        
        public CardView SpawnCard(CardData cardData, Transform spawnPoint, bool loadSprite = true)
        {
            return spawnPoint != null 
                ? SpawnCard(cardData, spawnPoint.position, loadSprite) 
                : SpawnCard(cardData, Vector3.zero, loadSprite);
        }
        
        public List<CardView> SpawnMultipleCards(List<CardData> cardsData, Vector3 basePosition, Vector3 offset, bool loadSprites = true)
        {
            var spawnedCards = new List<CardView>();
            
            for (var i = 0; i < cardsData.Count; i++)
            {
                var position = basePosition + (offset * i);
                var card = SpawnCard(cardsData[i], position, loadSprites);
                
                if (card != null)
                {
                    spawnedCards.Add(card);
                }
            }
            
            return spawnedCards;
        }
        
        #endregion
        
        #region Card Return
        
        public void ReturnCard(CardView cardView)
        {
            if (cardView == null) return;
            
            if (!_activeCards.Contains(cardView))
            {
                Debug.LogWarning($"[CardPoolManager] Attempting to return card that wasn't tracked as active");
                return;
            }
            
            _activeCards.Remove(cardView);
            _cardDataMap.Remove(cardView);
            
            cardView.ResetCard();
            _cardPool?.Return(cardView);
            
            Debug.Log($"[CardPoolManager] Returned card to pool. Active count: {_activeCards.Count}");
        }
        
        public void ReturnMultipleCards(List<CardView> cards)
        {
            if (cards == null || cards.Count == 0) return;
            
            foreach (var card in cards)
            {
                ReturnCard(card);
            }
            
            Debug.Log($"[CardPoolManager] Returned {cards.Count} cards to pool");
        }
        
        public void ReturnAllActiveCards()
        {
            var cardsToReturn = new List<CardView>(_activeCards);
            
            foreach (var card in cardsToReturn)
            {
                ReturnCard(card);
            }
            
            Debug.Log($"[CardPoolManager] All active cards returned to pool");
        }
        
        #endregion
        
        #region Sprite Loading
        
        private async UniTaskVoid LoadCardSpriteAsync(CardView cardView, CardData cardData)
        {
            if (cardView == null || cardData == null) return;
            
            var path = $"{GameSettings.CARD_SPRITE_ASSET_PATH}/{cardData.CardKey}";
            
            var sprite = await _assetService.LoadAssetAsync<Sprite>(path);
            
            if (sprite != null && cardView != null && _activeCards.Contains(cardView))
            {
                cardView.SetCardSprite(sprite);
                Debug.Log($"[CardPoolManager] Loaded sprite for card: {cardData.CardKey}");
            }
            else if (sprite == null)
            {
                Debug.LogWarning($"[CardPoolManager] Failed to load sprite: {path}");
            }
        }
        
        public async UniTask LoadCardBackSprite(CardView cardView)
        {
            if (cardView == null) return;
            
            var backSprite = await _assetService.LoadAssetAsync<Sprite>(GameSettings.CARD_BACK_SPRITE_ASSET_PATH);
            
            if (backSprite != null && cardView != null)
            {
                cardView.SetBackSprite(backSprite);
            }
        }
        
        #endregion
        
        #region Pool State Queries
        
        public bool IsCardActive(CardView card)
        {
            return card != null && _activeCards.Contains(card);
        }
        
        public CardData GetCardData(CardView card)
        {
            return card != null && _cardDataMap.TryGetValue(card, out var data) ? data : null;
        }
        
        public List<CardView> GetAllActiveCards()
        {
            return new List<CardView>(_activeCards);
        }
        
        public bool HasAvailableCards()
        {
            return _cardPool != null && _cardPool.ItemsInPool > 0;
        }
        
        public void ValidatePoolHealth()
        {
            if (_cardPool == null)
            {
                Debug.LogError("[CardPoolManager] Pool is null - needs initialization");
                return;
            }
            
            Debug.Log($"[CardPoolManager] Pool Health - Active: {_activeCards.Count}, Available: {_cardPool.ItemsInPool}");
            
            foreach (var card in _activeCards)
            {
                if (card == null)
                {
                    Debug.LogError("[CardPoolManager] Found null reference in active cards!");
                }
            }
        }
        
        #endregion
        
        #region Cleanup
        
        public void Cleanup()
        {
            ReturnAllActiveCards();
            
            _activeCards.Clear();
            _cardDataMap.Clear();
            
            _cardPool?.Dispose();
            _cardPool = null;
            
            Debug.Log("[CardPoolManager] Cleanup complete");
        }
        
        #endregion
    }
}