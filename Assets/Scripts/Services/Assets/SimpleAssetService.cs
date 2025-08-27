using System.Collections.Generic;
using UnityEngine;

namespace CardWar.Services.Assets
{
    public class SimpleAssetService : IAssetService
    {
        private readonly Dictionary<string, Object> _cache = new();

        public T Load<T>(string path) where T : Object
        {
            if (_cache.TryGetValue(path, out var cached))
                return cached as T;

            var asset = Resources.Load<T>(path);
            _cache[path] = asset;
            return asset;
        }

        public GameObject Instantiate(string path, Transform parent = null)
        {
            var prefab = Load<GameObject>(path);
            return Object.Instantiate(prefab, parent);
        }

        public void Release(GameObject instance) => Object.Destroy(instance);
    }
}
