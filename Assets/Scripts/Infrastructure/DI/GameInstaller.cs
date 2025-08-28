using UnityEngine;
using Zenject;
using CardWar.Infrastructure.Events;
using CardWar.Services.Game;
using CardWar.Gameplay.Controllers;
using CardWar.Gameplay.Animation;
using CardWar.UI;
using CardWar.UI.Cards;

namespace CardWar.Infrastructure.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [Header("UI Prefabs")]
        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private Transform _cardPoolContainer;
        
        public override void InstallBindings()
        {
            InstallEvents();
            InstallGameServices();
            InstallGameControllers();
            InstallUI();
            InstallPools();
        }
        
        private void InstallEvents()
        {
            SignalBusInstaller.Install(Container);
            
            Container.DeclareSignal<GameStartEvent>();
            Container.DeclareSignal<GameEndEvent>();
            Container.DeclareSignal<RoundStartEvent>();
            Container.DeclareSignal<RoundCompleteEvent>();
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
        
        private void InstallUI()
        {
            // UIManager should be attached to a GameObject in the scene
            Container.BindInterfacesAndSelfTo<UIManager>()
                .FromComponentInHierarchy()
                .AsSingle();
            
            // CardAnimationController handles all card animations
            Container.BindInterfacesAndSelfTo<CardAnimationController>()
                .FromComponentInHierarchy()
                .AsSingle();
        }
        
        private void InstallPools()
        {
            // Card pool for efficient card spawning
            if (_cardPrefab != null)
            {
                Container.BindMemoryPool<CardViewController, CardViewController.Pool>()
                    .WithInitialSize(10)
                    .FromComponentInNewPrefab(_cardPrefab)
                    .UnderTransform(_cardPoolContainer);
            }
            else
            {
                Debug.LogWarning("[GameInstaller] Card prefab not assigned. Card pool will not be created.");
            }
        }
    }
}