using CardWar.Infrastructure.Events;
using CardWar.Services.Assets;
using CardWar.Services.Network;
using CardWar.Services.Game;
using Zenject;

namespace CardWar.Infrastructure.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Asset Service
            Container.Bind<IAssetService>().To<AssetService>().AsSingle().NonLazy();
            Container.Bind<IInitializable>().To<AssetService>().AsSingle();
            
            // Network Service
            Container.Bind<IFakeServerService>().To<FakeWarServer>().AsSingle();
            
            // Game Service
            Container.Bind<IGameService>().To<GameService>().AsSingle();
            
            // Signals
            Container.DeclareSignal<GameStartEvent>();
            Container.DeclareSignal<RoundCompleteEvent>();
            Container.DeclareSignal<GameEndEvent>();
            Container.DeclareSignal<WarStartEvent>();
        }
    }
}