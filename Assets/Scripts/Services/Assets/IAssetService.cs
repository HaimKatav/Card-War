using UnityEngine;
using CardWar.Core.Data;
using CardWar.Configuration;
using Cysharp.Threading.Tasks;
using Zenject;

namespace CardWar.Services.Assets
{
    public interface IAssetService : IInitializable
    {
        bool AreAssetsLoaded { get; }
        
        Sprite GetCardSprite(CardData cardData);
        Sprite GetCardBackSprite();
        
        AudioClip GetSoundEffect(SFXType sfxType);
        AudioClip GetBackgroundMusic(string musicName);
        
        Sprite GetUISprite(string spriteName);
        
        T GetAsset<T>(string assetName, string customPath = null) where T : Object;
        
        UniTask PreloadCardAssets();
        void ClearCache();
        
        GameSettings GetGameSettings();
    }
    
    public enum SFXType
    {
        CardFlip,
        CardPlace,
        War,
        Victory,
        Defeat,
        ButtonClick,
        Deal,
        Shuffle
    }
}