using UnityEngine;
using Zenject;
using CardWar.Gameplay.Controllers;
using CardWar.Core.UI;
using CardWar.Core;
using CardWar.Infrastructure.Factories;
using CardWar.UI.Cards;

namespace CardWar.Infrastructure.Installers
{
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Debug.Log("[GameInstaller] Starting binding process...");
            
            try
            {
                BindSceneComponents();
                BindCardFactory();
                
                Debug.Log("[GameInstaller] All bindings completed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameInstaller] Binding failed: {ex.Message}");
                throw;
            }
        }
        
        private void BindSceneComponents()
        {
            var gameCamera = Camera.main;
            if (gameCamera != null)
            {
                Container.Bind<Camera>().FromInstance(gameCamera).AsSingle();
                Debug.Log("[GameInstaller] Camera bound successfully");
            }
            else
            {
                Debug.LogError("[GameInstaller] Camera not found in scene!");
            }
            
            var gameCanvas = FindObjectOfType<Canvas>();
            if (gameCanvas != null)
            {
                Container.Bind<Canvas>().FromInstance(gameCanvas).AsSingle();
                Debug.Log("[GameInstaller] Canvas bound successfully");
            }
            else
            {
                Debug.LogError("[GameInstaller] Canvas not found in scene!");
            }
            
            var canvasManager = FindObjectOfType<CanvasManager>();
            if (canvasManager != null)
            {
                Container.BindInterfacesAndSelfTo<CanvasManager>()
                    .FromInstance(canvasManager)
                    .AsSingle();
                Debug.Log("[GameInstaller] CanvasManager bound successfully");
            }
            else
            {
                Debug.LogError("[GameInstaller] CanvasManager not found in scene!");
            }
            
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                Container.BindInterfacesAndSelfTo<UIManager>()
                    .FromInstance(uiManager)
                    .AsSingle();
                Debug.Log("[GameInstaller] UIManager bound successfully");
            }
            else
            {
                Debug.LogError("[GameInstaller] UIManager not found in scene!");
            }
            
            var cardAnimationController = FindObjectOfType<CardAnimationController>();
            if (cardAnimationController != null)
            {
                Container.BindInterfacesAndSelfTo<CardAnimationController>()
                    .FromInstance(cardAnimationController)
                    .AsSingle();
                Debug.Log("[GameInstaller] CardAnimationController bound successfully");
            }
            else
            {
                Debug.LogError("[GameInstaller] CardAnimationController not found in scene!");
            }
            
            var gameInteractionController = FindObjectOfType<GameInteractionController>();
            if (gameInteractionController != null)
            {
                Container.BindInterfacesAndSelfTo<GameInteractionController>()
                    .FromInstance(gameInteractionController)
                    .AsSingle();
                Debug.Log("[GameInstaller] GameInteractionController bound successfully");
            }
            else
            {
                Debug.LogError("[GameInstaller] GameInteractionController not found in scene!");
            }
            
            Debug.Log("[GameInstaller] Scene components bound");
        }
        
        private void BindCardFactory()
        {
            var poolContainer = GameObject.Find("CardPoolContainer");
            if (poolContainer == null)
            {
                poolContainer = new GameObject("CardPoolContainer");
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    poolContainer.transform.SetParent(canvas.transform, false);
                }
            }
            
            Container.Bind<Transform>()
                .WithId("CardPoolContainer")
                .FromInstance(poolContainer.transform)
                .AsSingle();
            
            Container.Bind(typeof(ICardViewFactory), typeof(IInitializable))
                .To<CardViewFactory>()
                .AsSingle()
                .NonLazy();
            
            Container.Bind<CardViewController.Pool>()
                .FromMethod(CreateCardPool)
                .AsSingle();
            
            Debug.Log("[GameInstaller] Card factory and pool bound successfully");
        }
        
        private CardViewController.Pool CreateCardPool(InjectContext context)
        {
            var factory = context.Container.Resolve<ICardViewFactory>();
            return new CardViewControllerPool(factory);
        }
        
        private class CardViewControllerPool : CardViewController.Pool
        {
            private readonly ICardViewFactory _factory;
            
            public CardViewControllerPool(ICardViewFactory factory)
            {
                _factory = factory;
            }
            
            public override CardViewController Spawn()
            {
                var card = _factory.Create();
                card.OnSpawned(this);
                return card;
            }
            
            public override void Despawn(CardViewController item)
            {
                if (item != null)
                {
                    item.OnDespawned();
                    _factory.Return(item);
                }
            }
            
            public override void Clear()
            {
                _factory.Clear();
            }
        }
    }
}