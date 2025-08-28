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
        
        // Flexible asset paths - try multiple locations
        private readonly string[] _cardPaths = {
            "GameplaySprites/Cards",  // New location
            "Art/Cards",              // Current location
            "Cards"                   // Fallback
        };
        
        public bool AreAssetsLoaded => _initialized;
        
        public void Initialize()
        {
            if (_initialized) return;
            
            Debug.Log("[AssetService] Initializing asset service...");
            LoadCardBackSprite();
            _initialized = true;
            Debug.Log("[AssetService] Asset service initialized successfully");
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
            if (cardData == null) 
            {
                Debug.LogWarning("[AssetService] Null card data provided");
                return null;
            }
            
            var cardName = GetCardSpriteName(cardData);
            
            if (_spriteCache.TryGetValue(cardName, out var cachedSprite))
            {
                return cachedSprite;
            }
            
            // Try multiple paths for the card sprite
            Sprite sprite = null;
            foreach (string path in _cardPaths)
            {
                sprite = Resources.Load<Sprite>($"{path}/{cardName}");
                if (sprite != null)
                {
                    Debug.Log($"[AssetService] Loaded card sprite: {cardName} from {path}");
                    break;
                }
            }
            
            if (sprite != null)
            {
                _spriteCache[cardName] = sprite;
                return sprite;
            }
            
            Debug.LogError($"[AssetService] Missing card sprite: {cardName} - checked all paths: {string.Join(", ", _cardPaths)}");
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
                Debug.Log($"[AssetService] Loaded audio clip: {assetName}");
            }
            else
            {
                Debug.LogWarning($"[AssetService] Audio clip not found: {assetName}");
            }
            
            return clip;
        }
        
        public void ClearCache()
        {
            var cacheCount = _spriteCache.Count + _audioCache.Count;
            _spriteCache.Clear();
            _audioCache.Clear();
            _cardBackSprite = null;
            System.GC.Collect();
            Debug.Log($"[AssetService] Cleared {cacheCount} cached assets");
        }
        
        private void LoadCardBackSprite()
        {
            // Try multiple paths for card back
            foreach (string path in _cardPaths)
            {
                _cardBackSprite = Resources.Load<Sprite>($"{path}/card_back");
                if (_cardBackSprite != null)
                {
                    Debug.Log($"[AssetService] Loaded card back sprite from {path}");
                    return;
                }
            }
            
            Debug.LogError("[AssetService] Missing card_back sprite in all paths");
            _cardBackSprite = GetPlaceholderSprite();
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
        
        // Debug method to check what assets are available
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugAvailableAssets()
        {
            Debug.Log("[AssetService] Checking available card assets...");
            
            foreach (string path in _cardPaths)
            {
                var cardBack = Resources.Load<Sprite>($"{path}/card_back");
                Debug.Log($"[AssetService] Path {path}/card_back: {(cardBack != null ? "✅ Found" : "❌ Missing")}");
                
                // Check a few sample cards
                var testCards = new[] { "ace_spades", "king_hearts", "2_clubs" };
                foreach (var testCard in testCards)
                {
                    var sprite = Resources.Load<Sprite>($"{path}/{testCard}");
                    Debug.Log($"[AssetService] Path {path}/{testCard}: {(sprite != null ? "✅ Found" : "❌ Missing")}");
                }
            }
        }
    }
}