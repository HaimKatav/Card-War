using UnityEngine;
using System.Collections.Generic;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using Cysharp.Threading.Tasks;

namespace CardWar.Services.Assets
{
    public class AssetService : IAssetService
    {
        private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private Dictionary<string, AudioClip> _audioCache = new Dictionary<string, AudioClip>();
        
        private Sprite _cardBackSprite;
        private bool _initialized = false;
        
        public bool AreAssetsLoaded => _initialized;
        
        public void Initialize()
        {
            if (_initialized) return;
            
            LoadCardBackSprite();
            _initialized = true;
        }
        
        public async UniTask PreloadCardAssets()
        {
            if (!_initialized)
            {
                Initialize();
            }
            
            await UniTask.Delay(10);
        }
        
        public Sprite GetCardSprite(CardData cardData)
        {
            if (cardData == null) return null;
            
            var cardName = GetCardSpriteName(cardData);
            
            if (_spriteCache.TryGetValue(cardName, out var cachedSprite))
            {
                return cachedSprite;
            }
            
            var sprite = Resources.Load<Sprite>($"Art/Cards/{cardName}");
            if (sprite != null)
            {
                _spriteCache[cardName] = sprite;
                return sprite;
            }
            
            Debug.LogError($"Missing card sprite: {cardName}");
            return GetPlaceholderSprite();
        }
        
        public Sprite GetCardBackSprite()
        {
            if (_cardBackSprite == null)
            {
                LoadCardBackSprite();
            }
            
            return _cardBackSprite;
        }
        
        public AudioClip GetSoundEffect(SFXType sfxType)
        {
            var assetName = $"sfx_{sfxType.ToString().ToLower()}";
            
            if (_audioCache.TryGetValue(assetName, out var cachedClip))
            {
                return cachedClip;
            }
            
            var clip = Resources.Load<AudioClip>($"Audio/SFX/{assetName}");
            if (clip != null)
            {
                _audioCache[assetName] = clip;
            }
            
            return clip;
        }
        
        public void ClearCache()
        {
            _spriteCache.Clear();
            _audioCache.Clear();
            _cardBackSprite = null;
            System.GC.Collect();
        }
        
        private void LoadCardBackSprite()
        {
            _cardBackSprite = Resources.Load<Sprite>("Art/Cards/card_back");
            if (_cardBackSprite == null)
            {
                Debug.LogError("Missing card_back sprite");
                _cardBackSprite = GetPlaceholderSprite();
            }
        }
        
        private string GetCardSpriteName(CardData cardData)
        {
            var rankName = cardData.Rank switch
            {
                CardRank.Jack => "jack",
                CardRank.Queen => "queen", 
                CardRank.King => "king",
                CardRank.Ace => "ace",
                _ => ((int)cardData.Rank).ToString()
            };
            
            var suitName = cardData.Suit.ToString().ToLower();
            return $"{rankName}_{suitName}";
        }
        
        private Sprite GetPlaceholderSprite()
        {
            return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }
    }
}