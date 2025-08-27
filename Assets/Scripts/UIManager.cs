using UnityEngine;
using Zenject;
using Cysharp.Threading.Tasks;

public class UIManager : MonoBehaviour, IUIService
{
    [Header("UI Controllers")]
    [SerializeField] private MainMenuController _mainMenuControllerPrefab;
    [SerializeField] private GameUIController _gameUIControllerPrefab;
    [SerializeField] private Transform _uiParentTransform;
    [SerializeField] private Transform _gameplayParentTransform;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private GameObject _loadingPanel;

    private SignalBus _signalBus;
    private DiContainer _container;
    private IMainMenuController _mainMenu;
    private IGameUIController _gameUI;

    [Inject]
    public void Construct(SignalBus signalBus, DiContainer container)
    {
        _signalBus = signalBus;
        _container = container;
    }

    public async UniTask Initialize()
    {
        await InitializeControllersAsync();
        
        ShowMainMenu();
    }
    
    private async UniTask InitializeControllersAsync()
    {
        Debug.Log("UIManager: Initializing controllers...");
        
        if (_uiParentTransform == null)
        {
            Debug.LogError("UI Parent Transform not assigned to UIManager! UI controllers need a parent to display properly.");
            return;
        }
        
        if (_mainMenuControllerPrefab != null)
        {
            _mainMenu = Instantiate(_mainMenuControllerPrefab, _uiParentTransform);
            SetupRectTransform(_mainMenu.GetRectTransform());
            _mainMenu.Initialize(_signalBus);
        }
        else
        {
            Debug.LogError("MainMenuController prefab is null!");
        }
        
        // Small delay to ensure everything is properly initialized
        await UniTask.DelayFrame(1);
        
        Debug.Log("UIManager: Controllers initialization completed");
    }

    public void ShowMainMenu()
    {
        Debug.Log("UIManager: Showing main menu...");
        _mainMenu.Show();
        SetLoadingPanelActive(false);
        Debug.Log("UIManager: Main menu shown");
    }

    public void ShowGameplay()
    {
        Debug.Log("UIManager: Showing gameplay...");

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
        
        if (_gameUIControllerPrefab != null)
        {
            // Instantiate the prefab normally
            _gameUI = Instantiate(_gameUIControllerPrefab, _gameplayParentTransform);
            SetupRectTransform(_gameUI.GetRectTransform());
            
            // Inject dependencies using the container
            _container.Inject(_gameUI);
            
            // Update the service to communicate directly with GameUI
            UpdateGameServiceReference();
            
            Debug.Log("UIManager: GameUIController created successfully");
        }
        else
        {
            Debug.LogError("GameUIController prefab is null!");
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
            Debug.LogWarning("UIManager: GameUI is null, cannot fire signal");
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
            Debug.LogError("UIManager: Can't setup RectTransform without a RectTransform!");
            return;
        }
        
        uiObjectRect.anchorMin = Vector2.zero;
        uiObjectRect.anchorMax = Vector2.one;
        uiObjectRect.offsetMin = Vector2.zero;
        uiObjectRect.offsetMax = Vector2.zero;
        uiObjectRect.localScale = Vector3.one;
        uiObjectRect.localPosition = Vector3.zero;

        Debug.Log($"UIManager: Setup RectTransform for {uiObjectRect.name}");
    }
}