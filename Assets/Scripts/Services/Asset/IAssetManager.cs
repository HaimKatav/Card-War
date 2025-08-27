using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardWar.Services.Assets
{
    /// <summary>
    /// Interface for the main asset management service
    /// </summary>
    public interface IAssetManager : IDisposable
    {
        /// <summary>
        /// Creates a new asset load request for type-safe loading
        /// </summary>
        IAssetRequest<T> CreateLoadRequest<T>(string assetPath) where T : UnityEngine.Object;

        /// <summary>
        /// Loads an asset asynchronously with automatic caching
        /// </summary>
        UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken = default) 
            where T : UnityEngine.Object;

        /// <summary>
        /// Instantiates a prefab asynchronously
        /// </summary>
        UniTask<GameObject> InstantiateAsync(string assetPath, Transform parent = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Instantiates a prefab with a specific component
        /// </summary>
        UniTask<T> InstantiateAsync<T>(string assetPath, Transform parent = null,
            CancellationToken cancellationToken = default) where T : Component;

        /// <summary>
        /// Releases a cached asset from memory
        /// </summary>
        void ReleaseAsset(string assetPath);

        /// <summary>
        /// Releases an instantiated object and optionally its source asset
        /// </summary>
        void ReleaseInstance(GameObject instance, bool releaseAsset = false);

        /// <summary>
        /// Preloads multiple assets for faster access later
        /// </summary>
        UniTask PreloadAssetsAsync(string[] assetPaths, 
            Action<float> progressCallback = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an asset is already loaded in cache
        /// </summary>
        bool IsAssetLoaded(string assetPath);

        /// <summary>
        /// Clears all cached assets
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Internal method for loading - used by AssetLoadRequest
        /// </summary>
        UniTask<T> LoadAssetInternalAsync<T>(string assetPath, 
            Action<float> progressCallback,
            CancellationToken cancellationToken) where T : UnityEngine.Object;
    }
}