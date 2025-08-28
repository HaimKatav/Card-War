using UnityEngine;
using Zenject;
using CardWar.Core.Async;
using CardWar.Services.Network;

namespace CardWar.Infrastructure.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private NetworkErrorConfig _networkErrorConfig;
        
        public override void InstallBindings()
        {
            InstallAsyncManagement();
            InstallNetworkServices();
        }
        
        private void InstallAsyncManagement()
        {
            Container.BindInterfacesAndSelfTo<AsyncOperationManager>()
                .AsSingle()
                .NonLazy();
        }
        
        private void InstallNetworkServices()
        {
            if (_networkErrorConfig == null)
            {
                _networkErrorConfig = CreateDefaultNetworkConfig();
            }
            
            Container.Bind<NetworkErrorConfig>()
                .FromInstance(_networkErrorConfig)
                .AsSingle();
            
            Container.Bind<IFakeServerService>()
                .To<FakeWarServer>()
                .AsSingle()
                .NonLazy();
        }
        
        private NetworkErrorConfig CreateDefaultNetworkConfig()
        {
            return new NetworkErrorConfig
            {
                timeoutRate = 0.02f,
                networkErrorRate = 0.05f,
                serverErrorRate = 0.01f,
                corruptionRate = 0.005f,
                minNetworkDelay = 0.1f,
                maxNetworkDelay = 0.5f,
                timeoutDuration = 5.0f,
                retryBaseDelay = 1.0f
            };
        }
    }
}