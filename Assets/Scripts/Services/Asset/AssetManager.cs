using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace CardWar.Services.Assets
{
    /// <summary>
    /// Main asset management service - handles all asset loading, caching, and instantiation
    /// </summary>
    public class AssetManager : IAssetManager
    {
        private readonly Dictionary<string, UnityEngine.Object> _assetCache;
        private readonly Dictionary<GameObject, string> _instanceToAssetPath;
        private readonly HashSet<string> _loadingAssets;
        private bool _isDisposed;

        [Inject]
        public AssetManager()
        {
            _assetCache = new Dictionary<string, UnityEngine.Object>();
            _instanceToAssetPath = new Dictionary<GameObject, string>();
            _loadingAssets = new HashSet<string>();
        }

        public IAssetRequest<T> CreateLoadRequest<T>(string assetPath) where T : UnityEngine.Object
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AssetManager));

            return new AssetLoadRequest<T>(assetPath, this);
        }

        public async UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default) 
            where T : UnityEngine.Object
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AssetManager));

            // Check cache first
            if (_assetCache.TryGetValue(assetPath, out var cachedAsset))
            {
                if (cachedAsset is T typedAsset)
                {
                    Debug.Log($"[AssetManager] Asset '{assetPath}' loaded from cache");
                    return typedAsset;
                }
                
                Debug.LogWarning($"[AssetManager] Cached asset type mismatch for '{assetPath}'. " +
                               $"Expected {typeof(T)}, got {cachedAsset.GetType()}");
            }

            // Wait if asset is already being loaded
            while (_loadingAssets.Contains(assetPath))
            {
                await UniTask.Yield(cancellationToken);
                
                // Check cache again after waiting
                if (_assetCache.TryGetValue(assetPath, out cachedAsset))
                {
                    return cachedAsset as T;
                }
            }

            return await LoadAssetInternalAsync<T>(assetPath, null, cancellationToken);
        }

        public async UniTask<T> LoadAssetInternalAsync<T>(string assetPath, 
            Action<float> progressCallback,
            CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));

            _loadingAssets.Add(assetPath);

            try
            {
                progressCallback?.Invoke(0.1f);

                // TODO: Migrate to Addressables
                // Pseudo-implementation plan for Addressables:
                // 1. Replace Resources.Load with Addressables.LoadAssetAsync<T>(assetPath)
                // 2. Track the AsyncOperationHandle for proper release
                // 3. Use handle.PercentComplete for real progress updates
                // 4. Store handles in a Dictionary<string, AsyncOperationHandle> for release
                // 5. In ReleaseAsset: Addressables.Release(handle)
                // 6. Add initialization check for Addressables.InitializeAsync()
                // 7. Support both local and remote asset loading
                // 8. Implement download size checking before loading
                // 9. Add retry logic for remote asset failures
                // Example:
                // var handle = Addressables.LoadAssetAsync<T>(assetPath);
                // while (!handle.IsDone) {
                //     progressCallback?.Invoke(handle.PercentComplete);
                //     await UniTask.Yield(cancellationToken);
                // }
                // return handle.Result;

                // Current implementation using Resources.Load
                T asset = null;
                
                // Simulate async loading for Resources
                await UniTask.RunOnThreadPool(() =>
                {
                    asset = Resources.Load<T>(assetPath);
                }, cancellationToken: cancellationToken);

                progressCallback?.Invoke(0.5f);

                if (asset == null)
                {
                    // Try loading without type specification as fallback
                    var untyped = Resources.Load(assetPath);
                    if (untyped != null && untyped is T typedAsset)
                    {
                        asset = typedAsset;
                    }
                }

                progressCallback?.Invoke(0.9f);

                if (asset == null)
                {
                    throw new AssetLoadException($"Failed to load asset at path: {assetPath}");
                }

                // Cache the loaded asset
                _assetCache[assetPath] = asset;
                
                progressCallback?.Invoke(1f);
                
                Debug.Log($"[AssetManager] Successfully loaded asset '{assetPath}' of type {typeof(T)}");
                return asset;
            }
            finally
            {
                _loadingAssets.Remove(assetPath);
            }
        }

        public async UniTask<GameObject> InstantiateAsync(string assetPath, Transform parent = null, 
            CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AssetManager));

            var prefab = await LoadAssetAsync<GameObject>(assetPath, cancellationToken);
            
            if (prefab == null)
            {
                throw new AssetLoadException($"Failed to load prefab at path: {assetPath}");
            }

            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            
            // Track instance for proper cleanup
            _instanceToAssetPath[instance] = assetPath;
            
            Debug.Log($"[AssetManager] Instantiated '{assetPath}' -> {instance.name}");
            return instance;
        }

        public async UniTask<T> InstantiateAsync<T>(string assetPath, Transform parent = null,
            CancellationToken cancellationToken = default) where T : Component
        {
            var instance = await InstantiateAsync(assetPath, parent, cancellationToken);
            
            var component = instance.GetComponent<T>();
            if (component == null)
            {
                // Clean up the instance if component not found
                ReleaseInstance(instance);
                throw new AssetLoadException(
                    $"Prefab at '{assetPath}' does not have component of type {typeof(T)}");
            }

            return component;
        }

        public async UniTask PreloadAssetsAsync(string[] assetPaths, 
            Action<float> progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (assetPaths == null || assetPaths.Length == 0)
                return;

            var totalAssets = assetPaths.Length;
            var loadedAssets = 0;

            var loadTasks = assetPaths.Select(async path =>
            {
                try
                {
                    await LoadAssetAsync<UnityEngine.Object>(path, cancellationToken);
                    loadedAssets++;
                    progressCallback?.Invoke((float)loadedAssets / totalAssets);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AssetManager] Failed to preload '{path}': {ex.Message}");
                    // Continue loading other assets even if one fails
                    loadedAssets++;
                    progressCallback?.Invoke((float)loadedAssets / totalAssets);
                }
            });

            await UniTask.WhenAll(loadTasks);
            
            Debug.Log($"[AssetManager] Preloaded {loadedAssets}/{totalAssets} assets");
        }

        public void ReleaseAsset(string assetPath)
        {
            if (_assetCache.TryGetValue(assetPath, out var asset))
            {
                _assetCache.Remove(assetPath);
                
                // TODO: When using Addressables, call Addressables.Release(handle) here
                // For Resources, we can't explicitly unload individual assets
                // Only Resources.UnloadUnusedAssets() works, but it's expensive
                
                Debug.Log($"[AssetManager] Released asset '{assetPath}' from cache");
            }
        }

        public void ReleaseInstance(GameObject instance, bool releaseAsset = false)
        {
            if (instance == null)
                return;

            if (_instanceToAssetPath.TryGetValue(instance, out var assetPath))
            {
                _instanceToAssetPath.Remove(instance);
                
                if (releaseAsset)
                {
                    ReleaseAsset(assetPath);
                }
                
                Debug.Log($"[AssetManager] Released instance '{instance.name}'");
            }

            UnityEngine.Object.Destroy(instance);
        }

        public bool IsAssetLoaded(string assetPath)
        {
            return _assetCache.ContainsKey(assetPath);
        }

        public void ClearCache()
        {
            Debug.Log($"[AssetManager] Clearing cache with {_assetCache.Count} assets");
            
            // Destroy all tracked instances
            foreach (var instance in _instanceToAssetPath.Keys.ToList())
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }
            
            _instanceToAssetPath.Clear();
            _assetCache.Clear();
            _loadingAssets.Clear();
            
            // Force Unity to unload unused assets
            Resources.UnloadUnusedAssets();
            GC.Collect();
            
            Debug.Log("[AssetManager] Cache cleared and unused assets unloaded");
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            ClearCache();
            _isDisposed = true;
            
            Debug.Log("[AssetManager] AssetManager disposed");
        }
    }
}