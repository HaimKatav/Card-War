using UnityEngine;
using CardWar.Configuration;
using CardWar.Core.UI;
using Zenject;

namespace CardWar.UI.Core
{
    public class UILayoutController : MonoBehaviour
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
        
        private void Start()
        {
            PositionUIElements();
        }
        
        private void PositionUIElements()
        {
            var uiLayer = _canvasManager.GetLayer(UILayer.UI);
            
            // Position score panels at top and bottom
            if (_playerScorePanel != null)
            {
                _playerScorePanel.anchorMin = new Vector2(0, 0);
                _playerScorePanel.anchorMax = new Vector2(1, 0);
                _playerScorePanel.anchoredPosition = new Vector2(0, 100);
                _playerScorePanel.sizeDelta = new Vector2(-40, 80); // Leave margins
            }
            
            if (_opponentScorePanel != null)
            {
                _opponentScorePanel.anchorMin = new Vector2(0, 1);
                _opponentScorePanel.anchorMax = new Vector2(1, 1);
                _opponentScorePanel.anchoredPosition = new Vector2(0, -100);
                _opponentScorePanel.sizeDelta = new Vector2(-40, 80); // Leave margins
            }
            
            if (_centerPanel != null)
            {
                _centerPanel.anchorMin = new Vector2(0.5f, 0.5f);
                _centerPanel.anchorMax = new Vector2(0.5f, 0.5f);
                _centerPanel.anchoredPosition = Vector2.zero;
            }
            
            if (_gameStatePanel != null)
            {
                _gameStatePanel.anchorMin = new Vector2(0.5f, 0.3f);
                _gameStatePanel.anchorMax = new Vector2(0.5f, 0.3f);
                _gameStatePanel.anchoredPosition = Vector2.zero;
            }
        }
        
        /// <summary>
        /// Adjusts layout based on current screen aspect ratio
        /// </summary>
        public void AdaptToScreenSize()
        {
            float aspectRatio = (float)Screen.width / Screen.height;
            
            // Adjust spacing based on aspect ratio
            if (aspectRatio > 1.7f) // Wide screen
            {
                AdjustForWideScreen();
            }
            else if (aspectRatio < 1.5f) // Narrow screen
            {
                AdjustForNarrowScreen();
            }
            else
            {
                // Standard positioning already applied
            }
        }
        
        private void AdjustForWideScreen()
        {
            // Move UI elements closer to center on wide screens
            if (_playerScorePanel != null)
            {
                _playerScorePanel.anchoredPosition = new Vector2(0, 120);
            }
            
            if (_opponentScorePanel != null)
            {
                _opponentScorePanel.anchoredPosition = new Vector2(0, -120);
            }
        }
        
        private void AdjustForNarrowScreen()
        {
            // Spread UI elements more on narrow screens
            if (_playerScorePanel != null)
            {
                _playerScorePanel.anchoredPosition = new Vector2(0, 80);
            }
            
            if (_opponentScorePanel != null)
            {
                _opponentScorePanel.anchoredPosition = new Vector2(0, -80);
            }
        }
        
        private void OnRectTransformDimensionsChange()
        {
            // Respond to screen size changes
            if (_gameSettings != null && _canvasManager != null)
            {
                AdaptToScreenSize();
            }
        }
    }
}