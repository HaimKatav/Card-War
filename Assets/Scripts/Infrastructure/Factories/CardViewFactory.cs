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
        
        private GameObject _cardPrefab;
        
        [Inject]
        public void Construct(IAssetService assetService)
        {
            _assetService = assetService;
        }
        
        public void Initialize()
        {
            LoadCardPrefab();
        }
        
        private void LoadCardPrefab()
        {
            _cardPrefab = _assetService.LoadPrefab(GameSettings.CARD_PREFAB_NAME);
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