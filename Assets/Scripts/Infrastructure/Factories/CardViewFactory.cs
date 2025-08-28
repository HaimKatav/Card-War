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
        private IAssetService _assetService;
        private GameSettings _gameSettings;
        
        private GameObject _cardPrefab;
        
        [Inject]
        public void Construct(IAssetService assetService, GameSettings gameSettings)
        {
            _assetService = assetService;
            _gameSettings = gameSettings;
        }
        
        public void Initialize()
        {
            LoadCardPrefab();
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
            else
            {
                Debug.Log($"[CardViewFactory] Card prefab loaded successfully");
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
            frontObject.AddComponent<UnityEngine.UI.Image>();
            
            var backObject = new GameObject("CardBack");
            backObject.transform.SetParent(fallback.transform, false);
            backObject.AddComponent<UnityEngine.UI.Image>();
            
            Debug.LogWarning("[CardViewFactory] Created fallback card prefab");
            return fallback;
        }
        
        public GameObject GetCardPrefab()
        {
            if (_cardPrefab == null)
            {
                LoadCardPrefab();
            }
            return _cardPrefab;
        }
        
        public CardViewController Create()
        {
            return null;
        }
        
        public void Return(CardViewController card)
        {
        }
        
        public void Prewarm(int count)
        {
        }
        
        public void Clear()
        {
        }
    }
}