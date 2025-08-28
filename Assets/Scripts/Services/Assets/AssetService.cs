using UnityEngine;
using System.Collections.Generic;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using CardWar.Configuration;
using Cysharp.Threading.Tasks;

namespace CardWar.Services.Assets
{
    public class AssetService : IAssetService
    {
        private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private Dictionary<string, AudioClip> _audioCache = new Dictionary<string, AudioClip>();
        private Dictionary<string, Object> _objectCache = new Dictionary<string, Object>();
        
        private GameSettings _settings;
        private bool _initialized = false;
        
        public bool AreAssetsLoaded => _initialized;
        
        public void Initialize()
        {
            if (_initialized) return;
            
            _settings = GameSettings.Instance;
            if (_settings == null)
            {
                Debug.LogError("[AssetService] GameSettings not found. Cannot initialize asset service.");
                return;
            }
            
            if (_settings.enableDebugLogs)
                Debug.Log("[AssetService] Initializing with configurable paths...");
            
            if (_settings.enableAssetValidation)
            {
                _settings.ValidateAssetPaths();
            }
            
            PreloadEssentialAssets();
            _initialized = true;
            
            if (_settings.enableDebugLogs)
                Debug.Log("[AssetService] Asset service initialized successfully");
        }
        
        public async UniTask PreloadCardAssets()
        {
            if (!_initialized)
            {
                Initialize();
            }
            
            if (_settings.enableDebugLogs)
                Debug.Log("[AssetService] Preloading card assets...");
            
            // Preload card back
            GetCardBackSprite();
            
            // Could preload some common cards here
            await UniTask.Delay(10);
        }
        
        private void PreloadEssentialAssets()
        {
            // Preload card back sprite
            GetCardBackSprite();
            
            if (_settings.enableDebugLogs)
                Debug.Log("[AssetService] Essential assets preloaded");
        }
        
        public Sprite GetCardSprite(CardData cardData)
        {
            if (cardData == null) 
            {
                if (_settings.enableDebugLogs)
                    Debug.LogWarning("[AssetService] Null card data provided");
                return null;
            }
            
            var cardName = GetCardSpriteName(cardData);
            
            if (_spriteCache.TryGetValue(cardName, out var cachedSprite))
            {
                return cachedSprite;
            }
            
            var spritePath = $"{_settings.cardSpritesPath}/{cardName}";
            var sprite = Resources.Load<Sprite>(spritePath);
            
            if (sprite != null)
            {
                _spriteCache[cardName] = sprite;
                if (_settings.enableDebugLogs)
                    Debug.Log($"[AssetService] Loaded card sprite: {cardName}");
                return sprite;
            }
            
            Debug.LogError($"[AssetService] Missing card sprite: {cardName} at path: {spritePath}");
            return GetPlaceholderSprite();
        }
        
        public Sprite GetCardBackSprite()
        {
            var cacheKey = "card_back";
            
            if (_spriteCache.TryGetValue(cacheKey, out var cachedSprite))
            {
                return cachedSprite;
            }
            
            var spritePath = $"{_settings.cardSpritesPath}/{_settings.cardBackSpriteName}";
            var sprite = Resources.Load<Sprite>(spritePath);
            
            if (sprite != null)
            {
                _spriteCache[cacheKey] = sprite;
                if (_settings.enableDebugLogs)
                    Debug.Log($"[AssetService] Loaded card back sprite from: {spritePath}");
                return sprite;
            }
            
            Debug.LogError($"[AssetService] Missing card back sprite at path: {spritePath}");
            return GetPlaceholderSprite();
        }
        
        public AudioClip GetSoundEffect(SFXType sfxType)
        {
            var assetName = $"sfx_{sfxType.ToString().ToLower()}";
            
            if (_audioCache.TryGetValue(assetName, out var cachedClip))
            {
                return cachedClip;
            }
            
            var audioPath = $"{_settings.sfxPath}/{assetName}";
            var clip = Resources.Load<AudioClip>(audioPath);
            
            if (clip != null)
            {
                _audioCache[assetName] = clip;
                if (_settings.enableDebugLogs)
                    Debug.Log($"[AssetService] Loaded audio clip: {assetName}");
            }
            else if (_settings.enableDebugLogs)
            {
                Debug.LogWarning($"[AssetService] Audio clip not found: {assetName} at path: {audioPath}");
            }
            
            return clip;
        }
        
        public AudioClip GetBackgroundMusic(string musicName)
        {
            var assetName = $"music_{musicName.ToLower()}";
            
            if (_audioCache.TryGetValue(assetName, out var cachedClip))
            {
                return cachedClip;
            }
            
            var musicPath = $"{_settings.musicPath}/{assetName}";
            var clip = Resources.Load<AudioClip>(musicPath);
            
            if (clip != null)
            {
                _audioCache[assetName] = clip;
                if (_settings.enableDebugLogs)
                    Debug.Log($"[AssetService] Loaded music: {assetName}");
            }
            
            return clip;
        }
        
        public Sprite GetUISprite(string spriteName)
        {
            var cacheKey = $"ui_{spriteName}";
            
            if (_spriteCache.TryGetValue(cacheKey, out var cachedSprite))
            {
                return cachedSprite;
            }
            
            var spritePath = $"{_settings.uiSpritesPath}/{spriteName}";
            var sprite = Resources.Load<Sprite>(spritePath);
            
            if (sprite != null)
            {
                _spriteCache[cacheKey] = sprite;
            }
            
            return sprite;
        }
        
        public T GetAsset<T>(string assetName, string customPath = null) where T : Object
        {
            var cacheKey = $"{typeof(T).Name}_{assetName}";
            
            if (_objectCache.TryGetValue(cacheKey, out var cached))
            {
                return cached as T;
            }
            
            string assetPath;
            if (!string.IsNullOrEmpty(customPath))
            {
                assetPath = $"{customPath}/{assetName}";
            }
            else
            {
                // Default path based on asset type
                if (typeof(T) == typeof(Sprite))
                    assetPath = $"{_settings.uiSpritesPath}/{assetName}";
                else if (typeof(T) == typeof(AudioClip))
                    assetPath = $"{_settings.sfxPath}/{assetName}";
                else
                    assetPath = assetName; // Use as-is for other types
            }
            
            var asset = Resources.Load<T>(assetPath);
            if (asset != null)
            {
                _objectCache[cacheKey] = asset;
            }
            
            return asset;
        }
        
        public void ClearCache()
        {
            var cacheCount = _spriteCache.Count + _audioCache.Count + _objectCache.Count;
            
            _spriteCache.Clear();
            _audioCache.Clear();
            _objectCache.Clear();
            
            System.GC.Collect();
            
            if (_settings != null && _settings.enableDebugLogs)
                Debug.Log($"[AssetService] Cleared {cacheCount} cached assets");
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
            
            if (_settings != null)
            {
                return _settings.GetCardSpriteName(rankName, suitName);
            }
            
            return $"{rankName}_{suitName}"; // Fallback format
        }
        
        private Sprite GetPlaceholderSprite()
        {
            return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }
        
        public GameSettings GetGameSettings()
        {
            return _settings;
        }
        
#if UNITY_EDITOR
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugAvailableAssets()
        {
            if (_settings == null)
            {
                Debug.LogError("[AssetService] Cannot debug assets - GameSettings not loaded");
                return;
            }
            
            Debug.Log("[AssetService] Checking available assets with current settings...");
            
            // Check card back
            var cardBack = Resources.Load<Sprite>($"{_settings.cardSpritesPath}/{_settings.cardBackSpriteName}");
            Debug.Log($"[AssetService] Card back ({_settings.cardBackSpriteName}): {(cardBack != null ? "Found" : "Missing")}");
            
            // Check sample cards
            var testCards = new[] { "ace_spades", "king_hearts", "2_clubs" };
            foreach (var testCard in testCards)
            {
                var sprite = Resources.Load<Sprite>($"{_settings.cardSpritesPath}/{testCard}");
                Debug.Log($"[AssetService] Card {testCard}: {(sprite != null ? "Found" : "Missing")}");
            }
            
            // Check audio
            var testAudio = Resources.Load<AudioClip>($"{_settings.sfxPath}/sfx_cardflip");
            Debug.Log($"[AssetService] SFX (cardflip): {(testAudio != null ? "Found" : "Missing")}");
        }
#endif
    }
}