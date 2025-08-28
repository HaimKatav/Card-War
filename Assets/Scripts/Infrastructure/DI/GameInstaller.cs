using UnityEngine;
using Zenject;
using CardWar.Services.Assets;
using CardWar.Services.Network;
using CardWar.Services.Game;
using CardWar.Infrastructure.Events;
using CardWar.Gameplay.Controllers;
using CardWar.Configuration;
using CardWar.Core.UI;

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
                // Bind GameSettings (global)
                BindGameSettings();
                
                // Bind global services
                BindGlobalServices();
                
                // Bind scene-specific components
                BindSceneComponents();
                
                // Bind prefab references
                BindPrefabReferences();
                
                // Declare signals
                DeclareSignals();
                
                Debug.Log("[GameInstaller] All bindings completed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameInstaller] Binding failed: {ex.Message}");
                throw;
            }
        }
        
        private void BindGameSettings()
        {
            var gameSettings = Resources.Load<GameSettings>("GameSettings");
            if (gameSettings == null)
            {
                Debug.LogError("[GameInstaller] GameSettings not found! Create it first.");
                return;
            }
            
            Container.Bind<GameSettings>().FromInstance(gameSettings).AsSingle();
            Debug.Log("[GameInstaller] GameSettings bound");
        }
        
        private void BindGlobalServices()
        {
            Container.Bind(typeof(IAssetService), typeof(IInitializable))
                .To<AssetService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<IFakeServerService>().To<FakeWarServer>().AsSingle();
            
            Container.Bind<IGameService>().To<GameService>().AsSingle();
            
            Debug.Log("[GameInstaller] Global services bound");
        }
        
        private void BindSceneComponents()
        {
            // Camera and Canvas
            if (_gameCamera != null)
                Container.Bind<Camera>().FromInstance(_gameCamera).AsSingle();
            
            if (_gameCanvas != null)
                Container.Bind<Canvas>().FromInstance(_gameCanvas).AsSingle();
            
            // Find and bind scene managers
            var canvasManager = FindObjectOfType<CanvasManager>();
            if (canvasManager != null)
            {
                Container.BindInterfacesAndSelfTo<CanvasManager>()
                    .FromInstance(canvasManager)
                    .AsSingle();
            }
            
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                Container.BindInterfacesAndSelfTo<UIManager>()
                    .FromInstance(uiManager)
                    .AsSingle();
            }
            
            var cardAnimationController = FindObjectOfType<CardAnimationController>();
            if (cardAnimationController != null)
            {
                Container.Bind<CardAnimationController>()
                    .FromInstance(cardAnimationController)
                    .AsSingle();
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
            Container.DeclareSignal<GameStartEvent>();
            Container.DeclareSignal<RoundCompleteEvent>();
            Container.DeclareSignal<GameEndEvent>();
            Container.DeclareSignal<WarStartEvent>();
            Container.DeclareSignal<GameStateChangedEvent>();
            
            Debug.Log("[GameInstaller] Signals declared");
        }
    }
}
