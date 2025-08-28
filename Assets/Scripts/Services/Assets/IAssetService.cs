using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using Zenject;

namespace CardWar.Services.Assets
{
    public interface IAssetService
    {
        // Initialization
        UniTask PreloadAllAssets();
        UniTask PreloadCardAssets();
        UniTask PreloadUIAssets();
        UniTask PreloadAudioAssets();
        bool AreAssetsLoaded { get; }
        
        // Card Assets
        Sprite GetCardSprite(CardData card);
        Sprite GetCardSprite(CardRank rank, CardSuit suit);
        Sprite GetCardBackSprite();
        
        // UI Assets
        Sprite GetPanelBackground(PanelType type);
        Sprite GetButtonSprite(ButtonType type);
        Sprite GetIcon(IconType type);
        
        // Audio Assets
        AudioClip GetSoundEffect(SFXType type);
        AudioClip GetMusic(MusicType type);
        
        // Generic Asset Access
        T GetAsset<T>(string key) where T : UnityEngine.Object;
        bool TryGetAsset<T>(string key, out T asset) where T : UnityEngine.Object;
        
        // Memory Management
        void ReleaseAsset(string key);
        void ClearCache();
        void OnReturnToMenu();
    }
    
    // Enums for asset types
    public enum PanelType
    {
        Main, Game, Settings, GameOver, War, Loading
    }
    
    public enum ButtonType
    {
        Primary, Secondary, Close, Settings, Play, Pause
    }
    
    public enum IconType
    {
        Settings, Sound, Music, Close, Pause, Play
    }
    
    public enum SFXType
    {
        CardFlip, CardPlace, War, Victory, Defeat, ButtonClick
    }
    
    public enum MusicType
    {
        Menu, Game, Victory
    }

    public class AssetService : IAssetService, IInitializable, IDisposable
    {
        // Three-tier caching system for optimal memory management
        private readonly Dictionary<string, UnityEngine.Object> _permanentCache;  // Never released
        private readonly Dictionary<string, UnityEngine.Object> _gameCache;       // Released on menu
        private readonly Dictionary<string, UnityEngine.Object> _tempCache;       // Released frequently
        
        // Asset path constants - single source of truth
        private const string CARDS_PATH = "Cards/";
        private const string UI_PATH = "UI/";
        private const string AUDIO_PATH = "Audio/";
        private const string CARD_BACK_NAME = "card_back";
        
        private bool _assetsLoaded = false;
        private Sprite _placeholderSprite;
        
        public bool AreAssetsLoaded => _assetsLoaded;
        
        [Inject]
        public AssetService()
        {
            _permanentCache = new Dictionary<string, UnityEngine.Object>();
            _gameCache = new Dictionary<string, UnityEngine.Object>();
            _tempCache = new Dictionary<string, UnityEngine.Object>();
            
            Debug.Log("[AssetService] Initializing centralized asset management");
            CreatePlaceholderSprite();
        }
        
        #region IInitializable
        
        public async void Initialize()
        {
            Debug.Log("[AssetService] Starting initialization");
            await PreloadAllAssets();
            Debug.Log("[AssetService] Initialization complete");
        }
        
        #endregion
        
        #region Asset Loading & Preloading
        
        public async UniTask PreloadAllAssets()
        {
            if (_assetsLoaded)
            {
                Debug.Log("[AssetService] Assets already loaded");
                return;
            }
            
            Debug.Log("[AssetService] Starting comprehensive asset preload");
            
            // Load critical assets first
            await PreloadCardAssets();
            await PreloadUIAssets();
            await PreloadAudioAssets();
            
            _assetsLoaded = true;
            Debug.Log("[AssetService] All asset preloading complete");
        }
        
        public async UniTask PreloadCardAssets()
        {
            Debug.Log("[AssetService] Preloading card assets");
            
            // Load card back first (permanent cache)
            var cardBack = LoadAndCacheAsset<Sprite>(CARD_BACK_NAME, CARDS_PATH, CacheType.Permanent);
            if (cardBack == null)
            {
                Debug.LogWarning($"[AssetService] Card back not found: {CARDS_PATH}{CARD_BACK_NAME}");
            }
            
            // Load all 52 card faces (game cache - released when returning to menu)
            int loadedCount = 0;
            int missingCount = 0;
            
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardRank rank in Enum.GetValues(typeof(CardRank)))
                {
                    string spriteName = GetCardSpriteName(suit, rank);
                    string cardKey = GetCardKey(suit, rank);
                    
                    var sprite = LoadAndCacheAsset<Sprite>(spriteName, CARDS_PATH, CacheType.Game, cardKey);
                    if (sprite != null)
                    {
                        loadedCount++;
                    }
                    else
                    {
                        missingCount++;
                        // Cache placeholder for missing cards
                        _gameCache[cardKey] = CreateCardPlaceholder(suit, rank);
                        Debug.LogWarning($"[AssetService] Missing card: {CARDS_PATH}{spriteName}");
                    }
                }
            }
            
            Debug.Log($"[AssetService] Card assets: {loadedCount} loaded, {missingCount} missing");
            if (missingCount > 0)
            {
                Debug.LogWarning($"[AssetService] Expected format: 2_hearts.png, jack_spades.png, etc.");
            }
            
            await UniTask.Delay(10); // Simulate async behavior
        }
        
        public async UniTask PreloadUIAssets()
        {
            Debug.Log("[AssetService] Preloading UI assets");
            
            // Load UI backgrounds (permanent cache)
            LoadAndCacheAsset<Sprite>("panel_main", UI_PATH + "Panels/", CacheType.Permanent);
            LoadAndCacheAsset<Sprite>("panel_game", UI_PATH + "Panels/", CacheType.Permanent);
            LoadAndCacheAsset<Sprite>("panel_settings", UI_PATH + "Panels/", CacheType.Permanent);
            LoadAndCacheAsset<Sprite>("panel_gameover", UI_PATH + "Panels/", CacheType.Permanent);
            LoadAndCacheAsset<Sprite>("panel_war", UI_PATH + "Panels/", CacheType.Game);
            
            // Load buttons (permanent cache)
            LoadAndCacheAsset<Sprite>("btn_primary", UI_PATH + "Buttons/", CacheType.Permanent);
            LoadAndCacheAsset<Sprite>("btn_secondary", UI_PATH + "Buttons/", CacheType.Permanent);
            LoadAndCacheAsset<Sprite>("btn_close", UI_PATH + "Buttons/", CacheType.Permanent);
            
            // Load icons (permanent cache)
            LoadAndCacheAsset<Sprite>("icon_settings", UI_PATH + "Icons/", CacheType.Permanent);
            LoadAndCacheAsset<Sprite>("icon_sound", UI_PATH + "Icons/", CacheType.Permanent);
            LoadAndCacheAsset<Sprite>("icon_music", UI_PATH + "Icons/", CacheType.Permanent);
            
            await UniTask.Delay(10);
        }
        
        public async UniTask PreloadAudioAssets()
        {
            Debug.Log("[AssetService] Preloading audio assets");
            
            // Load sound effects (game cache)
            LoadAndCacheAsset<AudioClip>("card_flip", AUDIO_PATH + "SFX/", CacheType.Game);
            LoadAndCacheAsset<AudioClip>("card_place", AUDIO_PATH + "SFX/", CacheType.Game);
            LoadAndCacheAsset<AudioClip>("war_sound", AUDIO_PATH + "SFX/", CacheType.Game);
            LoadAndCacheAsset<AudioClip>("victory", AUDIO_PATH + "SFX/", CacheType.Game);
            LoadAndCacheAsset<AudioClip>("defeat", AUDIO_PATH + "SFX/", CacheType.Game);
            LoadAndCacheAsset<AudioClip>("button_click", AUDIO_PATH + "SFX/", CacheType.Permanent);
            
            // Load music (temp cache - stream as needed)
            LoadAndCacheAsset<AudioClip>("menu_music", AUDIO_PATH + "Music/", CacheType.Permanent);
            LoadAndCacheAsset<AudioClip>("game_music", AUDIO_PATH + "Music/", CacheType.Game);
            
            await UniTask.Delay(10);
        }
        
        #endregion
        
        #region Card Assets
        
        public Sprite GetCardSprite(CardData card)
        {
            if (card == null)
            {
                Debug.LogWarning("[AssetService] Null card data provided");
                return _placeholderSprite;
            }
            
            return GetCardSprite(card.Rank, card.Suit);
        }
        
        public Sprite GetCardSprite(CardRank rank, CardSuit suit)
        {
            string cardKey = GetCardKey(suit, rank);
            
            if (TryGetAsset<Sprite>(cardKey, out Sprite sprite))
            {
                return sprite;
            }
            
            Debug.LogWarning($"[AssetService] Card sprite not found: {cardKey}");
            return CreateCardPlaceholder(suit, rank);
        }
        
        public Sprite GetCardBackSprite()
        {
            if (TryGetAsset<Sprite>(CARD_BACK_NAME, out Sprite sprite))
            {
                return sprite;
            }
            
            Debug.LogWarning("[AssetService] Card back sprite not found");
            return _placeholderSprite;
        }
        
        #endregion
        
        #region UI Assets
        
        public Sprite GetPanelBackground(PanelType type)
        {
            string key = type switch
            {
                PanelType.Main => "panel_main",
                PanelType.Game => "panel_game",
                PanelType.Settings => "panel_settings",
                PanelType.GameOver => "panel_gameover",
                PanelType.War => "panel_war",
                PanelType.Loading => "panel_loading",
                _ => "panel_main"
            };
            
            return GetAsset<Sprite>(key) ?? _placeholderSprite;
        }
        
        public Sprite GetButtonSprite(ButtonType type)
        {
            string key = type switch
            {
                ButtonType.Primary => "btn_primary",
                ButtonType.Secondary => "btn_secondary",
                ButtonType.Close => "btn_close",
                ButtonType.Settings => "btn_settings",
                ButtonType.Play => "btn_play",
                ButtonType.Pause => "btn_pause",
                _ => "btn_primary"
            };
            
            return GetAsset<Sprite>(key) ?? _placeholderSprite;
        }
        
        public Sprite GetIcon(IconType type)
        {
            string key = type switch
            {
                IconType.Settings => "icon_settings",
                IconType.Sound => "icon_sound",
                IconType.Music => "icon_music",
                IconType.Close => "icon_close",
                IconType.Pause => "icon_pause",
                IconType.Play => "icon_play",
                _ => "icon_settings"
            };
            
            return GetAsset<Sprite>(key) ?? _placeholderSprite;
        }
        
        #endregion
        
        #region Audio Assets
        
        public AudioClip GetSoundEffect(SFXType type)
        {
            string key = type switch
            {
                SFXType.CardFlip => "card_flip",
                SFXType.CardPlace => "card_place",
                SFXType.War => "war_sound",
                SFXType.Victory => "victory",
                SFXType.Defeat => "defeat",
                SFXType.ButtonClick => "button_click",
                _ => "button_click"
            };
            
            return GetAsset<AudioClip>(key);
        }
        
        public AudioClip GetMusic(MusicType type)
        {
            string key = type switch
            {
                MusicType.Menu => "menu_music",
                MusicType.Game => "game_music",
                MusicType.Victory => "victory_music",
                _ => "menu_music"
            };
            
            return GetAsset<AudioClip>(key);
        }
        
        #endregion
        
        #region Generic Asset Access
        
        public T GetAsset<T>(string key) where T : UnityEngine.Object
        {
            if (TryGetAsset<T>(key, out T asset))
            {
                return asset;
            }
            
            return null;
        }
        
        public bool TryGetAsset<T>(string key, out T asset) where T : UnityEngine.Object
        {
            asset = null;
            
            // Check all cache levels
            if (_permanentCache.TryGetValue(key, out var permAsset) && permAsset is T)
            {
                asset = permAsset as T;
                return true;
            }
            
            if (_gameCache.TryGetValue(key, out var gameAsset) && gameAsset is T)
            {
                asset = gameAsset as T;
                return true;
            }
            
            if (_tempCache.TryGetValue(key, out var tempAsset) && tempAsset is T)
            {
                asset = tempAsset as T;
                return true;
            }
            
            return false;
        }
        
        #endregion
        
        #region Memory Management
        
        public void ReleaseAsset(string key)
        {
            _tempCache.Remove(key);
            Debug.Log($"[AssetService] Released asset: {key}");
        }
        
        public void ClearCache()
        {
            _tempCache.Clear();
            Debug.Log("[AssetService] Temporary cache cleared");
        }
        
        public void OnReturnToMenu()
        {
            Debug.Log("[AssetService] Returning to menu - clearing game assets");
            _gameCache.Clear();
            _tempCache.Clear();
            Resources.UnloadUnusedAssets();
        }
        
        #endregion
        
        #region Helper Methods
        
        private T LoadAndCacheAsset<T>(string assetName, string path, CacheType cacheType, string customKey = null) where T : UnityEngine.Object
        {
            string fullPath = path + assetName;
            string cacheKey = customKey ?? assetName;
            
            T asset = Resources.Load<T>(fullPath);
            
            if (asset != null)
            {
                var targetCache = cacheType switch
                {
                    CacheType.Permanent => _permanentCache,
                    CacheType.Game => _gameCache,
                    CacheType.Temp => _tempCache,
                    _ => _tempCache
                };
                
                targetCache[cacheKey] = asset;
                Debug.Log($"[AssetService] Cached {typeof(T).Name}: {cacheKey} (Type: {cacheType})");
            }
            
            return asset;
        }
        
        private string GetCardKey(CardSuit suit, CardRank rank)
        {
            return $"{rank}_{suit}".ToLower();
        }
        
        private string GetCardSpriteName(CardSuit suit, CardRank rank)
        {
            string rankName = GetRankName(rank);
            string suitName = suit.ToString().ToLower();
            return $"{rankName}_{suitName}";
        }
        
        private string GetRankName(CardRank rank)
        {
            return rank switch
            {
                CardRank.Jack => "jack",
                CardRank.Queen => "queen",
                CardRank.King => "king",
                CardRank.Ace => "ace",
                _ => ((int)rank).ToString()
            };
        }
        
        private void CreatePlaceholderSprite()
        {
            Texture2D texture = new Texture2D(100, 140, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[100 * 140];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.7f, 0.7f, 0.7f, 1f);
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            _placeholderSprite = Sprite.Create(
                texture,
                new Rect(0, 0, 100, 140),
                new Vector2(0.5f, 0.5f),
                100f
            );
            
            _permanentCache["placeholder"] = _placeholderSprite;
        }
        
        private Sprite CreateCardPlaceholder(CardSuit suit, CardRank rank)
        {
            Color suitColor = GetSuitColor(suit);
            
            Texture2D texture = new Texture2D(100, 140, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[100 * 140];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = suitColor;
            }
            
            // Add border
            for (int x = 0; x < 100; x++)
            {
                for (int y = 0; y < 140; y++)
                {
                    if (x < 2 || x >= 98 || y < 2 || y >= 138)
                    {
                        pixels[y * 100 + x] = Color.black;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(
                texture,
                new Rect(0, 0, 100, 140),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }
        
        private Color GetSuitColor(CardSuit suit)
        {
            return suit switch
            {
                CardSuit.Hearts => new Color(0.9f, 0.2f, 0.2f, 1f),
                CardSuit.Diamonds => new Color(0.9f, 0.4f, 0.1f, 1f),
                CardSuit.Clubs => new Color(0.2f, 0.2f, 0.2f, 1f),
                CardSuit.Spades => new Color(0.1f, 0.1f, 0.3f, 1f),
                _ => Color.gray
            };
        }
        
        #endregion
        
        #region IDisposable
        
        public void Dispose()
        {
            Debug.Log("[AssetService] Disposing all cached assets");
            
            // Destroy generated textures to prevent memory leaks
            DestroyGeneratedTextures();
            
            _permanentCache.Clear();
            _gameCache.Clear();
            _tempCache.Clear();
            
            _assetsLoaded = false;
        }
        
        private void DestroyGeneratedTextures()
        {
            if (_placeholderSprite?.texture != null)
            {
                UnityEngine.Object.Destroy(_placeholderSprite.texture);
                UnityEngine.Object.Destroy(_placeholderSprite);
            }
        }
        
        #endregion
    }
    
    // Helper enum for cache management
    public enum CacheType
    {
        Permanent,  // Never cleared (UI basics, card backs)
        Game,       // Cleared when returning to menu (card faces, game sounds)
        Temp        // Cleared frequently (temporary effects)
    }
}