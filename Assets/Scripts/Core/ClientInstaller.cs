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
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class InitializeAttribute : Attribute 
    {
        public int Order { get; set; }
        public InitializeAttribute(int order = 0) => Order = order;
    }

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
            Debug.Log($"[{GetType().Name}] Starting initialization");
            
            ValidateSettings();
            RegisterServiceDefinitions();
            RegisterSelf();
            
            await UniTask.DelayFrame(1);
            
            CreateAllServices();
            
            await UniTask.DelayFrame(1);
            
            InjectDependencies();
            InitializeServices();
            
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
            
            Debug.Log($"[{GetType().Name}] Settings validated");
        }

        private void RegisterServiceDefinitions()
        {
            RegisterServiceType<IGameStateService, GameManager>();
            RegisterServiceType<IAssetService, AssetManager>();
            RegisterServiceType<IAudioService, AudioManager>();
            RegisterServiceType<IUIService, UIManager>();
            RegisterServiceType<IGameControllerService, GameController>();
        }

        private void RegisterServiceType<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : Component
        {
            _serviceRegistry.Add((typeof(TInterface), typeof(TImplementation), typeof(TImplementation).Name));
        }

        private void RegisterSelf()
        {
            RegisterService<IDIService>(this);
            RegisterService<ClientInstaller>(this);
        }

        #endregion

        #region Service Creation

        private void CreateAllServices()
        {
            Debug.Log($"[{GetType().Name}] Creating all services");
            
            foreach (var (interfaceType, implementationType, name) in _serviceRegistry)
            {
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

        #region Dependency Injection

        private void InjectDependencies()
        {
            Debug.Log($"[{GetType().Name}] Injecting dependencies");
            
            foreach (var service in _services.Values.Distinct())
            {
                InjectServiceDependencies(service);
            }
        }

        private void InjectServiceDependencies(object service)
        {
            var serviceType = service.GetType();
            
            var fields = serviceType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<InjectAttribute>() != null);
            
            foreach (var field in fields)
            {
                var dependency = GetServiceForType(field.FieldType);
                if (dependency != null)
                {
                    field.SetValue(service, dependency);
                    Debug.Log($"[{GetType().Name}] Injected {field.FieldType.Name} into {serviceType.Name}.{field.Name}");
                }
            }
            
            var properties = serviceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<InjectAttribute>() != null && p.CanWrite);
            
            foreach (var property in properties)
            {
                var dependency = GetServiceForType(property.PropertyType);
                if (dependency != null)
                {
                    property.SetValue(service, dependency);
                    Debug.Log($"[{GetType().Name}] Injected {property.PropertyType.Name} into {serviceType.Name}.{property.Name}");
                }
            }
        }

        private object GetServiceForType(Type type)
        {
            if (type == typeof(GameSettings))
                return _gameSettings;
            if (type == typeof(NetworkSettingsData))
                return _networkSettings;
            
            return _services.TryGetValue(type, out var service) ? service : null;
        }

        private void InitializeServices()
        {
            Debug.Log($"[{GetType().Name}] Initializing services");
            
            var servicesWithInit = _services.Values.Distinct()
                .Select(s => new 
                {
                    Service = s,
                    Methods = s.GetType()
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .Where(m => m.GetCustomAttribute<InitializeAttribute>() != null)
                        .OrderBy(m => m.GetCustomAttribute<InitializeAttribute>().Order)
                        .ToList()
                })
                .Where(x => x.Methods.Any())
                .ToList();
            
            foreach (var serviceInfo in servicesWithInit)
            {
                foreach (var method in serviceInfo.Methods)
                {
                    method.Invoke(serviceInfo.Service, null);
                    Debug.Log($"[{GetType().Name}] Called {method.Name} on {serviceInfo.Service.GetType().Name}");
                }
            }
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