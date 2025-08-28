using UnityEngine;
using UnityEngine.UI;
using CardWar.Configuration;
using Zenject;

namespace CardWar.Core.UI
{
    public class CanvasManager : MonoBehaviour, IInitializable
    {
        [Header("Canvas References")]
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private CanvasScaler _canvasScaler;
        [SerializeField] private GraphicRaycaster _graphicRaycaster;
        
        [Header("Layer References")]
        [SerializeField] private RectTransform _backgroundLayer;
        [SerializeField] private RectTransform _gameLayer;
        [SerializeField] private RectTransform _uiLayer;
        [SerializeField] private RectTransform _overlayLayer;
        
        private GameSettings _gameSettings;
        
        [Inject]
        public void Construct(GameSettings gameSettings)
        {
            _gameSettings = gameSettings;
        }
        
        public void Initialize()
        {
            SetupCanvasForScreens();
        }
        
        private void SetupCanvasForScreens()
        {
            if (_canvasScaler == null || _gameSettings == null) 
            {
                Debug.LogError("[CanvasManager] Missing references - CanvasScaler or GameSettings is null");
                return;
            }
            
            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = _gameSettings.canvasReferenceResolution;
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _canvasScaler.matchWidthOrHeight = _gameSettings.canvasMatchWidthOrHeight;
            
            Debug.Log($"[CanvasManager] Canvas setup for resolution: {_gameSettings.canvasReferenceResolution}");
        }
        
        public RectTransform GetLayer(UILayer layer)
        {
            return layer switch
            {
                UILayer.Background => _backgroundLayer,
                UILayer.Game => _gameLayer,
                UILayer.UI => _uiLayer,
                UILayer.Overlay => _overlayLayer,
                _ => _uiLayer
            };
        }
    }
    
    public enum UILayer
    {
        Background = 0,
        Game = 10,
        UI = 20,
        Overlay = 90
    }
}