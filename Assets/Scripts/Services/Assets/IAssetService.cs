using UnityEngine;
using System.Collections.Generic;
using CardWar.Core.Data;
using Cysharp.Threading.Tasks;

namespace CardWar.Services.Assets
{
    public interface IAssetService
    {
        bool AreAssetsLoaded { get; }
        
        Sprite GetCardSprite(CardData card);
        Sprite GetCardSprite(string cardName);
        Sprite GetCardBackSprite();
        Sprite GetUISprite(string spriteName);
        
        GameObject LoadPrefab(string prefabName);
        
        AudioClip GetSoundEffect(string soundName);
        AudioClip GetMusic(string musicName);
        
        T LoadAsset<T>(string assetPath) where T : Object;
        UniTask<T> LoadAssetAsync<T>(string assetPath) where T : Object;
        
        void PreloadCardSprites(List<CardData> cards);
        void UnloadAsset(string assetPath);
        void ClearCache();
    }
}