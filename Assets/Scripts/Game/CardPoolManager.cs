using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Core;
using CardWar.Game.UI;
using CardWar.Game.Logic;
using CardWar.Services;
using Cysharp.Threading.Tasks;

namespace CardWar.Game.Helpers
{
    public class CardPoolManager
    {
        private readonly Transform _poolContainer;
        private readonly IAssetService _assetService;
        private GenericPool<CardView> _cardPool;
        private readonly List<CardView> _activeCards = new();
        
        public CardPoolManager(Transform poolContainer, IAssetService assetService)
        {
            _poolContainer = poolContainer;
            _assetService = assetService;
        }
        
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
                Debug.LogWarning("[CardPoolManager] Failed to load card pool");

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
            
            Debug.Log($"[CardPoolManager] Pool initialized - Size: {initialSize}/{maxSize}");
        }
        
        public CardView SpawnCard(CardData cardData, Vector3 position, bool loadSprites = true)
        {
            if (_cardPool == null)
            {
                Debug.LogError("[CardPoolManager] Pool not initialized");
                return null;
            }
            
            var card = _cardPool.Get();
            card.transform.position = position;
            card.ResetCard();
            
            if (cardData != null)
            {
                card.SetCardData(cardData);
                if (loadSprites)
                {
                    LoadCardSprites(card, cardData);
                }
            }
            else
            {
                LoadBackSprite(card);
            }
            
            card.SetFaceUp(false);
            _activeCards.Add(card);
            
            return card;
        }
        
        public void ReturnCard(CardView card)
        {
            if (card != null && _cardPool != null)
            {
                _activeCards.Remove(card);
                _cardPool.Return(card);
            }
        }
        
        public void ReturnAllActiveCards()
        {
            foreach (var card in _activeCards)
            {
                if (card != null && _cardPool != null)
                {
                    _cardPool.Return(card);
                }
            }
            _activeCards.Clear();
        }
        
        public void Cleanup()
        {
            ReturnAllActiveCards();
            _cardPool?.ReturnAll();
        }
        
        private CardView CreateCardPrefab()
        {
            var cardGO = new GameObject("CardPrefab");
            cardGO.transform.SetParent(_poolContainer);
            
            var cardView = cardGO.AddComponent<CardView>();
            
            // Add required components
            cardGO.AddComponent<UnityEngine.CanvasGroup>();
            
            // Create front image
            var frontGO = new GameObject("Front");
            frontGO.transform.SetParent(cardGO.transform);
            frontGO.AddComponent<UnityEngine.UI.Image>();
            
            // Create back image
            var backGO = new GameObject("Back");
            backGO.transform.SetParent(cardGO.transform);
            backGO.AddComponent<UnityEngine.UI.Image>();
            
            // Deactivate the prefab
            cardGO.SetActive(false);
            
            return cardView;
        }
        
        private void PrewarmPool(int count)
        {
            var cards = new List<CardView>();
            for (var i = 0; i < count; i++)
            {
                cards.Add(_cardPool.Get());
            }
            
            foreach (var card in cards)
            {
                _cardPool.Return(card);
            }
        }
        
        private void LoadCardSprites(CardView card, CardData cardData)
        {
            if (_assetService == null || cardData == null) return;
            
            var frontSprite = _assetService.GetCardSprite(cardData.CardKey);
            if (frontSprite != null)
            {
                card.SetCardSprite(frontSprite);
            }
            
            LoadBackSprite(card);
        }
        
        private void LoadBackSprite(CardView card)
        {
            if (_assetService == null) return;
            
            var backSprite = _assetService.GetCardBackSprite();
            if (backSprite != null)
            {
                card.SetBackSprite(backSprite);
            }
        }
    }
}