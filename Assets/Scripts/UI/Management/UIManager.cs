using UnityEngine;

namespace CardWar.UI.Management
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private Transform _playerArea;
        [SerializeField] private Transform _opponentArea;
        [SerializeField] private GameObject _gameplayScreen;
        [SerializeField] private GameObject _mainMenuScreen;

        public void ShowGameplayScreen()
        {
            if (_gameplayScreen != null) _gameplayScreen.SetActive(true);
            if (_mainMenuScreen != null) _mainMenuScreen.SetActive(false);
        }

        public Transform GetPlayerArea() => _playerArea;
        public Transform GetOpponentArea() => _opponentArea;
    }
}
