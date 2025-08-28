using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using Zenject;

namespace CardWar.UI.Cards
{
    public class CardViewController : MonoBehaviour
    {
        [Header("Card Components")]
        [SerializeField] private Image _cardFront;
        [SerializeField] private Image _cardBack;
        [SerializeField] private CanvasGroup _canvasGroup;
        
        [Header("Animation Settings")]
        [SerializeField] private float _flipDuration = 0.3f;
        [SerializeField] private float _moveDuration = 0.5f;
        [SerializeField] private Ease _moveEase = Ease.OutQuad;
        [SerializeField] private Ease _flipEase = Ease.InOutQuad;
        
        private CardData _cardData;
        private RectTransform _rectTransform;
        private bool _isFaceUp;
        private Tween _currentAnimation;
        
        public class Pool : MonoMemoryPool<CardViewController> 
        {
            protected override void OnDespawned(CardViewController item)
            {
                item.ResetCard();
                item.gameObject.SetActive(false);
            }
            
            protected override void Reinitialize(CardViewController item)
            {
                item.gameObject.SetActive(true);
                item.ResetCard();
            }
        }
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            
            // Find card faces if not assigned
            if (_cardFront == null)
                _cardFront = transform.Find("CardFront")?.GetComponent<Image>();
            
            if (_cardBack == null)
                _cardBack = transform.Find("CardBack")?.GetComponent<Image>();
                
            // Ensure we start face down
            SetFaceDown(instant: true);
        }
        
        #region Public API - Called by Controllers/Managers
        
        public void Setup(CardData cardData)
        {
            _cardData = cardData;
            
            Debug.Log($"[CardViewController] Card setup: {cardData?.Rank} of {cardData?.Suit}");
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
            
            Debug.Log($"[CardViewController] Sprites set for {_cardData?.Rank} of {_cardData?.Suit}");
        }
        
        public void SetFrontSprite(Sprite sprite)
        {
            if (_cardFront != null && sprite != null)
            {
                _cardFront.sprite = sprite;
            }
        }
        
        public void SetBackSprite(Sprite sprite)
        {
            if (_cardBack != null && sprite != null)
            {
                _cardBack.sprite = sprite;
            }
        }
        
        #endregion
        
        #region Card State Management
        
        public CardData GetCardData() => _cardData;
        public bool IsFaceUp => _isFaceUp;
        public bool IsAnimating => _currentAnimation != null && _currentAnimation.IsActive();
        
        public void SetFaceUp(bool instant = false)
        {
            if (_isFaceUp && !instant) return;
            
            _isFaceUp = true;
            
            if (instant)
            {
                _cardFront.gameObject.SetActive(true);
                _cardBack.gameObject.SetActive(false);
            }
            else
            {
                FlipToFront();
            }
        }
        
        public void SetFaceDown(bool instant = false)
        {
            if (!_isFaceUp && !instant) return;
            
            _isFaceUp = false;
            
            if (instant)
            {
                _cardFront.gameObject.SetActive(false);
                _cardBack.gameObject.SetActive(true);
            }
            else
            {
                FlipToBack();
            }
        }
        
        public void ResetCard()
        {
            _currentAnimation?.Kill();
            _currentAnimation = null;
            
            _cardData = null;
            _isFaceUp = false;
            
            _canvasGroup.alpha = 1f;
            _rectTransform.localScale = Vector3.one;
            _rectTransform.rotation = Quaternion.identity;
            
            _cardFront.gameObject.SetActive(false);
            _cardBack.gameObject.SetActive(true);
            
            _cardFront.sprite = null;
            _cardBack.sprite = null;
        }
        
        #endregion
        
        #region Animation Methods
        
        public async UniTask FlipToFrontAsync()
        {
            if (IsAnimating) return;
            
            _currentAnimation = DOTween.Sequence()
                .Append(transform.DOScaleX(0f, _flipDuration / 2).SetEase(_flipEase))
                .AppendCallback(() =>
                {
                    _cardFront.gameObject.SetActive(true);
                    _cardBack.gameObject.SetActive(false);
                    _isFaceUp = true;
                })
                .Append(transform.DOScaleX(1f, _flipDuration / 2).SetEase(_flipEase))
                .OnComplete(() => _currentAnimation = null);
            
            await _currentAnimation.AsyncWaitForCompletion();
        }
        
        public async UniTask FlipToBackAsync()
        {
            if (IsAnimating) return;
            
            _currentAnimation = DOTween.Sequence()
                .Append(transform.DOScaleX(0f, _flipDuration / 2).SetEase(_flipEase))
                .AppendCallback(() =>
                {
                    _cardFront.gameObject.SetActive(false);
                    _cardBack.gameObject.SetActive(true);
                    _isFaceUp = false;
                })
                .Append(transform.DOScaleX(1f, _flipDuration / 2).SetEase(_flipEase))
                .OnComplete(() => _currentAnimation = null);
            
            await _currentAnimation.AsyncWaitForCompletion();
        }
        
        public void FlipToFront()
        {
            if (IsAnimating) return;
            
            _currentAnimation = DOTween.Sequence()
                .Append(transform.DOScaleX(0f, _flipDuration / 2).SetEase(_flipEase))
                .AppendCallback(() =>
                {
                    _cardFront.gameObject.SetActive(true);
                    _cardBack.gameObject.SetActive(false);
                    _isFaceUp = true;
                })
                .Append(transform.DOScaleX(1f, _flipDuration / 2).SetEase(_flipEase))
                .OnComplete(() => _currentAnimation = null);
        }
        
        public void FlipToBack()
        {
            if (IsAnimating) return;
            
            _currentAnimation = DOTween.Sequence()
                .Append(transform.DOScaleX(0f, _flipDuration / 2).SetEase(_flipEase))
                .AppendCallback(() =>
                {
                    _cardFront.gameObject.SetActive(false);
                    _cardBack.gameObject.SetActive(true);
                    _isFaceUp = false;
                })
                .Append(transform.DOScaleX(1f, _flipDuration / 2).SetEase(_flipEase))
                .OnComplete(() => _currentAnimation = null);
        }
        
        public async UniTask MoveToPositionAsync(Vector2 targetPosition)
        {
            if (IsAnimating) return;
            _currentAnimation = _rectTransform
                .DOAnchorPos(targetPosition, _moveDuration, true)
                .SetEase(_moveEase)
                .OnComplete(() => _currentAnimation = null);
            
            await _currentAnimation.AsyncWaitForCompletion();
        }
        
        public void MoveToPosition(Vector3 targetPosition)
        {
            _currentAnimation?.Kill();
            
            _currentAnimation = _rectTransform
                .DOAnchorPos(targetPosition, _moveDuration, true)
                .SetEase(_moveEase)
                .OnComplete(() => _currentAnimation = null);
        }
        
        public async UniTask ScalePunchAsync(float punchScale = 1.2f, float duration = 0.3f)
        {
            if (IsAnimating) return;
            
            _currentAnimation = _rectTransform
                .DOPunchScale(Vector3.one * punchScale, duration, 1, 0.5f)
                .OnComplete(() => _currentAnimation = null);
            
            await _currentAnimation.AsyncWaitForCompletion();
        }
        
        public async UniTask FadeOutAsync(float duration = 0.5f)
        {
            if (IsAnimating) return;
            
            _currentAnimation = _canvasGroup
                .DOFade(0f, duration)
                .OnComplete(() => _currentAnimation = null);
            
            await _currentAnimation.AsyncWaitForCompletion();
        }
        
        public async UniTask FadeInAsync(float duration = 0.5f)
        {
            if (IsAnimating) return;
            
            _currentAnimation = _canvasGroup
                .DOFade(1f, duration)
                .OnComplete(() => _currentAnimation = null);
            
            await _currentAnimation.AsyncWaitForCompletion();
        }
        
        #endregion
        
        #region Debug & Validation
        
        private void OnValidate()
        {
            if (_cardFront == null)
                _cardFront = transform.Find("CardFront")?.GetComponent<Image>();
            
            if (_cardBack == null)
                _cardBack = transform.Find("CardBack")?.GetComponent<Image>();
            
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }
        
        #if UNITY_EDITOR
        [ContextMenu("Test Flip to Front")]
        private void TestFlipToFront() => FlipToFront();
        
        [ContextMenu("Test Flip to Back")]
        private void TestFlipToBack() => FlipToBack();
        
        [ContextMenu("Test Scale Punch")]
        private void TestScalePunch() => _ = ScalePunchAsync();
        #endif
        
        #endregion
        
        private void OnDestroy()
        {
            _currentAnimation?.Kill();
        }
    }
}