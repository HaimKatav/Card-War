using UnityEngine;
using Zenject;
using CardWar.Gameplay.Controllers;
using CardWar.Core.UI;
using CardWar.Core;

namespace CardWar.Infrastructure.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [Header("Scene References - Assign in Inspector")]
        [SerializeField] private Camera _gameCamera;
        [SerializeField] private Canvas _gameCanvas;
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
            if (_gameCamera != null)
                Container.Bind<Camera>().FromInstance(_gameCamera).AsSingle();
            
            if (_gameCanvas != null)
                Container.Bind<Canvas>().FromInstance(_gameCanvas).AsSingle();
            
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
            
            if (_cardPoolContainer != null)
                Container.Bind<Transform>().WithId("CardPoolContainer").FromInstance(_cardPoolContainer);
            
            Debug.Log("[GameInstaller] Prefab references bound");
        }
        
        private void DeclareSignals()
        {
            Debug.Log("[GameInstaller] Signals declared (already handled by ProjectContext)");
        }
    }
}