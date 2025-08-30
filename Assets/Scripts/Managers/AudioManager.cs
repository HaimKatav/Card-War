using System;
using UnityEngine;
using CardWar.Services;
using Zenject;

namespace CardWar.Managers
{
    public class AudioManager : MonoBehaviour, IAudioService, IDisposable
    {
        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        
        private float _masterVolume = 1.0f;
        private float _sfxVolume = 1.0f;
        private float _musicVolume = 0.5f;
        private bool _isMuted = false;
        
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;
                UpdateVolumes();
            }
        }

        [Inject]
        public void Construct()
        {
            SetupAudioSources();
            Debug.Log($"[AudioManager] Initialized");
        }

        private void SetupAudioSources()
        {
            var musicObject = new GameObject("MusicSource");
            musicObject.transform.SetParent(transform);
            _musicSource = musicObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;

            var sfxObject = new GameObject("SfxSource");
            sfxObject.transform.SetParent(transform);
            _sfxSource = sfxObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            
            UpdateVolumes();
        }

        private void UpdateVolumes()
        {
            var effectiveMasterVolume = _isMuted ? 0 : _masterVolume;
            
            if (_musicSource != null)
            {
                _musicSource.volume = _musicVolume * effectiveMasterVolume;
            }
            
            if (_sfxSource != null)
            {
                _sfxSource.volume = _sfxVolume * effectiveMasterVolume;
            }
        }

        public void PlaySound(SoundEffect soundEffect)
        {
            Debug.Log($"[AudioManager] Playing sound: {soundEffect}");
        }

        public void PlayMusic(string musicKey, bool loop = true)
        {
            Debug.Log($"[AudioManager] Playing music: {musicKey}, loop: {loop}");
        }

        public void StopMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
            }
            Debug.Log($"[AudioManager] Music stopped");
        }

        public void PauseMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Pause();
            }
            Debug.Log($"[AudioManager] Music paused");
        }

        public void ResumeMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.UnPause();
            }
            Debug.Log($"[AudioManager] Music resumed");
        }

        public void Dispose()
        {
            StopMusic();
            Debug.Log($"[AudioManager] Disposed");
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}