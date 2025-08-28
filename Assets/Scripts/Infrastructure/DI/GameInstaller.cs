using UnityEngine;
using Zenject;
using CardWar.Infrastructure.Events;
using CardWar.Services.Game;
using CardWar.Gameplay.Controllers;

namespace CardWar.Infrastructure.Installers
{
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            InstallSignals();
            InstallGameServices();
            InstallGameControllers();
        }
        
        private void InstallSignals()
        {
            SignalBusInstaller.Install(Container);
            
            Container.DeclareSignal<GameStartEvent>();
            Container.DeclareSignal<GameEndEvent>();
            Container.DeclareSignal<RoundStartEvent>();
            Container.DeclareSignal<RoundCompleteSignal>();
            Container.DeclareSignal<WarStartEvent>();
            Container.DeclareSignal<GameStateChangedEvent>();
            Container.DeclareSignal<PlayerActionEvent>();
        }
        
        private void InstallGameServices()
        {
            Container.Bind<IGameService>()
                .To<GameService>()
                .AsSingle()
                .NonLazy();
        }
        
        private void InstallGameControllers()
        {
            Container.BindInterfacesAndSelfTo<GameController>()
                .AsSingle()
                .NonLazy();
        }
    }
}