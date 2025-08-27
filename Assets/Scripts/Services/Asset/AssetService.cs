using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

public class AssetService : IAssetService
{
    private readonly Dictionary<string, Object> _loadedAssets = new Dictionary<string, Object>();
    private readonly Dictionary<string, GameObject> _instances = new Dictionary<string, GameObject>();

    public async Task<T> LoadAssetAsync<T>(string assetKey) where T : Object
    {
        if (_loadedAssets.TryGetValue(assetKey, out Object cachedAsset))
        {
            return cachedAsset as T;
        }

        // For now, use Resources.Load - this will be replaced with Addressables
        var asset = Resources.Load<T>(assetKey);
        if (asset != null)
        {
            _loadedAssets[assetKey] = asset;
        }

        // Simulate async loading
        await Task.Delay(10);
        
        return asset;
    }

    public async Task<GameObject> InstantiateAsync(string assetKey, Transform parent = null)
    {
        var prefab = await LoadAssetAsync<GameObject>(assetKey);
        if (prefab == null)
        {
            Debug.LogError($"Failed to load prefab with key: {assetKey}");
            return null;
        }

        var instance = Object.Instantiate(prefab, parent);
        _instances[assetKey] = instance;
        
        return instance;
    }

    public void ReleaseAsset(Object asset)
    {
        if (asset != null)
        {
            // Remove from loaded assets cache
            var keyToRemove = "";
            foreach (var kvp in _loadedAssets)
            {
                if (kvp.Value == asset)
                {
                    keyToRemove = kvp.Key;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(keyToRemove))
            {
                _loadedAssets.Remove(keyToRemove);
            }

            // For now, just destroy the asset
            // This will be replaced with proper Addressables release
            Object.Destroy(asset);
        }
    }

    public void ReleaseInstance(GameObject instance)
    {
        if (instance != null)
        {
            // Remove from instances cache
            var keyToRemove = "";
            foreach (var kvp in _instances)
            {
                if (kvp.Value == instance)
                {
                    keyToRemove = kvp.Key;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(keyToRemove))
            {
                _instances.Remove(keyToRemove);
            }

            Object.Destroy(instance);
        }
    }
}
