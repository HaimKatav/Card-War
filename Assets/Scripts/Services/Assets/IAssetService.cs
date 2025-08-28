using UnityEngine;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using Cysharp.Threading.Tasks;
using Zenject;

namespace CardWar.Services.Assets
{
    public interface IAssetService : IInitializable
    {
        Sprite GetCardSprite(CardData cardData);
        
        Sprite GetCardBackSprite();
        
        UniTask PreloadAllCardSprites();
        bool AreAllCardSpritesLoaded();
        
        Sprite GetPanelBackground(PanelType panelType);
        Sprite GetButtonSprite(ButtonType buttonType, ButtonState state = ButtonState.Normal);
        Sprite GetStatusSprite(StatusType statusType);
        AudioClip GetSoundEffect(SFXType sfxType);
        AudioClip GetBackgroundMusic(MusicType musicType);
        ParticleSystem GetParticleEffect(EffectType effectType);
        T GetAsset<T>(string assetName) where T : Object;
        void ClearCache();
        AssetMemoryStats GetMemoryStats();
        void PreloadEssentialAssets();
    }
    
    [System.Serializable]
    public struct AssetMemoryStats
    {
        public int LoadedSprites;
        public int LoadedAudioClips;
        public int LoadedParticles;
        public int CachedAssets;
        public long EstimatedMemoryUsage;
        
        public override string ToString()
        {
            return $"Assets: {CachedAssets} total, Sprites: {LoadedSprites}, Audio: {LoadedAudioClips}, Particles: {LoadedParticles}, Memory: ~{EstimatedMemoryUsage / 1024 / 1024}MB";
        }
    }
}