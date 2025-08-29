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

    public interface IAudioService
    {
        float MasterVolume { get; set; }
        float SfxVolume { get; set; }
        float MusicVolume { get; set; }
        bool IsMuted { get; set; }
        
        void PlaySound(SoundEffect sound);
        void PlayMusic(string musicKey, bool loop = true);
        void StopMusic();
        void PauseMusic();
        void ResumeMusic();
    }
}