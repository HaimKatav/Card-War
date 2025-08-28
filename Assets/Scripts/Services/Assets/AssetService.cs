using UnityEngine;
using System.Collections.Generic;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using CardWar.Configuration;
using Cysharp.Threading.Tasks;
using Zenject;

namespace CardWar.Services.Assets
{
    public class AssetService : IAssetService, IInitializable
    {
        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, AudioClip> _audioCache = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, Object> _objectCache = new Dictionary<string, Object>();
        
        private GameSettings _settings;
        private bool _initialized = false;
        
        public bool AreAssetsLoaded => _initialized;
        
        [Inject]
        public void Construct(GameSettings gameSettings)
        {
            _settings = gameSettings;
        }
        
        public void Initialize()
        {
            if (_initialized) return;
            
            if (_settings == null)
            {
                Debug.LogError("[AssetService] GameSettings not found. Cannot initialize asset service.");
                return;
            }
            
            PreloadCriticalAssets();
            _initialized = true;
            Debug.Log("[AssetService] Initialized successfully");
        }
        
        private void PreloadCriticalAssets()
        {
            var cardBackPath = $"{_settings.cardSpritesPath}/{_settings.cardBackSpriteName}";
            var cardBack = Resources.Load<Sprite>(cardBackPath);
            
            if (cardBack != null)
            {
                _spriteCache[_settings.cardBackSpriteName] = cardBack;
                Debug.Log($"[AssetService] Preloaded card back sprite");
            }
            else
            {
                Debug.LogWarning($"[AssetService] Card back sprite not found at: {cardBackPath}");
            }
        }
        
        public Sprite GetCardSprite(CardData card)
        {
            if (card == null) return null;
            
            var spriteName = _settings.GetCardSpriteName(card.Rank.ToString(), card.Suit.ToString());
            return GetCardSprite(spriteName);
        }
        
        public Sprite GetCardSprite(string cardName)
        {
            if (string.IsNullOrEmpty(cardName)) return null;
            
            if (_spriteCache.TryGetValue(cardName, out var cachedSprite))
                return cachedSprite;
            
            var paths = new[]
            {
                $"{_settings.cardSpritesPath}/{cardName}",
                $"GameplaySprites/Cards/{cardName}",
                $"Art/Cards/{cardName}",
                $"Cards/{cardName}"
            };
            
            foreach (var path in paths)
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                {
                    _spriteCache[cardName] = sprite;
                    return sprite;
                }
            }
            
            Debug.LogWarning($"[AssetService] Card sprite not found: {cardName}");
            return null;
        }
        
        public Sprite GetCardBackSprite()
        {
            return GetCardSprite(_settings.cardBackSpriteName);
        }
        
        public Sprite GetUISprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            
            if (_spriteCache.TryGetValue(spriteName, out var cachedSprite))
                return cachedSprite;
            
            var paths = new[]
            {
                $"{_settings.uiSpritesPath}/{spriteName}",
                $"GameplaySprites/UI/{spriteName}",
                $"UI/{spriteName}"
            };
            
            foreach (var path in paths)
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                {
                    _spriteCache[spriteName] = sprite;
                    return sprite;
                }
            }
            
            Debug.LogWarning($"[AssetService] UI sprite not found: {spriteName}");
            return null;
        }
        
        public GameObject LoadPrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return null;
            
            if (_prefabCache.TryGetValue(prefabName, out var cachedPrefab))
                return cachedPrefab;
            
            var paths = new[]
            {
                $"{_settings.prefabsPath}/{prefabName}",
                $"Prefabs/{prefabName}",
                $"Prefabs/Cards/{prefabName}",
                prefabName
            };
            
            foreach (var path in paths)
            {
                var prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    _prefabCache[prefabName] = prefab;
                    Debug.Log($"[AssetService] Loaded prefab from: {path}");
                    return prefab;
                }
            }
            
            Debug.LogError($"[AssetService] Prefab not found: {prefabName}");
            return null;
        }
        
        public AudioClip GetSoundEffect(string soundName)
        {
            if (string.IsNullOrEmpty(soundName)) return null;
            
            if (_audioCache.TryGetValue(soundName, out var cachedClip))
                return cachedClip;
            
            var paths = new[]
            {
                $"{_settings.sfxPath}/{soundName}",
                $"{_settings.cardSoundPath}/{soundName}",
                $"Audio/SFX/{soundName}"
            };
            
            foreach (var path in paths)
            {
                var clip = Resources.Load<AudioClip>(path);
                if (clip != null)
                {
                    _audioCache[soundName] = clip;
                    return clip;
                }
            }
            
            Debug.LogWarning($"[AssetService] Sound effect not found: {soundName}");
            return null;
        }
        
        public AudioClip GetMusic(string musicName)
        {
            if (string.IsNullOrEmpty(musicName)) return null;
            
            if (_audioCache.TryGetValue(musicName, out var cachedClip))
                return cachedClip;
            
            var path = $"{_settings.musicPath}/{musicName}";
            var clip = Resources.Load<AudioClip>(path);
            
            if (clip != null)
            {
                _audioCache[musicName] = clip;
            }
            else
            {
                Debug.LogWarning($"[AssetService] Music not found: {musicName}");
            }
            
            return clip;
        }
        
        public T LoadAsset<T>(string assetPath) where T : Object
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            
            if (_objectCache.TryGetValue(assetPath, out var cachedObject))
                return cachedObject as T;
            
            var asset = Resources.Load<T>(assetPath);
            if (asset != null)
            {
                _objectCache[assetPath] = asset;
            }
            else
            {
                Debug.LogWarning($"[AssetService] Asset not found: {assetPath}");
            }
            
            return asset;
        }
        
        public async UniTask<T> LoadAssetAsync<T>(string assetPath) where T : Object
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            
            if (_objectCache.TryGetValue(assetPath, out var cachedObject))
                return cachedObject as T;
            
            var request = Resources.LoadAsync<T>(assetPath);
            await request;
            
            var asset = request.asset as T;
            if (asset != null)
            {
                _objectCache[assetPath] = asset;
            }
            else
            {
                Debug.LogWarning($"[AssetService] Asset not found (async): {assetPath}");
            }
            
            return asset;
        }
        
        public void PreloadCardSprites(List<CardData> cards)
        {
            foreach (var card in cards)
            {
                GetCardSprite(card);
            }
        }
        
        public void UnloadAsset(string assetPath)
        {
            if (_spriteCache.ContainsKey(assetPath))
                _spriteCache.Remove(assetPath);
            
            if (_audioCache.ContainsKey(assetPath))
                _audioCache.Remove(assetPath);
            
            if (_prefabCache.ContainsKey(assetPath))
                _prefabCache.Remove(assetPath);
            
            if (_objectCache.ContainsKey(assetPath))
                _objectCache.Remove(assetPath);
        }
        
        public void ClearCache()
        {
            _spriteCache.Clear();
            _audioCache.Clear();
            _prefabCache.Clear();
            _objectCache.Clear();
            Resources.UnloadUnusedAssets();
            
            Debug.Log("[AssetService] Cache cleared");
        }
    }
}