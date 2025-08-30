using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Services;
using CardWar.Core;

namespace CardWar.Managers
{
    public class AudioManager : MonoBehaviour, IAudioService
    {
        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private Dictionary<string, AudioClip> _audioClips;
        private GameSettings _gameSettings;
        
        private float _masterVolume = 1f;
        private float _musicVolume = 0.7f;
        private float _sfxVolume = 1f;
        private bool _isMuted = false;

        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;
        public bool IsMuted => _isMuted;

        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;
        public event Action<bool> OnMuteStateChanged;

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            _gameSettings = ServiceLocator.Instance.Get<GameSettings>();
            _audioClips = new Dictionary<string, AudioClip>();
            
            SetupAudioSources();
            
            Debug.Log("[AudioManager] Initialized");
        }

        private void SetupAudioSources()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.volume = _musicVolume * _masterVolume;
            
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.volume = _sfxVolume * _masterVolume;
        }

        #endregion

        #region IAudioService Implementation

        public void PlayMusic(string musicKey, bool loop = true)
        {
            if (_isMuted) return;
            
            var clip = GetAudioClip(musicKey);
            if (clip != null && _musicSource != null)
            {
                _musicSource.clip = clip;
                _musicSource.Play();
                Debug.Log($"[AudioManager] Playing music: {musicKey}");
            }
        }

        public void PlaySound(SoundEffect sound)
        {
            if (_isMuted) return;
            
            var clip = GetAudioClip(sound.ToString());
            if (clip != null && _sfxSource != null)
            {
                _sfxSource.PlayOneShot(clip);
                Debug.Log($"[AudioManager] Playing SFX: {sound.ToString()}");
            }
        }

        public void StopMusic()
        {
            if (_musicSource != null && _musicSource.isPlaying)
            {
                _musicSource.Stop();
                Debug.Log("[AudioManager] Music stopped");
            }
        }

        public void PauseMusic()
        {
            if (_musicSource != null && _musicSource.isPlaying)
            {
                _musicSource.Pause();
                Debug.Log("[AudioManager] Music paused");
            }
        }

        public void ResumeMusic()
        {
            if (_musicSource != null && !_musicSource.isPlaying && _musicSource.clip != null)
            {
                _musicSource.UnPause();
                Debug.Log("[AudioManager] Music resumed");
            }
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
            OnMasterVolumeChanged?.Invoke(_masterVolume);
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
            OnMusicVolumeChanged?.Invoke(_musicVolume);
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
            OnSFXVolumeChanged?.Invoke(_sfxVolume);
        }

        public void SetMute(bool mute)
        {
            _isMuted = mute;
            
            if (_musicSource != null)
                _musicSource.mute = mute;
                
            if (_sfxSource != null)
                _sfxSource.mute = mute;
                
            OnMuteStateChanged?.Invoke(_isMuted);
            Debug.Log($"[AudioManager] Mute state: {_isMuted}");
        }

        #endregion

        #region Private Methods

        private AudioClip GetAudioClip(string clipName)
        {
            if (_audioClips.TryGetValue(clipName, out var clip))
                return clip;
            
            if (_gameSettings == null)
            {
                Debug.LogWarning("[AudioManager] GameSettings not found");
                return null;
            }
            
            var path = $"{GameSettings.AUDIO_ASSETS_PATH}/{clipName}";
            clip = Resources.Load<AudioClip>(path);
            
            if (clip != null)
            {
                _audioClips[clipName] = clip;
                return clip;
            }
            
            Debug.LogWarning($"[AudioManager] Audio clip not found: {clipName}");
            return null;
        }

        private void UpdateVolumes()
        {
            if (_musicSource != null)
                _musicSource.volume = _musicVolume * _masterVolume;
                
            if (_sfxSource != null)
                _sfxSource.volume = _sfxVolume * _masterVolume;
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            StopMusic();
            _audioClips?.Clear();
        }

        #endregion
    }
}