using System;
using System.Collections.Generic;
using CardWar.Game;
using UnityEngine;
using CardWar.Services;
using CardWar.Managers;
using Cysharp.Threading.Tasks;

namespace CardWar.Core
{
    public class ClientInstaller : MonoBehaviour, IDIService
    {
        [SerializeField] private GameSettings _gameSettings;
        [SerializeField] private NetworkSettingsData _networkSettings;
        
        private Dictionary<Type, object> _services;
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
            
            _services = new Dictionary<Type, object>();
            
            ValidateSettings();
            RegisterSelf();
            
            await UniTask.DelayFrame(1);
            
            CreateCoreServices();
            await UniTask.DelayFrame(1);
            
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

        private void RegisterSelf()
        {
            RegisterService<IDIService>(this);
        }

        #endregion

        #region Service Creation

        private void CreateCoreServices()
        {
            Debug.Log($"[{GetType().Name}] Creating core services");
            
            CreateGameManager();
            CreateAssetManager();
            CreateAudioManager();
            CreateUIManager();
            CreateGameController();
        }

        private void CreateGameManager()
        {
            GameObject managerObject = new GameObject("GameManager");
            managerObject.transform.SetParent(transform);
            GameManager gameManager = managerObject.AddComponent<GameManager>();
            RegisterService<IGameStateService>(gameManager);
            Debug.Log($"[{GetType().Name}] GameManager created and registered");
        }

        private void CreateAssetManager()
        {
            GameObject managerObject = new GameObject("AssetManager");
            managerObject.transform.SetParent(transform);
            AssetManager assetManager = managerObject.AddComponent<AssetManager>();
            RegisterService<IAssetService>(assetManager);
            Debug.Log($"[{GetType().Name}] AssetManager created and registered");
        }

        private void CreateAudioManager()
        {
            GameObject managerObject = new GameObject("AudioManager");
            managerObject.transform.SetParent(transform);
            AudioManager audioManager = managerObject.AddComponent<AudioManager>();
            RegisterService<IAudioService>(audioManager);
            Debug.Log($"[{GetType().Name}] AudioManager created and registered");
        }

        private void CreateUIManager()
        {
            GameObject managerObject = new GameObject("UIManager");
            managerObject.transform.SetParent(transform);
            UIManager uiManager = managerObject.AddComponent<UIManager>();
            RegisterService<IUIService>(uiManager);
            Debug.Log($"[{GetType().Name}] UIManager created and registered");
        }

        private void CreateGameController()
        {
            GameObject controllerObject = new GameObject("GameController");
            controllerObject.transform.SetParent(transform);
            GameController gameController = controllerObject.AddComponent<GameController>();
            RegisterService<IGameControllerService>(gameController);
            Debug.Log($"[{GetType().Name}] GameController created and registered");
        }

        #endregion

        #region Service Initialization

        private void InitializeServices()
        {
            Debug.Log($"[{GetType().Name}] Initializing services");
            
            GameManager gameManager = GetService<IGameStateService>() as GameManager;
            AssetManager assetManager = GetService<IAssetService>() as AssetManager;
            AudioManager audioManager = GetService<IAudioService>() as AudioManager;
            UIManager uiManager = GetService<IUIService>() as UIManager;
            GameController gameController = GetService<IGameControllerService>() as GameController;
            
            if (gameManager != null)
                gameManager.Initialize(this, _gameSettings);
            
            if (assetManager != null)
                assetManager.Initialize(this, _gameSettings);
            
            if (audioManager != null)
                audioManager.Initialize(this);
            
            if (uiManager != null)
                uiManager.Initialize(this, gameManager);
            
            if (gameController != null)
                gameController.Initialize(this, gameManager, uiManager, assetManager);
            
            Debug.Log($"[{GetType().Name}] All services initialized");
        }

        private void NotifyStartupComplete()
        {
            IGameStateService gameStateService = GetService<IGameStateService>();
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

        #region IDIService Implementation

        public T GetService<T>() where T : class
        {
            Type serviceType = typeof(T);
            if (_services.TryGetValue(serviceType, out object service))
            {
                return service as T;
            }
            
            Debug.LogWarning($"[{GetType().Name}] Service {serviceType.Name} not found");
            return null;
        }

        public void RegisterService<T>(T service) where T : class
        {
            Type serviceType = typeof(T);
            
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
            Type serviceType = typeof(T);
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