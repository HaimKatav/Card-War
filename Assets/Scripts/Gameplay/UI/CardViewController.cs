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
        private Sequence _currentAnimation;
        
        // For pooling
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
        }
        
        public void Setup(CardData cardData)
        {
            _cardData = cardData;
            UpdateCardVisual();
        }
        
        public void SetFaceUp(bool faceUp, bool immediate = false)
        {
            if (_isFaceUp == faceUp) return;
            
            _isFaceUp = faceUp;
            
            if (immediate)
            {
                _cardFront.gameObject.SetActive(faceUp);
                _cardBack.gameObject.SetActive(!faceUp);
                transform.localRotation = Quaternion.identity;
            }
            else
            {
                FlipCard().Forget();
            }
        }
        
        public async UniTaskVoid FlipCard()
        {
            KillCurrentAnimation();
            
            // First half of flip
            await transform.DORotate(new Vector3(0, 90, 0), _flipDuration / 2)
                .SetEase(_flipEase)
                .ToUniTask();
            
            // Switch card face
            _cardFront.gameObject.SetActive(_isFaceUp);
            _cardBack.gameObject.SetActive(!_isFaceUp);
            
            // Second half of flip
            await transform.DORotate(new Vector3(0, _isFaceUp ? 0 : 180, 0), _flipDuration / 2)
                .SetEase(_flipEase)
                .ToUniTask();
        }
        
        public async UniTask FlipToFront(CardData cardData)
        {
            _cardData = cardData;
            UpdateCardVisual();
            
            if (!_isFaceUp)
            {
                await FlipCard();
            }
        }
        
        public async UniTask MoveTo(Vector3 targetPosition, float duration = -1)
        {
            if (duration < 0)
                duration = _moveDuration;
            
            KillCurrentAnimation();
            
            await transform.DOMove(targetPosition, duration)
                .SetEase(_moveEase)
                .ToUniTask();
        }
        
        public async UniTask MoveToLocal(Vector3 targetLocalPosition, float duration = -1)
        {
            if (duration < 0)
                duration = _moveDuration;
            
            KillCurrentAnimation();
            
            await transform.DOLocalMove(targetLocalPosition, duration)
                .SetEase(_moveEase)
                .ToUniTask();
        }
        
        public async UniTask AnimateDeal(Vector3 fromPosition, Vector3 toPosition, float delay = 0)
        {
            transform.position = fromPosition;
            SetFaceUp(false, true);
            
            _canvasGroup.alpha = 0;
            transform.localScale = Vector3.zero;
            
            if (delay > 0)
                await UniTask.Delay(TimeSpan.FromSeconds(delay));
            
            KillCurrentAnimation();
            
            _currentAnimation = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(1, 0.2f))
                .Join(transform.DOScale(1, 0.3f).SetEase(Ease.OutBack))
                .Append(transform.DOMove(toPosition, _moveDuration).SetEase(_moveEase));
            
            await _currentAnimation.ToUniTask();
        }
        
        public async UniTask AnimateWin(Vector3 targetPosition)
        {
            KillCurrentAnimation();
            
            // Victory bounce animation
            _currentAnimation = DOTween.Sequence()
                .Append(transform.DOPunchScale(Vector3.one * 0.2f, 0.3f))
                .Append(transform.DOMove(targetPosition, _moveDuration).SetEase(Ease.InBack))
                .Join(_canvasGroup.DOFade(0, _moveDuration));
            
            await _currentAnimation.ToUniTask();
        }
        
        public async UniTask AnimateDiscard()
        {
            KillCurrentAnimation();
            
            _currentAnimation = DOTween.Sequence()
                .Append(transform.DOScale(0, 0.3f).SetEase(Ease.InBack))
                .Join(_canvasGroup.DOFade(0, 0.3f));
            
            await _currentAnimation.ToUniTask();
        }
        
        public void ShowWarHighlight()
        {
            // Pulsing effect for war
            transform.DOScale(1.1f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        
        public void StopWarHighlight()
        {
            DOTween.Kill(transform);
            transform.localScale = Vector3.one;
        }
        
        private void UpdateCardVisual()
        {
            if (_cardData == null) return;
            
            // TODO: Load actual card sprite from resources
            // For now, just set a placeholder color based on suit
            Color suitColor = _cardData.Suit switch
            {
                CardSuit.Hearts => Color.red,
                CardSuit.Diamonds => Color.red,
                CardSuit.Clubs => Color.black,
                CardSuit.Spades => Color.black,
                _ => Color.gray
            };
            
            if (_cardFront != null)
            {
                _cardFront.color = Color.white;
                
                // Try to load sprite
                string spritePath = $"Cards/{_cardData.CardId}";
                Sprite cardSprite = Resources.Load<Sprite>(spritePath);
                
                if (cardSprite != null)
                {
                    _cardFront.sprite = cardSprite;
                }
                else
                {
                    // Fallback: show text
                    // TODO: Add TextMeshPro component for card rank/suit display
                    Debug.LogWarning($"Card sprite not found: {spritePath}");
                }
            }
        }
        
        private void ResetCard()
        {
            KillCurrentAnimation();
            
            _cardData = null;
            _isFaceUp = false;
            _canvasGroup.alpha = 1;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            
            if (_cardFront != null)
            {
                _cardFront.gameObject.SetActive(false);
                _cardFront.sprite = null;
            }
            
            if (_cardBack != null)
            {
                _cardBack.gameObject.SetActive(true);
            }
        }
        
        private void KillCurrentAnimation()
        {
            if (_currentAnimation != null && _currentAnimation.IsActive())
            {
                _currentAnimation.Kill();
                _currentAnimation = null;
            }
            
            DOTween.Kill(transform);
            DOTween.Kill(_canvasGroup);
        }
        
        private void OnDestroy()
        {
            KillCurrentAnimation();
        }
    }
}