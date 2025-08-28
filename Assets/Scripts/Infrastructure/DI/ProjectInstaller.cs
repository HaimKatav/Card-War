using UnityEngine;
using Zenject;
using CardWar.Services.Assets;
using CardWar.Services.Game;
using CardWar.Services.Network;
using CardWar.Infrastructure.Events;
using CardWar.UI.Cards;
using CardWar.Configuration;
using CardWar.Core.Async;
using System;

namespace CardWar.Infrastructure.DI
{
    public class ProjectInstaller : MonoInstaller<ProjectInstaller>
    {
        [SerializeField] private GameSettings _gameSettings;
        [SerializeField] private NetworkErrorConfig _networkErrorConfig;
        
        public override void InstallBindings()
        {
            Debug.Log("[ProjectInstaller] Starting global service binding...");
            
            InstallSignals();
            InstallGameSettings();
            InstallNetworkConfiguration();
            InstallGlobalServices();
            InstallAsyncManagement();
            InstallCardPools();
            
            Debug.Log("[ProjectInstaller] Global services bound successfully");
        }
        
        private void InstallSignals()
        {
            SignalBusInstaller.Install(Container);
            Debug.Log("[ProjectInstaller] SignalBus installed successfully");
            
            Container.DeclareSignal<GameStartEvent>().OptionalSubscriber();
            Container.DeclareSignal<GameEndEvent>().OptionalSubscriber();
            Container.DeclareSignal<RoundStartEvent>().OptionalSubscriber();
            Container.DeclareSignal<RoundCompleteEvent>().OptionalSubscriber();
            Container.DeclareSignal<WarStartEvent>().OptionalSubscriber();
            Container.DeclareSignal<GameStateChangedEvent>().OptionalSubscriber();
            Container.DeclareSignal<PlayerActionEvent>().OptionalSubscriber();
            
            Debug.Log("[ProjectInstaller] All signals declared successfully");
        }
        
        private void InstallGameSettings()
        {
            if (_gameSettings == null)
            {
                _gameSettings = GameSettings.Instance;
                
                if (_gameSettings == null)
                {
                    Debug.LogError("[ProjectInstaller] GameSettings not found! Creating default...");
                    _gameSettings = ScriptableObject.CreateInstance<GameSettings>();
                }
            }
            
            Container.BindInstance(_gameSettings).AsSingle();
            Debug.Log("[ProjectInstaller] GameSettings bound successfully");
        }
        
        private void InstallNetworkConfiguration()
        {
            if (_networkErrorConfig == null)
            {
                _networkErrorConfig = Resources.Load<NetworkErrorConfig>("Settings/NetworkErrorConfig");
                
                if (_networkErrorConfig == null)
                {
                    Debug.LogWarning("[ProjectInstaller] NetworkErrorConfig not found, creating default...");
                    _networkErrorConfig = CreateDefaultNetworkConfig();
                }
            }
            
            Container.BindInstance(_networkErrorConfig).AsSingle();
            Debug.Log("[ProjectInstaller] NetworkErrorConfig bound successfully");
        }
        
        private NetworkErrorConfig CreateDefaultNetworkConfig()
        {
            var config = ScriptableObject.CreateInstance<NetworkErrorConfig>();
            
            // Set default values that match NetworkErrorConfig structure
            config.timeoutRate = 0.02f;
            config.networkErrorRate = 0.05f;
            config.serverErrorRate = 0.01f;
            config.corruptionRate = 0.005f;
            config.minNetworkDelay = 0.1f;
            config.maxNetworkDelay = 0.5f;
            config.timeoutDuration = 5f;
            config.retryBaseDelay = 1f;
            
            // If GameSettings exists, use some of its values
            if (_gameSettings != null)
            {
                config.networkErrorRate = _gameSettings.networkErrorRate;
                config.minNetworkDelay = _gameSettings.minNetworkDelay;
                config.maxNetworkDelay = _gameSettings.maxNetworkDelay;
                config.timeoutDuration = _gameSettings.networkTimeoutDuration;
                // Note: GameSettings doesn't have timeoutRate or retryBaseDelay
            }
            
            return config;
        }
        
        private void InstallGlobalServices()
        {
            // Asset Service with IInitializable
            Container.Bind(typeof(IAssetService), typeof(IInitializable))
                .To<AssetService>()
                .AsSingle()
                .NonLazy();
            
            // Network Services - Note: No INetworkSimulator interface exists
            // Just bind the concrete FakeWarServer
            Container.Bind<IFakeServerService>()
                .To<FakeWarServer>()
                .AsSingle();
            
            // Game Service
            Container.Bind<IGameService>()
                .To<GameService>()
                .AsSingle();
            
            Debug.Log("[ProjectInstaller] Global services bound successfully");
        }
        
        private void InstallAsyncManagement()
        {
            // AsyncOperationManager is just a concrete class with IDisposable
            Container.Bind<AsyncOperationManager>()
                .AsSingle()
                .NonLazy();
            
            // Bind IDisposable separately
            Container.Bind<IDisposable>()
                .To<AsyncOperationManager>()
                .FromResolve()
                .WhenInjectedInto<AsyncOperationManager>();
            
            Debug.Log("[ProjectInstaller] AsyncOperationManager bound successfully");
        }
        
        private void InstallCardPools()
        {
            int poolSize = _gameSettings != null ? _gameSettings.cardPoolInitialSize : 20;
            string prefabPath = _gameSettings != null ? _gameSettings.GetCardPrefabResourcePath() : "Prefabs/CardPrefab";
            
            Container.BindMemoryPool<CardViewController, CardViewController.Pool>()
                .WithInitialSize(poolSize)
                .FromComponentInNewPrefabResource(prefabPath)
                .UnderTransformGroup("CardPool");
            
            Debug.Log($"[ProjectInstaller] Card pools bound with initial size: {poolSize}");
        }
    }
}