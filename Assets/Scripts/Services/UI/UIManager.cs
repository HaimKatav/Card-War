using UnityEngine;
using Zenject;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using Assets.Scripts.Services.AssetManagement;
using CardWar.Core.Events;
using CardWar.Controllers.UI;

namespace CardWar.Services.UI
{
    public class UIManager : MonoBehaviour, IUIService, IDisposable
    {
        [Header("UI Asset Paths")]
        [SerializeField] private string _mainMenuPrefabPath = "Prefabs/UI/MainMenuScreen";
        [SerializeField] private string _gameUIPrefabPath = "Prefabs/UI/GameplayUI";
        
        [Header("UI Parents")]
        [SerializeField] private Transform _uiParentTransform;
        [SerializeField] private Transform _gameplayParentTransform;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private GameObject _loadingPanel;

        private SignalBus _signalBus;
        private DiContainer _container;
        private IAssetManager _assetManager;
        
        private IMainMenuController _mainMenu;
        private IGameUIController _gameUI;
        
        private IAssetRequest<GameObject> _mainMenuRequest;
        private IAssetRequest<GameObject> _gameUIRequest;
        private CancellationTokenSource _cancellationTokenSource;

        [Inject]
        public void Construct(SignalBus signalBus, DiContainer container, IAssetManager assetManager)
        {
            _signalBus = signalBus;
            _container = container;
            _assetManager = assetManager;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async UniTask Initialize()
        {
            try
            {
                await InitializeControllersAsync();
                ShowMainMenu();
            }
            catch (Exception ex)
            {
                Debug.LogError($"UIManager: Failed to initialize - {ex.Message}");
                // TODO: Show error UI to user
                throw;
            }
        }
        
        private async UniTask InitializeControllersAsync()
        {
            Debug.Log("UIManager: Initializing controllers...");
            
            if (_uiParentTransform == null)
            {
                var error = "UI Parent Transform not assigned to UIManager!";
                Debug.LogError(error);
                throw new InvalidOperationException(error);
            }

            try
            {
                // Load main menu using AssetManager
                _mainMenuRequest = _assetManager.CreateLoadRequest<GameObject>(_mainMenuPrefabPath);
                
                // Track loading progress
                _mainMenuRequest.OnProgress += (progress) => 
                {
                    Debug.Log($"UIManager: Loading main menu... {progress:P0}");
                };
                
                _mainMenuRequest.OnError += (error) =>
                {
                    Debug.LogError($"UIManager: Failed to load main menu - {error}");
                };

                var mainMenuPrefab = await _mainMenuRequest.LoadAsync(_cancellationTokenSource.Token);
                
                if (mainMenuPrefab == null)
                {
                    throw new AssetLoadException($"Failed to load main menu prefab from path: {_mainMenuPrefabPath}");
                }

                // Instantiate and get component without GetComponent call
                var mainMenuGO = Instantiate(mainMenuPrefab, _uiParentTransform);
                _mainMenu = mainMenuGO.GetComponent<IMainMenuController>();
                
                if (_mainMenu == null)
                {
                    Destroy(mainMenuGO);
                    throw new InvalidOperationException(
                        $"Main menu prefab does not have IMainMenuController component");
                }

                SetupRectTransform(_mainMenu.GetRectTransform());
                _mainMenu.Initialize(_signalBus);
                
                Debug.Log("UIManager: Main menu initialized successfully");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("UIManager: Initialization cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"UIManager: Failed to initialize controllers - {ex.Message}");
                // TODO: Implement retry mechanism
                throw;
            }
            
            // Small delay to ensure everything is properly initialized
            await UniTask.DelayFrame(1);
            
            Debug.Log("UIManager: Controllers initialization completed");
        }

        public void ShowMainMenu()
        {
            Debug.Log("UIManager: Showing main menu...");
            
            if (_mainMenu == null)
            {
                throw new InvalidOperationException("Main menu controller is not initialized");
            }
            
            _mainMenu.Show();
            SetLoadingPanelActive(false);
            Debug.Log("UIManager: Main menu shown");
        }

        public void ShowGameplay()
        {
            Debug.Log("UIManager: Showing gameplay...");

            if (_gameUI == null)
            {
                throw new InvalidOperationException("Game UI controller is not initialized. Call CreateGameUIControllerAsync first.");
            }

            _mainMenu.Hide();
            _gameUI.Show();
            SetLoadingPanelActive(false);
            Debug.Log("UIManager: Gameplay shown");
        }

        public void ShowLoading(bool isLoading)
        {
            SetLoadingPanelActive(isLoading);
        }

        public async UniTask CreateGameUIControllerAsync()
        {
            if (_gameUI != null)
            {
                Debug.Log("UIManager: GameUIController already exists");
                return;
            }

            Debug.Log("UIManager: Creating GameUIController...");
            
            try
            {
                // Create load request for game UI
                _gameUIRequest = _assetManager.CreateLoadRequest<GameObject>(_gameUIPrefabPath);
                
                _gameUIRequest.OnProgress += (progress) => 
                {
                    Debug.Log($"UIManager: Loading game UI... {progress:P0}");
                };
                
                _gameUIRequest.OnError += (error) =>
                {
                    Debug.LogError($"UIManager: Failed to load game UI - {error}");
                    // TODO: Show error dialog with retry option
                };

                var gameUIPrefab = await _gameUIRequest.LoadAsync(_cancellationTokenSource.Token);
                
                if (gameUIPrefab == null)
                {
                    throw new AssetLoadException($"Failed to load game UI prefab from path: {_gameUIPrefabPath}");
                }

                // Instantiate using AssetManager for proper tracking
                var gameUIInstance = await _assetManager.InstantiateAsync<CardWar.Controllers.UI.GameUIController>(
                    _gameUIPrefabPath, 
                    _gameplayParentTransform,
                    _cancellationTokenSource.Token);
                
                _gameUI = gameUIInstance;
                SetupRectTransform(_gameUI.GetRectTransform());
                
                // Inject dependencies
                _container.Inject(_gameUI);
                
                // Update the service reference
                UpdateGameServiceReference();
                
                Debug.Log("UIManager: GameUIController created successfully");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("UIManager: Game UI creation cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"UIManager: Failed to create GameUIController - {ex.Message}");
                // TODO: Implement retry dialog for user
                throw;
            }
        }

        public async UniTask ReturnToMainMenuAsync()
        {
            Debug.Log("UIManager: Returning to main menu...");
            
            // Destroy game UI if it exists
            if (_gameUI != null)
            {
                await DestroyGameUIControllerAsync();
            }
            
            // Show main menu
            ShowMainMenu();
            
            // Fire signal for game cleanup
            _signalBus.Fire<ReturnToMenuEvent>();
        }
        
        public async UniTask DestroyGameUIControllerAsync()
        {
            if (_gameUI == null)
            {
                Debug.Log("UIManager: No GameUIController to destroy");
                return;
            }

            Debug.Log("UIManager: Destroying GameUIController...");
            
            try
            {
                // Hide the UI first
                _gameUI.Hide();
                
                // Allow one frame for cleanup
                await UniTask.DelayFrame(1);
                
                // Get the GameObject to destroy
                if (_gameUI is Component component)
                {
                    _assetManager.ReleaseInstance(component.gameObject, releaseAsset: false);
                }
                
                _gameUI = null;
                _gameUIRequest = null;
                
                Debug.Log("UIManager: GameUIController destroyed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"UIManager: Failed to destroy GameUIController - {ex.Message}");
            }
        }

        private void UpdateGameServiceReference()
        {
            if (_gameUI != null)
            {
                _signalBus.Fire(new GameUIControllerReadySignal(_gameUI));
                Debug.Log("UIManager: Fired GameUIControllerReadySignal");
            }
            else
            {
                var error = "GameUI is null, cannot fire signal";
                Debug.LogError($"UIManager: {error}");
                throw new InvalidOperationException(error);
            }
        }

        private void SetLoadingPanelActive(bool active)
        {
            if (_loadingPanel != null)
                _loadingPanel.SetActive(active);
        }

        private void SetupRectTransform(RectTransform uiObjectRect)
        {
            if (uiObjectRect == null)
            {
                throw new ArgumentNullException(nameof(uiObjectRect), 
                    "Cannot setup RectTransform with null reference");
            }
            
            uiObjectRect.anchorMin = Vector2.zero;
            uiObjectRect.anchorMax = Vector2.one;
            uiObjectRect.offsetMin = Vector2.zero;
            uiObjectRect.offsetMax = Vector2.zero;
            uiObjectRect.localScale = Vector3.one;
            uiObjectRect.localPosition = Vector3.zero;

            Debug.Log($"UIManager: Setup RectTransform for {uiObjectRect.name}");
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            Debug.Log("UIManager: Disposing...");
            
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            _mainMenuRequest?.Cancel();
            _gameUIRequest?.Cancel();
            
            // Destroy UI elements if they exist
            if (_mainMenu is Component mainMenuComponent)
            {
                Destroy(mainMenuComponent.gameObject);
            }
            
            if (_gameUI is Component gameUIComponent)
            {
                Destroy(gameUIComponent.gameObject);
            }
            
            Debug.Log("UIManager: Disposed");
        }
    }
}