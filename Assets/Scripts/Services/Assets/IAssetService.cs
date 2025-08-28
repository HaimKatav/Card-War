using UnityEngine;
using CardWar.Core.Data;
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
        
        UniTask PreloadCardAssets();
        void ClearCache();
    }
    
    public enum SFXType
    {
        CardFlip,
        CardPlace,
        War,
        Victory,
        Defeat,
        ButtonClick,
        Deal
    }
}