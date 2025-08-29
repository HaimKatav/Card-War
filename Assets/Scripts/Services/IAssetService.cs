using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace CardWar.Services
{
    public interface IAssetService
    {
        UniTask<T> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object;
        T LoadAsset<T>(string assetPath) where T : UnityEngine.Object;
        void UnloadAsset(string assetPath);
        UniTask PreloadCardAssets();
        Sprite GetCardSprite(string cardKey);
        Sprite GetCardBackSprite();
    }
}