using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Services;
using CardWar.Managers;
using Cysharp.Threading.Tasks;
using Zenject;

namespace CardWar.Core
{
    public class ClientInstaller : MonoInstaller, IDIService
    {
        private Dictionary<Type, object> _services;
        private GameSettings _gameSettings;
        private NetworkSettingsData _networkSettings;
        private bool _isInitialized;

        private void Awake()
        {
            if (_isInitialized) return;
            
            DontDestroyOnLoad(gameObject);
            InitializeContainer();
        }

        private void InitializeContainer()
        {
            _services = new Dictionary<Type, object>();
            
            LoadSettings();
            RegisterSelf();
            CreateCoreServices();
            NotifyStartupComplete();
            
            _isInitialized = true;
        }

        private void LoadSettings()
        {
            _gameSettings = Resources.Load<GameSettings>(GameSettings.GAME_SETTINGS_ASSET_PATH);
            if (_gameSettings == null)
            {
                Debug.LogError($"GameSettings not found.");
                _gameSettings = ScriptableObject.CreateInstance<GameSettings>();
            }
            
            _networkSettings = Resources.Load<NetworkSettingsData>(GameSettings.NETWORK_SETTINGS_ASSET_PATH);
            if (_networkSettings == null)
            {
                Debug.LogWarning("NetworkSettings not found, using defaults");
                _networkSettings = ScriptableObject.CreateInstance<NetworkSettingsData>();
            }
        }

        private void RegisterSelf()
        {
            RegisterService<IDIService>(this);
        }

        private void CreateCoreServices()
        {
            var managerObject = new GameObject("Managers").transform;
            CreateManager<AssetManager>("AssetManager", managerObject);
            CreateManager<AudioManager>("AudioManager", managerObject);
            CreateManager<GameManager>("GameManager", managerObject);
        }

        private void CreateManager<T>(string managerName, Transform managerTransform) where T : Component
        {
            var manager = new GameObject(managerName);
            manager.transform.SetParent(managerTransform);
            manager.AddComponent<T>();
        }
        
        private void NotifyStartupComplete()
        {
            var gameStateService = GetService<IGameStateService>();
            gameStateService?.NotifyStartupComplete();
        }

        public T GetService<T>() where T : class
        {
            var serviceType = typeof(T);
            if (_services.TryGetValue(serviceType, out var service))
            {
                return service as T;
            }
            
            Debug.LogWarning($"Service {serviceType.Name} not found in container");
            return null;
        }

        public void RegisterService<T>(T service) where T : class
        {
            var serviceType = typeof(T);
            
            if (_services.ContainsKey(serviceType))
            {
                Debug.LogWarning($"Service {serviceType.Name} already registered, replacing");
                _services[serviceType] = service;
            }
            else
            {
                _services.Add(serviceType, service);
                Debug.Log($"Service {serviceType.Name} registered successfully");
            }
        }

        public void UnregisterService<T>() where T : class
        {
            var serviceType = typeof(T);
            if (_services.Remove(serviceType))
            {
                Debug.Log($"Service {serviceType.Name} unregistered");
            }
        }

        public bool HasService<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        public GameSettings GetGameSettings()
        {
            return _gameSettings;
        }

        public NetworkSettingsData GetNetworkSettings()
        {
            return _networkSettings;
        }

        public void Dispose()
        {
            foreach (var kvp in _services)
            {
                if (kvp.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _services.Clear();
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}