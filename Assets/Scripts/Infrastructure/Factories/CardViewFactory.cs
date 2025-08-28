using UnityEngine;
using System.Collections.Generic;
using CardWar.UI.Cards;
using CardWar.Services.Assets;
using CardWar.Configuration;
using Zenject;

namespace CardWar.Infrastructure.Factories
{
    public class CardViewFactory : ICardViewFactory, IInitializable
    {
        private DiContainer _container;
        private IAssetService _assetService;
        private GameSettings _gameSettings;
        private Transform _poolContainer;
        
        private GameObject _cardPrefab;
        private readonly Stack<CardViewController> _availableCards = new Stack<CardViewController>();
        private readonly HashSet<CardViewController> _activeCards = new HashSet<CardViewController>();
        
        [Inject]
        public void Construct(
            DiContainer container,
            IAssetService assetService, 
            GameSettings gameSettings,
            [Inject(Id = "CardPoolContainer")] Transform poolContainer)
        {
            _container = container;
            _assetService = assetService;
            _gameSettings = gameSettings;
            _poolContainer = poolContainer;
        }
        
        public void Initialize()
        {
            LoadCardPrefab();
            Prewarm(_gameSettings.cardPoolInitialSize);
        }
        
        private void LoadCardPrefab()
        {
            var prefabPath = _gameSettings.cardPrefabPath;
            
            if (prefabPath.StartsWith("Prefabs/"))
            {
                prefabPath = prefabPath.Substring("Prefabs/".Length);
            }
            
            _cardPrefab = _assetService.LoadPrefab(prefabPath);
            
            if (_cardPrefab == null)
            {
                Debug.LogError($"[CardViewFactory] Failed to load card prefab from path: {prefabPath}");
                _cardPrefab = CreateFallbackPrefab();
            }
        }
        
        private GameObject CreateFallbackPrefab()
        {
            var fallback = new GameObject("CardPrefab");
            var rectTransform = fallback.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 180);
            
            var canvasGroup = fallback.AddComponent<CanvasGroup>();
            var cardView = fallback.AddComponent<CardViewController>();
            
            var frontObject = new GameObject("CardFront");
            frontObject.transform.SetParent(fallback.transform, false);
            var frontImage = frontObject.AddComponent<UnityEngine.UI.Image>();
            
            var backObject = new GameObject("CardBack");
            backObject.transform.SetParent(fallback.transform, false);
            var backImage = backObject.AddComponent<UnityEngine.UI.Image>();
            
            Debug.LogWarning("[CardViewFactory] Created fallback card prefab");
            return fallback;
        }
        
        public CardViewController Create()
        {
            CardViewController card = null;
            
            if (_availableCards.Count > 0)
            {
                card = _availableCards.Pop();
                card.gameObject.SetActive(true);
            }
            else
            {
                card = CreateNewCard();
            }
            
            _activeCards.Add(card);
            return card;
        }
        
        private CardViewController CreateNewCard()
        {
            var instance = _container.InstantiatePrefab(_cardPrefab, _poolContainer);
            var cardView = instance.GetComponent<CardViewController>();
            
            if (cardView == null)
            {
                Debug.LogError("[CardViewFactory] CardViewController component not found on prefab!");
                cardView = instance.AddComponent<CardViewController>();
            }
            
            _container.InjectGameObject(instance);
            
            return cardView;
        }
        
        public void Return(CardViewController card)
        {
            if (card == null || !_activeCards.Contains(card))
                return;
            
            _activeCards.Remove(card);
            
            card.OnDespawned();
            card.gameObject.SetActive(false);
            card.transform.SetParent(_poolContainer, false);
            
            if (_availableCards.Count < _gameSettings.cardPoolMaxSize)
            {
                _availableCards.Push(card);
            }
            else
            {
                Object.Destroy(card.gameObject);
            }
        }
        
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var card = CreateNewCard();
                card.gameObject.SetActive(false);
                _availableCards.Push(card);
            }
            
            Debug.Log($"[CardViewFactory] Prewarmed {count} cards");
        }
        
        public void Clear()
        {
            foreach (var card in _activeCards)
            {
                if (card != null)
                    Object.Destroy(card.gameObject);
            }
            _activeCards.Clear();
            
            while (_availableCards.Count > 0)
            {
                var card = _availableCards.Pop();
                if (card != null)
                    Object.Destroy(card.gameObject);
            }
            
            Debug.Log("[CardViewFactory] Cleared all cards");
        }
    }
}