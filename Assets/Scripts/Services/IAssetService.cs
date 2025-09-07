using UnityEngine;
using Cysharp.Threading.Tasks;

namespace CardWar.Services
{
    public interface IAssetService
    {
        UniTask<T> LoadAssetAsync<T>(string assetPath) where T : Object;
        void UnloadAsset(string assetPath);
        T LoadAsset<T>(string assetPath) where T : Object;
        
        UniTask<Sprite> GetCardSpriteAsync(string cardKey);
        UniTask<Sprite> GetCardBackSpriteAsync();
    }
}