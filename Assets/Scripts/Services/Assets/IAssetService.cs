using UnityEngine;

namespace CardWar.Services.Assets
{
    public interface IAssetService
    {
        T Load<T>(string path) where T : Object;
        GameObject Instantiate(string path, Transform parent = null);
        void Release(GameObject instance);
    }
}
