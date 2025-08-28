using UnityEngine;
using Zenject;
using CardWar.Core.Async;
using CardWar.Services.Network;
using CardWar.Services.Assets;

namespace CardWar.Infrastructure.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        [Header("Network Configuration")]
        [SerializeField] private NetworkErrorConfig _networkErrorConfig;
        
        [Header("Asset Loading")]
        [SerializeField] private bool _preloadCardAssets = true;
        
        public override void InstallBindings()
        {
            Debug.Log("[ProjectInstaller] Installing project bindings");
            
            InstallAsyncManagement();
            InstallNetworkServices();
            InstallAssetServices();
        }
        
        private void InstallAsyncManagement()
        {
            Container.BindInterfacesAndSelfTo<AsyncOperationManager>()
                .AsSingle()
                .NonLazy();
                
            Debug.Log("[ProjectInstaller] AsyncOperationManager bound");
        }
        
        private void InstallNetworkServices()
        {
            // Create default config if not assigned
            if (_networkErrorConfig == null)
            {
                _networkErrorConfig = CreateDefaultNetworkConfig();
                Debug.LogWarning("[ProjectInstaller] NetworkErrorConfig was null, using default configuration");
            }
            
            Container.Bind<NetworkErrorConfig>()
                .FromInstance(_networkErrorConfig)
                .AsSingle();
            
            Container.Bind<IFakeServerService>()
                .To<FakeWarServer>()
                .AsSingle()
                .NonLazy();
                
            Debug.Log("[ProjectInstaller] Network services bound");
        }
        
        private void InstallAssetServices()
        {
            Container.Bind<ICardAssetManager>()
                .To<CardAssetManager>()
                .AsSingle()
                .NonLazy();
            
            // If preload is enabled, mark for initialization
            if (_preloadCardAssets)
            {
                Container.BindInterfacesAndSelfTo<CardAssetInitializer>()
                    .AsSingle()
                    .NonLazy();
            }
            
            Debug.Log("[ProjectInstaller] Asset services bound");
        }
        
        private NetworkErrorConfig CreateDefaultNetworkConfig()
        {
            var config = ScriptableObject.CreateInstance<NetworkErrorConfig>();
            
            // Set reasonable defaults
            config.timeoutRate = 0.02f;
            config.networkErrorRate = 0.05f;
            config.serverErrorRate = 0.01f;
            config.corruptionRate = 0.005f;
            config.minNetworkDelay = 0.1f;
            config.maxNetworkDelay = 0.5f;
            config.timeoutDuration = 5.0f;
            config.retryBaseDelay = 1.0f;
            
            // Default error messages
            config.networkErrorMessages = new string[]
            {
                "Connection timeout",
                "Network unreachable",
                "Connection lost",
                "DNS resolution failed"
            };
            
            config.serverErrorMessages = new string[]
            {
                "Internal server error",
                "Service unavailable",
                "Database connection failed",
                "Rate limit exceeded"
            };
            
            return config;
        }
        
        // Helper class to initialize card assets at startup
        private class CardAssetInitializer : IInitializable
        {
            private readonly ICardAssetManager _cardAssetManager;
            
            public CardAssetInitializer(ICardAssetManager cardAssetManager)
            {
                _cardAssetManager = cardAssetManager;
            }
            
            public async void Initialize()
            {
                Debug.Log("[CardAssetInitializer] Starting card asset preload");
                await _cardAssetManager.PreloadAllCardAssets();
                Debug.Log("[CardAssetInitializer] Card asset preload complete");
            }
        }
    }
}