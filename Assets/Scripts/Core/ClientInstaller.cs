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
            _gameSettings = Resources.Load<GameSettings>("Settings/GameSettings");
            if (_gameSettings == null)
            {
                Debug.LogError("GameSettings not found in Resources/Settings/");
                _gameSettings = ScriptableObject.CreateInstance<GameSettings>();
            }
            
            _networkSettings = Resources.Load<NetworkSettingsData>("Settings/NetworkSettings");
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
            CreateGameManager();
            CreateAssetManager();
            CreateAudioManager();
        }

        private void CreateGameManager()
        {
            GameObject managerObject = new GameObject("GameManager");
            managerObject.transform.SetParent(transform);
            GameManager gameManager = managerObject.AddComponent<GameManager>();
            
            RegisterService<IGameStateService>(gameManager);
        }

        private void CreateAssetManager()
        {
            GameObject managerObject = new GameObject("AssetManager");
            managerObject.transform.SetParent(transform);
            managerObject.AddComponent<AssetManager>();
        }

        private void CreateAudioManager()
        {
            GameObject managerObject = new GameObject("AudioManager");
            managerObject.transform.SetParent(transform);
            managerObject.AddComponent<AudioManager>();
        }

        private void NotifyStartupComplete()
        {
            IGameStateService gameStateService = GetService<IGameStateService>();
            gameStateService?.NotifyStartupComplete();
        }

        public T GetService<T>() where T : class
        {
            Type serviceType = typeof(T);
            if (_services.TryGetValue(serviceType, out object service))
            {
                return service as T;
            }
            
            Debug.LogWarning($"Service {serviceType.Name} not found in container");
            return null;
        }

        public void RegisterService<T>(T service) where T : class
        {
            Type serviceType = typeof(T);
            
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
            Type serviceType = typeof(T);
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