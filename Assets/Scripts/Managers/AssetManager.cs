using System;
using System.Collections.Generic;
using CardWar.Core;
using UnityEngine;
using CardWar.Services;
using Cysharp.Threading.Tasks;
using Zenject;

namespace CardWar.Managers
{
    public class AssetManager : MonoBehaviour, IAssetService, IDisposable
    {
        private IDIService _diService;
        private Dictionary<string, UnityEngine.Object> _loadedAssets;
        private Dictionary<string, Sprite> _cardSprites;
        private Sprite _cardBackSprite;

        [Inject]
        public void Initialize(IDIService diService)
        {
            _diService = diService;
            _diService.RegisterService<IAssetService>(this);

            _loadedAssets = new Dictionary<string, UnityEngine.Object>();
            _cardSprites = new Dictionary<string, Sprite>();
        }

        public async UniTask<T> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            if (_loadedAssets.TryGetValue(assetPath, out var cachedAsset))
            {
                return cachedAsset as T;
            }

            await UniTask.Delay(100);
            
            T asset = Resources.Load<T>(assetPath);
            if (asset != null)
            {
                _loadedAssets[assetPath] = asset;
            }
            
            return asset;
        }

        public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            if (_loadedAssets.TryGetValue(assetPath, out var cachedAsset))
            {
                return cachedAsset as T;
            }

            T asset = Resources.Load<T>(assetPath);
            if (asset != null)
            {
                _loadedAssets[assetPath] = asset;
            }
            
            return asset;
        }

        public void UnloadAsset(string assetPath)
        {
            if (_loadedAssets.Remove(assetPath))
            {
                Debug.Log($"[AssetManager] Asset unloaded: {assetPath}");
            }
        }

        public async UniTask PreloadCardAssets()
        {
            await LoadCardSprites();
            Debug.Log($"[AssetManager] Preloaded {_cardSprites.Count} card sprites");
        }

        private async UniTask LoadCardSprites()
        {
            _cardBackSprite = await LoadAssetAsync<Sprite>(GameSettings.CARD_BACK_SPRITE_ASSET_PATH);
            
            string[] suits = { "hearts", "diamonds", "clubs", "spades" };
            string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "jack", "queen", "king", "ace" };
            
            foreach (var suit in suits)
            {
                foreach (var rank in ranks)
                {
                    string cardKey = $"{rank}_{suit}";
                    string path = $"{GameSettings.CARD_SPRITE_ASSET_PATH}{cardKey}";
                    Sprite sprite = await LoadAssetAsync<Sprite>(path);
                    
                    if (sprite != null)
                    {
                        _cardSprites[cardKey] = sprite;
                    }
                }
            }
        }

        public Sprite GetCardSprite(string cardKey)
        {
            _cardSprites.TryGetValue(cardKey, out Sprite sprite);
            return sprite;
        }

        public Sprite GetCardBackSprite()
        {
            return _cardBackSprite;
        }

        public void Dispose()
        {
            _loadedAssets.Clear();
            _cardSprites.Clear();
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}