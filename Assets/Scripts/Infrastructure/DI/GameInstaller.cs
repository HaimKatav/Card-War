using UnityEngine;
using Zenject;
using CardWar.Gameplay.Controllers;
using CardWar.Core.UI;
using CardWar.Core;

namespace CardWar.Infrastructure.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [Header("Prefab References - Assign in Inspector")]
        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private Transform _cardPoolContainer;
        
        public override void InstallBindings()
        {
            Debug.Log("[GameInstaller] Starting binding process...");
            
            try
            {
                BindSceneComponents();
                BindPrefabReferences();
                DeclareSignals();
                
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
            }
            else
            {
                Debug.LogError("[GameInstaller] GameInteractionController not found in scene!");
            }
            
            Debug.Log("[GameInstaller] Scene components bound");
        }
        
        private void BindPrefabReferences()
        {
            if (_cardPrefab != null)
                Container.Bind<GameObject>().WithId("CardPrefab").FromInstance(_cardPrefab);
            else
                Debug.LogWarning("[GameInstaller] CardPrefab not assigned in Inspector");
            
            if (_cardPoolContainer != null)
                Container.Bind<Transform>().WithId("CardPoolContainer").FromInstance(_cardPoolContainer);
            else
                Debug.LogWarning("[GameInstaller] CardPoolContainer not assigned in Inspector");
            
            Debug.Log("[GameInstaller] Prefab references bound");
        }
        
        private void DeclareSignals()
        {
            Debug.Log("[GameInstaller] Signals declared (already handled by ProjectContext)");
        }
    }
}