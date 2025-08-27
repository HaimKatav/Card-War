using UnityEngine;
using System.Threading.Tasks;

public interface IAssetService
{
    Task<T> LoadAssetAsync<T>(string assetKey) where T : Object;
    Task<GameObject> InstantiateAsync(string assetKey, Transform parent = null);
    void ReleaseAsset(Object asset);
    void ReleaseInstance(GameObject instance);
}
