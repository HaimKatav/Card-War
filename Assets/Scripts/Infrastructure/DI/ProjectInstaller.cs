using CardWar.Configuration;
using UnityEngine;
using Zenject;
using CardWar.Services.Assets;
using CardWar.Services.Network;
using CardWar.Services.Game;
using CardWar.Infrastructure.Events;
using CardWar.Core.Async;
using CardWar.UI.Cards;

namespace CardWar.Infrastructure.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private GameSettings _gameSettings;
        [SerializeField] private NetworkErrorConfig _networkErrorConfig;
        
        public override void InstallBindings()
        {
            Debug.Log("[ProjectInstaller] Starting global service binding...");
            
            try
            {
                BindGameSettings();
                BindNetworkConfig();
                BindGlobalServices();
                BindAsyncManager();
                BindCardPools();
                DeclareSignals();
                
                Debug.Log("[ProjectInstaller] Global services bound successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ProjectInstaller] Binding failed: {ex.Message}");
                throw;
            }
        }
        
        private void BindGameSettings()
        {
            if (_gameSettings == null)
            {
                _gameSettings = Resources.Load<GameSettings>("Settings/GameSettings");
                if (_gameSettings == null)
                {
                    Debug.LogError("[ProjectInstaller] GameSettings not found! Please create it using: Assets -> Create -> CardWar -> Game Settings");
                    return;
                }
            }
            
            Container.Bind<GameSettings>().FromInstance(_gameSettings).AsSingle();
            Debug.Log("[ProjectInstaller] GameSettings bound successfully");
        }
        
        private void BindNetworkConfig()
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
            
            Container.Bind<NetworkErrorConfig>().FromInstance(_networkErrorConfig).AsSingle();
            Debug.Log("[ProjectInstaller] NetworkErrorConfig bound successfully");
        }
        
        private NetworkErrorConfig CreateDefaultNetworkConfig()
        {
            var config = ScriptableObject.CreateInstance<NetworkErrorConfig>();
            
            // Set reasonable defaults
            config.timeoutRate = 0.01f;
            config.networkErrorRate = 0.02f;
            config.serverErrorRate = 0.005f;
            config.corruptionRate = 0.001f;
            config.minNetworkDelay = 0.1f;
            config.maxNetworkDelay = 0.3f;
            config.timeoutDuration = 5f;
            config.retryBaseDelay = 1f;
            
            return config;
        }
        
        private void BindGlobalServices()
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
        
        private void BindAsyncManager()
        {
            Container.Bind<AsyncOperationManager>()
                .AsSingle()
                .NonLazy();
                
            Debug.Log("[ProjectInstaller] AsyncOperationManager bound successfully");
        }
        
        private void BindCardPools()
        {
            Container.BindMemoryPool<CardViewController, CardViewController.Pool>()
                .WithInitialSize(10)
                .FromComponentInNewPrefabResource("Prefabs/Cards/CardPrefab")
                .UnderTransformGroup("CardPool");
                
            Debug.Log("[ProjectInstaller] Card pools bound successfully");
        }
        
        private void DeclareSignals()
        {
            Container.DeclareSignal<GameStartEvent>();
            Container.DeclareSignal<RoundCompleteEvent>();
            Container.DeclareSignal<GameEndEvent>();
            Container.DeclareSignal<WarStartEvent>();
            Container.DeclareSignal<GameStateChangedEvent>();
            Container.DeclareSignal<PlayerActionEvent>();
            
            Debug.Log("[ProjectInstaller] All signals declared successfully");
        }
    }
}