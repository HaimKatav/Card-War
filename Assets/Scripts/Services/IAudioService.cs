using System;

namespace CardWar.Services
{
    public enum SoundEffect
    {
        CardFlip,
        CardDraw,
        CardCollect,
        WarStart,
        Victory,
        Defeat,
        ButtonClick
    }

    public interface IAudioService : IBaseServiceProvider
    {
        float MasterVolume { get; }
        float SFXVolume { get; }
        float MusicVolume { get; }
        bool IsMuted { get; }
        
        void PlaySound(SoundEffect sound);
        void PlayMusic(string musicKey, bool loop = true);
        void StopMusic();
        void PauseMusic();
        void ResumeMusic();
    }
}