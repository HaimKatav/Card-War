using UnityEngine;
using CardWar.Configuration;
using CardWar.Core.UI;
using Zenject;

namespace CardWar.UI.Core
{
    public class UILayoutController : MonoBehaviour, IInitializable
    {
        [Header("UI Positions")]
        [SerializeField] private RectTransform _playerScorePanel;
        [SerializeField] private RectTransform _opponentScorePanel;
        [SerializeField] private RectTransform _centerPanel;
        [SerializeField] private RectTransform _gameStatePanel;
        
        private GameSettings _gameSettings;
        private CanvasManager _canvasManager;
        
        [Inject]
        public void Construct(GameSettings gameSettings, CanvasManager canvasManager)
        {
            _gameSettings = gameSettings;
            _canvasManager = canvasManager;
        }
        
        public void Initialize()
        {
            PositionUIElements();
        }
        
        private void PositionUIElements()
        {
            if (_canvasManager == null) return;
            
            var uiLayer = _canvasManager.GetLayer(UILayer.UI);
            
            PositionPlayerScorePanel();
            PositionOpponentScorePanel();
            PositionCenterPanel();
            PositionGameStatePanel();
        }
        
        private void PositionPlayerScorePanel()
        {
            if (_playerScorePanel == null) return;
            
            _playerScorePanel.anchorMin = new Vector2(0, 0);
            _playerScorePanel.anchorMax = new Vector2(1, 0);
            _playerScorePanel.anchoredPosition = new Vector2(0, 100);
            _playerScorePanel.sizeDelta = new Vector2(-40, 80);
        }
        
        private void PositionOpponentScorePanel()
        {
            if (_opponentScorePanel == null) return;
            
            _opponentScorePanel.anchorMin = new Vector2(0, 1);
            _opponentScorePanel.anchorMax = new Vector2(1, 1);
            _opponentScorePanel.anchoredPosition = new Vector2(0, -100);
            _opponentScorePanel.sizeDelta = new Vector2(-40, 80);
        }
        
        private void PositionCenterPanel()
        {
            if (_centerPanel == null) return;
            
            _centerPanel.anchorMin = new Vector2(0.5f, 0.5f);
            _centerPanel.anchorMax = new Vector2(0.5f, 0.5f);
            _centerPanel.anchoredPosition = Vector2.zero;
        }
        
        private void PositionGameStatePanel()
        {
            if (_gameStatePanel == null) return;
            
            _gameStatePanel.anchorMin = new Vector2(0.5f, 0.3f);
            _gameStatePanel.anchorMax = new Vector2(0.5f, 0.3f);
            _gameStatePanel.anchoredPosition = Vector2.zero;
        }
        
        public void AdaptToScreenSize()
        {
            float aspectRatio = (float)Screen.width / Screen.height;
            
            if (aspectRatio > 1.7f)
            {
                AdjustForWideScreen();
            }
            else if (aspectRatio < 1.3f)
            {
                AdjustForNarrowScreen();
            }
            else
            {
                AdjustForStandardScreen();
            }
        }
        
        private void AdjustForWideScreen()
        {
            if (_playerScorePanel != null)
            {
                _playerScorePanel.anchoredPosition = new Vector2(0, 80);
            }
            
            if (_opponentScorePanel != null)
            {
                _opponentScorePanel.anchoredPosition = new Vector2(0, -80);
            }
        }
        
        private void AdjustForNarrowScreen()
        {
            if (_playerScorePanel != null)
            {
                _playerScorePanel.anchoredPosition = new Vector2(0, 120);
            }
            
            if (_opponentScorePanel != null)
            {
                _opponentScorePanel.anchoredPosition = new Vector2(0, -120);
            }
        }
        
        private void AdjustForStandardScreen()
        {
            PositionUIElements();
        }
    }
}