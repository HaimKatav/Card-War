using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace CardWar.Gameplay.Players
{
    /// <summary>
    /// UI component for displaying player's card count
    /// </summary>
    public class PlayerCardCountUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private TextMeshProUGUI _cardCountText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private GameObject _highlightEffect;
        
        [Header("Visual Settings")]
        [SerializeField] private Color _normalColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color _highlightColor = new Color(0.3f, 0.5f, 0.8f, 0.8f);
        [SerializeField] private Color _warningColor = new Color(0.8f, 0.3f, 0.2f, 0.8f);
        [SerializeField] private int _warningThreshold = 5;
        
        private string _playerName;
        private int _currentCount;
        private bool _isHighlighted;
        private Tween _pulseTween;
        
        private void Awake()
        {
            if (_highlightEffect != null)
                _highlightEffect.SetActive(false);
            
            if (_backgroundImage != null)
                _backgroundImage.color = _normalColor;
        }
        
        public void SetPlayerName(string playerName)
        {
            _playerName = playerName;
            
            if (_playerNameText != null)
            {
                _playerNameText.text = playerName;
            }
        }
        
        public void UpdateCount(int count)
        {
            _currentCount = Mathf.Max(0, count);
            
            if (_cardCountText != null)
            {
                _cardCountText.text = $"Cards: {_currentCount}";
                
                // Change color based on card count
                if (_currentCount <= _warningThreshold && _currentCount > 0)
                {
                    _cardCountText.color = _warningColor;
                    StartWarningPulse();
                }
                else if (_currentCount == 0)
                {
                    _cardCountText.color = Color.gray;
                    StopWarningPulse();
                }
                else
                {
                    _cardCountText.color = Color.white;
                    StopWarningPulse();
                }
            }
            
            // Animate count change
            AnimateCountChange();
        }
        
        public void SetHighlight(bool highlighted)
        {
            _isHighlighted = highlighted;
            
            if (_highlightEffect != null)
            {
                _highlightEffect.SetActive(highlighted);
            }
            
            if (_backgroundImage != null)
            {
                DOTween.Kill(_backgroundImage);
                _backgroundImage.DOColor(
                    highlighted ? _highlightColor : _normalColor,
                    0.3f);
            }
            
            if (highlighted)
            {
                AnimateTurnIndicator();
            }
        }
        
        private void AnimateCountChange()
        {
            if (_cardCountText == null) return;
            
            // Punch scale effect
            DOTween.Kill(_cardCountText.transform);
            _cardCountText.transform.localScale = Vector3.one;
            _cardCountText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);
        }
        
        private void AnimateTurnIndicator()
        {
            if (_playerNameText == null) return;
            
            // Glow effect for turn indicator
            DOTween.Kill(_playerNameText);
            
            _playerNameText.DOColor(Color.yellow, 0.3f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        
        private void StartWarningPulse()
        {
            if (_cardCountText == null) return;
            
            StopWarningPulse();
            
            _pulseTween = _cardCountText.transform
                .DOScale(Vector3.one * 1.1f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        
        private void StopWarningPulse()
        {
            _pulseTween?.Kill();
            _pulseTween = null;
            
            if (_cardCountText != null)
            {
                _cardCountText.transform.localScale = Vector3.one;
            }
        }
        
        private void OnDestroy()
        {
            // Clean up DOTween animations
            DOTween.Kill(transform);
            DOTween.Kill(_cardCountText);
            DOTween.Kill(_playerNameText);
            DOTween.Kill(_backgroundImage);
            _pulseTween?.Kill();
        }
    }
}