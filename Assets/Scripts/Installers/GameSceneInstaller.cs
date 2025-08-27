using Assets.Scripts.Player;
using Assets.Scripts.Services;
using UnityEngine;
using Zenject;
using Assets.Scripts.Services.AssetManagement;
using CardWar.Core.Events;
using CardWar.Services.Game;
using CardWar.Services.Network;
using CardWar.View.Cards;

namespace CardWar.Installers
{
    public class GameSceneInstaller : MonoInstaller
    {
        [Header("Settings")]
        [SerializeField] private float _defaultAnimationDuration = 0.5f;

        [Header("Asset Paths")]
        [SerializeField] private string _cardPrefabPath = "Prefabs/Cards/CardView";
        
        [Header("Scene References")]
        [SerializeField] private Transform _playersParent;

        public override void InstallBindings()
        {
            Debug.Log("GameSceneInstaller: Starting installation...");

            SignalBusInstaller.Install(Container);
            
            DeclareSignals();

            BindAssetManagement();

            BindSceneManagers();

            BindServices();

            BindFactories();

            BindGameManager();

            Debug.Log("GameSceneInstaller: Installation completed successfully!");
        }

        private void DeclareSignals()
        {
            Container.DeclareSignal<StartGameEvent>();
            Container.DeclareSignal<RoundStartedEvent>();
            Container.DeclareSignal<RoundCompletedEvent>();
            Container.DeclareSignal<GameEndedEvent>();
            Container.DeclareSignal<NetworkErrorEvent>();
            Container.DeclareSignal<GameUIControllerReadySignal>();
            Container.DeclareSignal<ReturnToMenuEvent>();
            Container.DeclareSignal<WarStartedEvent>();
            Container.DeclareSignal<PoolResizeEvent>();
            Container.DeclareSignal<PlayerInputSignal>();
            Container.DeclareSignal<AIReadyToPlaySignal>();
        }

        private void BindAssetManagement()
        {
            // Bind AssetManager as singleton service
            Container.Bind<IAssetManager>()
                .To<AssetManager>()
                .AsSingle()
                .NonLazy(); // Create immediately for preloading

            Debug.Log("Asset management bound successfully");
        }

        private void BindSceneManagers()
        {
            // Find existing managers in scene
            CardWar.Services.UI.UIManager uiManager = FindObjectOfType<CardWar.Services.UI.UIManager>();

            if (uiManager == null)
            {
                Debug.LogError("UIManager not found in scene! Please add it manually.");
                return;
            }

            Container.Bind<CardWar.Services.UI.UIManager>().FromInstance(uiManager).AsSingle();
            Container.Bind<IUIService>().FromInstance(uiManager).AsSingle();
            
            // Bind scene references
            if (_playersParent == null)
            {
                // Create a parent for players if not assigned
                _playersParent = new GameObject("Players").transform;
            }
            
            Container.Bind<Transform>()
                .WithId("PlayersParent")
                .FromInstance(_playersParent)
                .AsSingle();

            Debug.Log("Scene managers bound successfully");
        }

        private void BindServices()
        {
            Container.Bind<IGameService>()
                .To<CardWar.Services.Game.GameService>()
                .AsSingle()
                .NonLazy();

            // Animation is now a controller, not a service - created per use case
            Container.Bind<IAnimationService>()
                .To<AnimationService>()
                .AsSingle()
                .WithArguments(_defaultAnimationDuration);

            Container.Bind<IFakeServerService>()
                .To<CardWar.Services.Network.FakeWarServer>()
                .AsSingle()
                .NonLazy();
                
            // Bind WarResolver as service
            Container.Bind<CardWar.Services.Network.WarResolver>()
                .AsSingle()
                .NonLazy();
                
            // Bind WarAnimationController as service
            Container.Bind<WarAnimationController>()
                .AsSingle()
                .NonLazy();

            Debug.Log("Services bound successfully");
        }

        private void BindFactories()
        {
            // Bind async card factory
            Container.Bind<AsyncCardViewFactory>()
                .AsSingle()
                .WithArguments(_cardPrefabPath);

            // Bind player controller factory
            Container.Bind<Assets.Scripts.Player.PlayerControllerFactory>()
                .AsSingle();
            
            // Bind card pool
            Container.BindMemoryPool<CardView, CardView.Pool>()
                .WithInitialSize(16) // Minimum pool size
                .ExpandByDoubling()  // Double size when expanding
                .To<CardWar.View.Cards.CardView>()
                .FromComponentInNewPrefab(GetCardPrefab())
                .UnderTransformGroup("CardPool");

            Debug.Log("Factories bound successfully");
        }
        
        private GameObject GetCardPrefab()
        {
            // Load card prefab for pool
            // In production, this would be loaded via AssetManager
            var prefab = Resources.Load<GameObject>(_cardPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Card prefab not found at path: {_cardPrefabPath}");
                            // Create a simple fallback prefab
            prefab = new GameObject("CardView");
            prefab.AddComponent<CardWar.View.Cards.CardView>();
            }
            return prefab;
        }

        private void BindGameManager()
        {
            // Create GameManager and bind as both concrete type and interfaces
            Container.Bind<CardWar.Controllers.Game.GameManager>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .NonLazy();

            // Bind GameManager interfaces
            Container.Bind<IInitializable>()
                .To<CardWar.Controllers.Game.GameManager>()
                .FromResolve();

            Debug.Log("GameManager bound successfully");
        }
    }
}