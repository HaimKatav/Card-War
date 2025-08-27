using UnityEngine;
using Zenject;
using Cysharp.Threading.Tasks;
using CardWar.Services.Assets;

namespace CardWar.Services.UI
{
    public class UIManager : MonoBehaviour, IUIService, System.IDisposable
    {
        [Header("UI Asset Paths")]
        [SerializeField] private string _mainMenuPrefabPath = "Prefabs/UI/MainMenuScreen";
        [SerializeField] private string _gameUIPrefabPath = "Prefabs/UI/GameplayUI";

        [Header("UI Parents")]
        [SerializeField] private Transform _uiParentTransform;
        [SerializeField] private GameObject _loadingPanel;

        private SignalBus _signalBus;
        private DiContainer _container;
        private IAssetManager _assetManager;

        private IMainMenuController _mainMenu;
        private IGameUIController _gameUI;

        [Inject]
        public void Construct(SignalBus signalBus, DiContainer container, IAssetManager assetManager)
        {
            _signalBus = signalBus;
            _container = container;
            _assetManager = assetManager;
        }

        public async UniTask Initialize()
        {
            var mainMenuGO = await _assetManager.InstantiateAsync<GameObject>(_mainMenuPrefabPath, _uiParentTransform);
            _mainMenu = mainMenuGO.GetComponent<IMainMenuController>();
            var gameUIGO = await _assetManager.InstantiateAsync<GameObject>(_gameUIPrefabPath, _uiParentTransform);
            _gameUI = gameUIGO.GetComponent<IGameUIController>();
            ShowMainMenu();
        }

        public async UniTask CreateGameUIControllerAsync() => await UniTask.CompletedTask;

        public void ShowMainMenu()
        {
            _mainMenu?.Show();
            _gameUI?.Hide();
        }

        public void ShowGameplay()
        {
            _mainMenu?.Hide();
            _gameUI?.Show();
        }

        public void ShowLoading(bool show)
        {
            _loadingPanel?.SetActive(show);
        }

        public async UniTask DestroyGameUIControllerAsync() => await UniTask.CompletedTask;

        public async UniTask ReturnToMainMenuAsync()
        {
            ShowMainMenu();
            await UniTask.CompletedTask;
        }

        public Transform GetPlayerArea() => _gameUI?.GetPlayerArea();
        public Transform GetOpponentArea() => _gameUI?.GetOpponentArea();

        public void Dispose() { }
    }
}
