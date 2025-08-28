using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using CardWar.Core.Data;
using Zenject;

namespace CardWar.UI.Cards
{
    public class CardViewController : MonoBehaviour, IPoolable<IMemoryPool>
    {
        [SerializeField] private Image _cardFront;
        [SerializeField] private Image _cardBack;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _rectTransform;
        
        private CardData _cardData;
        private IMemoryPool _pool;
        private Tween _currentAnimation;
        private bool _isFaceUp = false;
        
        public bool IsAnimating => _currentAnimation != null && _currentAnimation.IsActive();
        
        public void Setup(CardData cardData)
        {
            _cardData = cardData;
        }
        
        public void SetCardSprites(Sprite frontSprite, Sprite backSprite)
        {
            if (_cardFront != null && frontSprite != null)
            {
                _cardFront.sprite = frontSprite;
            }
            
            if (_cardBack != null && backSprite != null)
            {
                _cardBack.sprite = backSprite;
            }
        }
        
        public CardData GetCardData() => _cardData;
        
        public void SetFaceUp(bool instant = false)
        {
            if (_isFaceUp) return;
            
            _isFaceUp = true;
            
            if (instant)
            {
                _cardFront.gameObject.SetActive(true);
                _cardBack.gameObject.SetActive(false);
                transform.localScale = Vector3.one;
            }
        }
        
        public void SetFaceDown(bool instant = false)
        {
            if (!_isFaceUp) return;
            
            _isFaceUp = false;
            
            if (instant)
            {
                _cardFront.gameObject.SetActive(false);
                _cardBack.gameObject.SetActive(true);
                transform.localScale = Vector3.one;
            }
        }
        
        public async UniTask FlipToFrontAsync()
        {
            if (IsAnimating || _isFaceUp) return;
            
            _currentAnimation = DOTween.Sequence()
                .Append(transform.DOScaleX(0f, 0.15f))
                .AppendCallback(() =>
                {
                    _cardFront.gameObject.SetActive(true);
                    _cardBack.gameObject.SetActive(false);
                    _isFaceUp = true;
                })
                .Append(transform.DOScaleX(1f, 0.15f))
                .OnComplete(() => _currentAnimation = null);
            
            await _currentAnimation.AsyncWaitForCompletion();
        }
        
        public async UniTask FlipToBackAsync()
        {
            if (IsAnimating || !_isFaceUp) return;
            
            _currentAnimation = DOTween.Sequence()
                .Append(transform.DOScaleX(0f, 0.15f))
                .AppendCallback(() =>
                {
                    _cardFront.gameObject.SetActive(false);
                    _cardBack.gameObject.SetActive(true);
                    _isFaceUp = false;
                })
                .Append(transform.DOScaleX(1f, 0.15f))
                .OnComplete(() => _currentAnimation = null);
            
            await _currentAnimation.AsyncWaitForCompletion();
        }
        
        public async UniTask MoveToPositionAsync(Vector3 targetPosition, float duration = 0.5f)
        {
            if (IsAnimating) return;
            
            _currentAnimation = transform
                .DOMove(targetPosition, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => _currentAnimation = null);
            
            await _currentAnimation.AsyncWaitForCompletion();
        }
        
        public async UniTask ScalePunchAsync(float punchScale = 1.2f, float duration = 0.3f)
        {
            if (IsAnimating) return;
            
            _currentAnimation = _rectTransform
                .DOPunchScale(Vector3.one * punchScale, duration, 1, 0.5f)
                .OnComplete(() => _currentAnimation = null);
            
            await _currentAnimation.AsyncWaitForCompletion();
        }
        
        public void OnSpawned(IMemoryPool pool)
        {
            _pool = pool;
            _isFaceUp = false;
            _canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;
            
            _cardFront.gameObject.SetActive(false);
            _cardBack.gameObject.SetActive(true);
        }
        
        public void OnDespawned()
        {
            _currentAnimation?.Kill();
            _currentAnimation = null;
            _cardData = null;
            _pool = null;
        }
        
        private void OnDestroy()
        {
            _currentAnimation?.Kill();
        }
        
        public class Pool : MonoMemoryPool<CardViewController>
        {
        }
    }
}