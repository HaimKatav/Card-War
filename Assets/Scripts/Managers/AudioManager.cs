using System;
using UnityEngine;
using CardWar.Services;
using Zenject;

namespace CardWar.Managers
{
    public class AudioManager : MonoBehaviour, IAudioService, IDisposable
    {
        private IDIService _diService;
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
        public void Initialize(IDIService diService)
        {
            _diService = diService;
            _diService.RegisterService<IAudioService>(this);
            SetupAudioSources();
        }

        private void SetupAudioSources()
        {
            GameObject musicObject = new GameObject("MusicSource");
            musicObject.transform.SetParent(transform);
            _musicSource = musicObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;

            GameObject sfxObject = new GameObject("SfxSource");
            sfxObject.transform.SetParent(transform);
            _sfxSource = sfxObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            
            UpdateVolumes();
        }

        private void UpdateVolumes()
        {
            float effectiveMasterVolume = _isMuted ? 0f : _masterVolume;
            
            if (_musicSource != null)
            {
                _musicSource.volume = _musicVolume * effectiveMasterVolume;
            }
            
            if (_sfxSource != null)
            {
                _sfxSource.volume = _sfxVolume * effectiveMasterVolume;
            }
        }

        public void PlaySound(SoundEffect sound)
        {
            if (_sfxSource == null || _isMuted) return;
            
            AudioClip clip = GetSoundClip(sound);
            if (clip != null)
            {
                _sfxSource.PlayOneShot(clip);
                Debug.Log($"[AudioManager] Playing sound: {sound}");
            }
        }

        private AudioClip GetSoundClip(SoundEffect sound)
        {
            string clipPath = $"Audio/SFX/{sound}";
            return Resources.Load<AudioClip>(clipPath);
        }

        public void PlayMusic(string musicKey, bool loop = true)
        {
            if (_musicSource == null) return;
            
            AudioClip clip = Resources.Load<AudioClip>($"Audio/Music/{musicKey}");
            if (clip != null)
            {
                _musicSource.clip = clip;
                _musicSource.loop = loop;
                _musicSource.Play();
                Debug.Log($"[AudioManager] Playing music: {musicKey}");
            }
        }

        public void StopMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
            }
        }

        public void PauseMusic()
        {
            if (_musicSource != null && _musicSource.isPlaying)
            {
                _musicSource.Pause();
            }
        }

        public void ResumeMusic()
        {
            if (_musicSource != null && !_musicSource.isPlaying && _musicSource.clip != null)
            {
                _musicSource.UnPause();
            }
        }

        public void Dispose()
        {
            StopMusic();
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}