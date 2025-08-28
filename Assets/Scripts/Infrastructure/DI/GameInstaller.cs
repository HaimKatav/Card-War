using UnityEngine;
using Zenject;
using CardWar.Gameplay.Controllers;
using CardWar.Core.UI;
using CardWar.Core;
using CardWar.Infrastructure.Factories;
using CardWar.UI.Cards;
using CardWar.Configuration;

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
                BindCardSystem();
                
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
        
        private void BindCardSystem()
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
            
            var gameSettings = Container.Resolve<GameSettings>();
            int poolSize = gameSettings != null ? gameSettings.cardPoolInitialSize : 20;
            
            Container.BindMemoryPool<CardViewController, CardViewController.Pool>()
                .WithInitialSize(poolSize)
                .FromComponentInNewPrefab(GetCardPrefab())
                .UnderTransform(poolContainer.transform);
            
            Debug.Log($"[GameInstaller] Card system bound with pool size: {poolSize}");
        }
        
        private GameObject GetCardPrefab()
        {
            var factory = Container.Resolve<ICardViewFactory>() as CardViewFactory;
            if (factory != null)
            {
                return factory.GetCardPrefab();
            }
            
            var gameSettings = Container.Resolve<GameSettings>();
            if (gameSettings != null)
            {
                var prefabPath = gameSettings.cardPrefabPath;
                if (prefabPath.StartsWith("Prefabs/"))
                {
                    prefabPath = prefabPath.Substring("Prefabs/".Length);
                }
                
                var prefab = Resources.Load<GameObject>($"Prefabs/{prefabPath}");
                if (prefab != null)
                {
                    return prefab;
                }
            }
            
            Debug.LogError("[GameInstaller] Could not load card prefab, creating fallback");
            return CreateFallbackCardPrefab();
        }
        
        private GameObject CreateFallbackCardPrefab()
        {
            var fallback = new GameObject("CardPrefab");
            var rectTransform = fallback.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 180);
            
            fallback.AddComponent<CanvasGroup>();
            var cardView = fallback.AddComponent<CardViewController>();
            
            var frontObject = new GameObject("CardFront");
            frontObject.transform.SetParent(fallback.transform, false);
            frontObject.AddComponent<UnityEngine.UI.Image>();
            
            var backObject = new GameObject("CardBack");
            backObject.transform.SetParent(fallback.transform, false);
            backObject.AddComponent<UnityEngine.UI.Image>();
            
            return fallback;
        }
    }
}