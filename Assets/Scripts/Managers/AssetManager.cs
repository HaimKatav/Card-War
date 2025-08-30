using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using CardWar.Services;
using CardWar.Core;

namespace CardWar.Managers
{
    public class AssetManager : MonoBehaviour, IAssetService
    {
        private readonly Dictionary<string, UnityEngine.Object> _loadedAssets = new Dictionary<string, UnityEngine.Object>();
        private GameSettings _gameSettings;

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            _gameSettings = ServiceLocator.Instance.Get<GameSettings>();
            Debug.Log("[AssetManager] Initialized");
        }

        #endregion

        #region IAssetService Implementation

        public async UniTask<T> LoadAssetAsync<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[AssetManager] Asset path is null or empty");
                return null;
            }

            if (_loadedAssets.TryGetValue(path, out var cachedAsset))
            {
                return cachedAsset as T;
            }

            var request = Resources.LoadAsync<T>(path);
            await request;

            if (request.asset != null)
            {
                _loadedAssets[path] = request.asset;
                Debug.Log($"[AssetManager] Asset loaded: {path}");
                return request.asset as T;
            }

            Debug.LogWarning($"[AssetManager] Failed to load asset: {path}");
            return null;
        }

        public T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[AssetManager] Asset path is null or empty");
                return null;
            }

            if (_loadedAssets.TryGetValue(path, out var cachedAsset))
            {
                return cachedAsset as T;
            }

            var asset = Resources.Load<T>(path);
            if (asset != null)
            {
                _loadedAssets[path] = asset;
                Debug.Log($"[AssetManager] Asset loaded: {path}");
                return asset;
            }

            Debug.LogWarning($"[AssetManager] Failed to load asset: {path}");
            return null;
        }

        public void UnloadAsset(string path)
        {
            if (_loadedAssets.TryGetValue(path, out var asset))
            {
                _loadedAssets.Remove(path);
                Resources.UnloadAsset(asset);
                Debug.Log($"[AssetManager] Asset unloaded: {path}");
            }
        }

        public UniTask PreloadCardAssets()
        {
            throw new NotImplementedException();
        }

        public Sprite GetCardSprite(string cardKey)
        {
            var path = $"{GameSettings.CARD_SPRITE_ASSET_PATH}/{cardKey}";
            return LoadAsset<Sprite>(path);
        }

        public Sprite GetCardBackSprite()
        {
            return LoadAsset<Sprite>(GameSettings.CARD_BACK_SPRITE_ASSET_PATH);
        }

        #endregion

        #region Private Methods

        private bool CanBeUnloaded(UnityEngine.Object asset)
        {
            if (asset == null)
                return false;
            
            var assetType = asset.GetType();
            
            return !typeof(GameObject).IsAssignableFrom(assetType) &&
                   !typeof(Component).IsAssignableFrom(assetType) &&
                   !typeof(AssetBundle).IsAssignableFrom(assetType);
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            foreach (var kvp in _loadedAssets)
            {
                if (CanBeUnloaded(kvp.Value))
                {
                    try
                    {
                        Resources.UnloadAsset(kvp.Value);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[AssetManager] Failed to unload {kvp.Key}: {e.Message}");
                    }
                }
            }
            _loadedAssets.Clear();
        }

        #endregion
    }
}