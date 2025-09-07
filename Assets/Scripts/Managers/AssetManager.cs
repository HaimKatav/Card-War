using System;
using System.Collections.Generic;
using CardWar.Core;
using UnityEngine;
using CardWar.Services;
using Cysharp.Threading.Tasks;

namespace CardWar.Managers
{
    public class AssetManager : MonoBehaviour, IAssetService
    {
        private readonly Dictionary<string, UnityEngine.Object> _loadedAssets = new();
        private readonly HashSet<string> _gameObjectPaths = new();
        
        #region Initialization
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            ServiceLocator.Instance.Register<IAssetService>(this);
            Debug.Log("[AssetManager] Initialized");
        }
        
        #endregion
        
        #region Asset Loading
        
        public async UniTask<T> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[AssetManager] Asset path is null or empty");
                return null;
            }
            
            if (_loadedAssets.ContainsKey(assetPath))
            {
                return _loadedAssets[assetPath] as T;
            }
            
            try
            {
                var request = Resources.LoadAsync<T>(assetPath);
                await request.ToUniTask();
                
                if (request.asset != null)
                {
                    _loadedAssets[assetPath] = request.asset;
                    
                    if (request.asset is GameObject || request.asset is Component)
                    {
                        _gameObjectPaths.Add(assetPath);
                    }
                    
                    Debug.Log($"[AssetManager] Asset loaded: {assetPath}");
                    return request.asset as T;
                }
                
                Debug.LogError($"[AssetManager] Failed to load asset: {assetPath}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetManager] Exception loading asset {assetPath}: {e.Message}");
                return null;
            }
        }
        
        public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[AssetManager] Asset path is null or empty");
                return null;
            }
            
            if (_loadedAssets.ContainsKey(assetPath))
            {
                return _loadedAssets[assetPath] as T;
            }
            
            try
            {
                var asset = Resources.Load<T>(assetPath);
                
                if (asset != null)
                {
                    _loadedAssets[assetPath] = asset;
                    
                    if (asset is GameObject || asset is Component)
                    {
                        _gameObjectPaths.Add(assetPath);
                    }
                    
                    Debug.Log($"[AssetManager] Asset loaded: {assetPath}");
                    return asset;
                }
                
                Debug.LogError($"[AssetManager] Failed to load asset: {assetPath}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetManager] Exception loading asset {assetPath}: {e.Message}");
                return null;
            }
        }
        
        #endregion
        
        #region Sprite Loading
        
        public async UniTask<Sprite> GetCardSpriteAsync(string cardKey)
        {
            if (string.IsNullOrEmpty(cardKey))
            {
                Debug.LogError("[AssetManager] Card key is null or empty");
                return null;
            }

            var spritePath = $"{GameSettings.CARD_SPRITE_ASSET_PATH}/{cardKey}";
            return await LoadAssetAsync<Sprite>(spritePath);
        }
        
        public async UniTask<Sprite> GetCardBackSpriteAsync()
        {
            return await LoadAssetAsync<Sprite>(GameSettings.CARD_BACK_SPRITE_ASSET_PATH);
        }
        
        #endregion
        
        #region Asset Unloading
        
        public void UnloadAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }
            
            if (_loadedAssets.TryGetValue(assetPath, out var asset))
            {
                if (asset != null && !_gameObjectPaths.Contains(assetPath))
                {
                    if (asset is Sprite || asset is Texture2D || asset is AudioClip || asset is ScriptableObject)
                    {
                        Resources.UnloadAsset(asset);
                        Debug.Log($"[AssetManager] Asset unloaded: {assetPath}");
                    }
                }
                
                _loadedAssets.Remove(assetPath);
                _gameObjectPaths.Remove(assetPath);
            }
        }
        
        public void UnloadAllAssets()
        {
            var assetsToUnload = new List<UnityEngine.Object>();
            
            foreach (var kvp in _loadedAssets)
            {
                if (kvp.Value != null && !_gameObjectPaths.Contains(kvp.Key))
                {
                    if (kvp.Value is Sprite || kvp.Value is Texture2D || 
                        kvp.Value is AudioClip || kvp.Value is ScriptableObject)
                    {
                        assetsToUnload.Add(kvp.Value);
                    }
                }
            }
            
            foreach (var asset in assetsToUnload)
            {
                if (asset != null)
                {
                    Resources.UnloadAsset(asset);
                }
            }
            
            _loadedAssets.Clear();
            _gameObjectPaths.Clear();
            
            Resources.UnloadUnusedAssets();
            
            Debug.Log("[AssetManager] All assets unloaded");
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            UnloadAllAssets();
            ServiceLocator.Instance.Unregister<IAssetService>(this);
        }
        
        #endregion
    }
}