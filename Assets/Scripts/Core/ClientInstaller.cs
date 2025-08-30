using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CardWar.Services;
using CardWar.Managers;
using CardWar.Game;
using Cysharp.Threading.Tasks;

namespace CardWar.Core
{
    public class ClientInstaller : MonoBehaviour, IDIService
    {
        [SerializeField] private GameSettings _gameSettings;
        [SerializeField] private NetworkSettingsData _networkSettings;
        
        private readonly Dictionary<Type, object> _services = new ();
        private readonly List<(Type Interface, Type Implementation, string Name)> _serviceRegistry = new();
        
        private bool _isInitialized;

        #region Initialization
        
        private void Awake()
        {
            if (_isInitialized) return;
            
            DontDestroyOnLoad(gameObject);
            InitializeContainer().Forget();
        }

        private async UniTaskVoid InitializeContainer()
        {
            ValidateSettings();
            RegisterServiceDefinitions();
            RegisterSelf();
            
            await UniTask.DelayFrame(1);
            
            CreateAllServices();
            
            await UniTask.DelayFrame(1);
            
            NotifyStartupComplete();
            
            _isInitialized = true;
            Debug.Log($"[{GetType().Name}] Initialization complete");
        }

        private void ValidateSettings()
        {
            if (_gameSettings == null)
            {
                Debug.LogError($"[{GetType().Name}] GameSettings not assigned!");
                _gameSettings = ScriptableObject.CreateInstance<GameSettings>();
            }
            
            if (_networkSettings == null)
            {
                Debug.LogWarning($"[{GetType().Name}] NetworkSettings not assigned, using defaults");
                _networkSettings = ScriptableObject.CreateInstance<NetworkSettingsData>();
            }
        }

        private void RegisterServiceDefinitions()
        {
            RegisterServiceType<GameSettings, GameSettings>();
            RegisterServiceType<IAssetService, AssetManager>();
            RegisterServiceType<IAudioService, AudioManager>();
            RegisterServiceType<IGameStateService, GameManager>();
            RegisterServiceType<IUIService, UIManager>();
            RegisterServiceType<IGameControllerService, GameController>();
        }

        private void RegisterServiceType<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : UnityEngine.Object
        {
            _serviceRegistry.Add((typeof(TInterface), typeof(TImplementation), typeof(TImplementation).Name));
        }

        private void RegisterSelf()
        {
            RegisterService<IDIService>(this);
        }

        #endregion

        #region Service Creation

        private void CreateAllServices()
        {
            Debug.Log($"[{GetType().Name}] Creating all services");
            
            foreach (var (interfaceType, implementationType, name) in _serviceRegistry)
            {
                if (interfaceType != typeof(GameSettings)) 
                    CreateService(interfaceType, implementationType, name);
            }
        }

        private void CreateService(Type interfaceType, Type implementationType, string name)
        {
            var serviceObject = new GameObject(name);
            serviceObject.transform.SetParent(transform);
            
            var component = serviceObject.AddComponent(implementationType);
            
            _services[interfaceType] = component;
            _services[implementationType] = component;
            
            Debug.Log($"[{GetType().Name}] {name} created and registered");
        }

        #endregion

        #region IDIService Implementation

        public T GetService<T>() where T : class
        {
            var serviceType = typeof(T);
            if (_services.TryGetValue(serviceType, out var service))
            {
                return service as T;
            }
            
            Debug.LogWarning($"[{GetType().Name}] Service {serviceType.Name} not found");
            return null;
        }

        public void RegisterService<T>(T service) where T : class
        {
            var serviceType = typeof(T);
            
            if (_services.ContainsKey(serviceType))
            {
                Debug.LogWarning($"[{GetType().Name}] Service {serviceType.Name} already registered, replacing");
                _services[serviceType] = service;
            }
            else
            {
                _services.Add(serviceType, service);
                Debug.Log($"[{GetType().Name}] Service {serviceType.Name} registered");
            }
        }

        public void UnregisterService<T>() where T : class
        {
            var serviceType = typeof(T);
            if (_services.Remove(serviceType))
            {
                Debug.Log($"[{GetType().Name}] Service {serviceType.Name} unregistered");
            }
        }

        public bool HasService<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        #endregion

        #region Public Methods

        public GameSettings GetGameSettings() => _gameSettings;
        public NetworkSettingsData GetNetworkSettings() => _networkSettings;

        private void NotifyStartupComplete()
        {
            var gameStateService = GetService<IGameStateService>();
            if (gameStateService != null)
            {
                gameStateService.NotifyStartupComplete();
                Debug.Log($"[{GetType().Name}] Startup complete notification sent");
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] GameStateService not found!");
            }
        }

        #endregion

        #region Cleanup

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
            Debug.Log($"[{GetType().Name}] Disposed");
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion
    }
}