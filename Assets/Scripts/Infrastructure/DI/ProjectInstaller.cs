using CardWar.Configuration;
using UnityEngine;
using Zenject;
using CardWar.Services.Assets;
using CardWar.Services.Network;
using CardWar.Services.Game;
using CardWar.Infrastructure.Events;

namespace CardWar.Infrastructure.Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        private GameSettings _gameSettings;

        public override void InstallBindings()
        {

            _gameSettings = Resources.Load<GameSettings>("Settings/GameSettings");
            if (_gameSettings == null)
            {
                Debug.LogError("[ProjectInstaller] GameSettings not found in Resources!");
                return;
            }

            // Global Services
            Container.Bind<GameSettings>().FromInstance(_gameSettings).AsSingle();

            Container.Bind(typeof(IAssetService), typeof(IInitializable))
                .To<AssetService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<IFakeServerService>().To<FakeWarServer>().AsSingle();
            Container.Bind<IGameService>().To<GameService>().AsSingle();

            // Event System
            Container.DeclareSignal<GameStartEvent>();
            Container.DeclareSignal<RoundCompleteEvent>();
            Container.DeclareSignal<GameEndEvent>();
            Container.DeclareSignal<WarStartEvent>();
            Container.DeclareSignal<GameStateChangedEvent>();

            Debug.Log("[ProjectInstaller] Global services bound successfully");
        }
    }
}