using System;
using CardWar.Common;
using UnityEngine;
using UnityEngine.UI;
using CardWar.Game.Logic;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace CardWar.Game.UI
{
    public class CardView : MonoBehaviour
    {
        [Header("Card Images")]
        [SerializeField] private Image _cardFront;
        [SerializeField] private Image _cardBack;
        
        [Header("Animation Components")]
        [SerializeField] private CanvasGroup _canvasGroup;
        
        private CardData _cardData;
        private bool _isFaceUp = false;
        private Color _originalFrontColor = Color.white;
        private Color _originalBackColor = Color.white;
        
        public CardData CardData => _cardData;
        public bool IsFaceUp => _isFaceUp;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_cardFront != null)
                _originalFrontColor = _cardFront.color;
            if (_cardBack != null)
                _originalBackColor = _cardBack.color;
        }
        
        private void OnDisable()
        {
            _isFaceUp = false;
            ShowCardSide(false);
            ResetVisuals();
        }
        
        private void OnEnable()
        {
            _isFaceUp = false;
            ShowCardSide(false);
            ResetVisuals();
        }
        
        #endregion
        
        #region Public Methods
        
        public void SetCardData(CardData cardData)
        {
            _cardData = cardData;
        }
        
        public void SetCardSprite(Sprite frontSprite)
        {
            if (_cardFront != null)
                _cardFront.sprite = frontSprite;
        }
        
        public void SetBackSprite(Sprite backSprite)
        {
            if (_cardBack != null)
                _cardBack.sprite = backSprite;
        }
        
        public void SetFaceUp(bool faceUp)
        {
            _isFaceUp = faceUp;
            ShowCardSide(_isFaceUp);
        }
        
        public async UniTask FlipCard(bool faceUp, float duration = 0.3f)
        {
            _isFaceUp = faceUp;
            
            if (duration <= 0)
            {
                ShowCardSide(_isFaceUp);
                return;
            }
            
            var endValue = transform.localRotation.eulerAngles + new Vector3(0, 90, 0);
            
            await transform.DOLocalRotate(endValue, duration * 0.5f)
                .AsyncWaitForCompletion().AsUniTask();

            
            ShowCardSide(_isFaceUp);
            
            await transform.DORotateQuaternion(Quaternion.identity, duration * 0.5f)
                .AsyncWaitForCompletion().AsUniTask();
        }
        
        public async UniTask FlipCardAsync(bool faceUp, float duration, Ease ease)
        {
            _isFaceUp = faceUp;
            
            if (duration <= 0)
            {
                ShowCardSide(_isFaceUp);
                return;
            }
            
            await transform.DORotateQuaternion(Quaternion.Euler(0, 90, 0), duration * 0.5f)
                .SetEase(ease)
                .AsyncWaitForCompletion().AsUniTask();
            
            ShowCardSide(_isFaceUp);
            
            await transform.DORotateQuaternion(Quaternion.identity, duration * 0.5f)
                .SetEase(ease)
                .AsyncWaitForCompletion().AsUniTask();
        }
        
        public void ResetCard()
        {
            _cardData = null;
            _isFaceUp = false;
            ShowCardSide(false);
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            ResetVisuals();
        }
        
        public CardData GetCardData()
        {
            return _cardData;
        }
        
        #endregion
        
        #region Animation Methods
        
        public void SetAlpha(float alpha)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }
        }
        
        public Tween FadeIn(float duration)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                return _canvasGroup.DOFade(1f, duration);
            }
            return null;
        }
        
        public Tween FadeOut(float duration)
        {
            if (_canvasGroup != null)
            {
                return _canvasGroup.DOFade(0f, duration);
            }
            return null;
        }
        
        public async UniTask FadeInAsync(float duration)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                await _canvasGroup.DOFade(1f, duration).AsyncWaitForCompletion().AsUniTask();
            }
        }
        
        public async UniTask FadeOutAsync(float duration)
        {
            if (_canvasGroup != null)
            {
                await _canvasGroup.DOFade(0f, duration).AsyncWaitForCompletion().AsUniTask();
            }
        }
        
        public Tween SetTint(Color color, float duration)
        {
            var sequence = DOTween.Sequence();
            
            if (_isFaceUp && _cardFront != null)
            {
                sequence.Append(_cardFront.DOColor(color, duration));
            }
            else if (!_isFaceUp && _cardBack != null)
            {
                sequence.Append(_cardBack.DOColor(color, duration));
            }
            
            return sequence;
        }
        
        public async UniTask SetTintAsync(Color color, float duration)
        {
            if (_isFaceUp && _cardFront != null)
            {
                await _cardFront.DOColor(color, duration).AsyncWaitForCompletion().AsUniTask();
            }
            else if (!_isFaceUp && _cardBack != null)
            {
                await _cardBack.DOColor(color, duration).AsyncWaitForCompletion().AsUniTask();
            }
        }
        
        public void ResetTint()
        {
            if (_cardFront != null)
                _cardFront.color = _originalFrontColor;
            if (_cardBack != null)
                _cardBack.color = _originalBackColor;
        }
        
        public Tween ScaleCard(float targetScale, float duration, Ease ease = Ease.OutBack)
        {
            return transform.DOScale(targetScale, duration).SetEase(ease);
        }
        
        public async UniTask ScaleCardAsync(float targetScale, float duration, Ease ease = Ease.OutBack)
        {
            await transform.DOScale(targetScale, duration).SetEase(ease).AsyncWaitForCompletion().AsUniTask();
        }
        
        #endregion
        
        #region Private Methods
        
        private void ShowCardSide(bool showFront)
        {
            if (_cardFront != null)
                _cardFront.gameObject.SetActive(showFront);
                
            if (_cardBack != null)
                _cardBack.gameObject.SetActive(!showFront);
        }
        
        private void ResetVisuals()
        {
            SetAlpha(1f);
            ResetTint();
            transform.localScale = Vector3.one;
        }
        
        #endregion
    }
}