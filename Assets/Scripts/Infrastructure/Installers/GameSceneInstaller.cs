using CardWar.Core.Events;
using CardWar.Core.GameLogic;
using CardWar.Gameplay.Cards;
using CardWar.Services.Assets;
using CardWar.Services.Network;
using CardWar.UI.Management;
using UnityEngine;
using Zenject;

namespace CardWar.Infrastructure
{
    public class GameSceneInstaller : MonoInstaller
    {
        [Header("Prefab References")]
        [SerializeField] private CardView _cardPrefab;

        public override void InstallBindings()
        {
            SignalBusInstaller.Install(Container);
            Container.DeclareSignal<GameStartSignal>();
            Container.DeclareSignal<CardPlayedSignal>();
            Container.DeclareSignal<GameEndSignal>();

            Container.Bind<IAssetService>().To<SimpleAssetService>().AsSingle();
            Container.Bind<FakeWarServer>().AsSingle();
            Container.Bind<GameService>().AsSingle();

            Container.BindMemoryPool<CardView, CardView.Pool>()
                .WithInitialSize(16)
                .FromComponentInNewPrefab(_cardPrefab)
                .UnderTransformGroup("CardPool");

            Container.Bind<UIManager>().FromComponentInHierarchy().AsSingle();

            Container.BindInterfacesAndSelfTo<GameManager>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();
        }
    }
}
