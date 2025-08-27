using UnityEngine;
using UnityEngine.UI;
using Zenject;
using CardWar.Core.Events;
using CardWar.Services.UI;

namespace CardWar.UI.Screens
{
    public class MainMenuController : MonoBehaviour, IMainMenuController
    {
        [Header("Main Menu UI")]
        [SerializeField] private GameObject _mainMenuScreen;
        [SerializeField] private RectTransform _mainMenuScreenRect;
        [SerializeField] private Button _startGameButton;

        private SignalBus _signalBus;

        public RectTransform GetRectTransform()
        {
            return _mainMenuScreenRect;
        }

        public void Initialize(SignalBus signalBus)
        {
            _signalBus = signalBus;
            SetupButtons();
        }

        private void SetupButtons()
        {
            if (_startGameButton != null)
            {
                _startGameButton.onClick.AddListener(() => _signalBus.Fire<StartGameEvent>());
            }
        }

        public void Show()
        {
            Debug.Log("MainMenuController: Showing main menu screen");
            SetScreenActive(true);
        }

        public void Hide()
        {
            Debug.Log("MainMenuController: Hiding main menu screen");
            SetScreenActive(false);
        }

        private void SetScreenActive(bool active)
        {
            if (_mainMenuScreen != null)
                _mainMenuScreen.SetActive(active);
        }

        private void OnDestroy()
        {
            if (_startGameButton != null)
                _startGameButton.onClick.RemoveAllListeners();
        }
    }
}
