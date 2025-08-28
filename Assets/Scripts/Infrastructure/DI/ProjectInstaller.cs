using System;
using UnityEngine;
using Zenject;
using CardWar.Configuration;
using CardWar.Services.Assets;
using CardWar.Services.Network;
using CardWar.Services.Game;
using CardWar.Core.Async;
using CardWar.Infrastructure.Events;

namespace CardWar.Infrastructure.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        [Header("Configuration")]
        [SerializeField] private GameSettings _gameSettings;
        [SerializeField] private NetworkErrorConfig _networkErrorConfig;
        
        public override void InstallBindings()
        {
            Debug.Log("[ProjectInstaller] Starting global installations...");
            
            InstallGameSettings();
            InstallNetworkConfiguration();
            InstallGlobalServices();
            InstallAsyncManagement();
            InstallSignals();
            
            Debug.Log("[ProjectInstaller] Global installations complete");
        }
        
        private void InstallGameSettings()
        {
            if (_gameSettings == null)
            {
                _gameSettings = Resources.Load<GameSettings>("GameSettings");
                
                if (_gameSettings == null)
                {
                    Debug.LogWarning("[ProjectInstaller] GameSettings not found, creating default...");
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
            
            config.timeoutRate = 0.02f;
            config.networkErrorRate = 0.05f;
            config.serverErrorRate = 0.01f;
            config.corruptionRate = 0.005f;
            config.minNetworkDelay = 0.1f;
            config.maxNetworkDelay = 0.5f;
            config.timeoutDuration = 5f;
            config.retryBaseDelay = 1f;
            
            if (_gameSettings != null)
            {
                config.networkErrorRate = _gameSettings.networkErrorRate;
                config.minNetworkDelay = _gameSettings.minNetworkDelay;
                config.maxNetworkDelay = _gameSettings.maxNetworkDelay;
                config.timeoutDuration = _gameSettings.networkTimeoutDuration;
            }
            
            return config;
        }
        
        private void InstallGlobalServices()
        {
            Container.Bind(typeof(IAssetService), typeof(IInitializable))
                .To<AssetService>()
                .AsSingle()
                .NonLazy();
            
            Container.Bind<IFakeServerService>()
                .To<FakeWarServer>()
                .AsSingle();
            
            Container.Bind<IGameService>()
                .To<GameService>()
                .AsSingle();
            
            Debug.Log("[ProjectInstaller] Global services bound successfully");
        }
        
        private void InstallAsyncManagement()
        {
            Container.Bind<AsyncOperationManager>()
                .AsSingle()
                .NonLazy();
            
            Container.Bind<IDisposable>()
                .To<AsyncOperationManager>()
                .FromResolve()
                .WhenInjectedInto<AsyncOperationManager>();
            
            Debug.Log("[ProjectInstaller] AsyncOperationManager bound successfully");
        }
        
        private void InstallSignals()
        {
            SignalBusInstaller.Install(Container);
            
            Container.DeclareSignal<GameStartEvent>();
            Container.DeclareSignal<RoundStartEvent>();
            Container.DeclareSignal<RoundCompleteEvent>();
            Container.DeclareSignal<GameEndEvent>();
            Container.DeclareSignal<WarStartEvent>();
            Container.DeclareSignal<GameStateChangedEvent>();
            Container.DeclareSignal<PlayerActionEvent>();
            
            Debug.Log("[ProjectInstaller] Signals declared successfully");
        }
    }
}