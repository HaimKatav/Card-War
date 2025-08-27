using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardWar.Core.Data;
using CardWar.Gameplay.Cards;

namespace CardWar.Gameplay.Players
{
    /// <summary>
    /// Simple implementation of card deck visualization
    /// </summary>
    public class SimpleCardDeck : MonoBehaviour, ICardDeck
    {
        [Header("Visual Components")]
        [SerializeField] private Image _deckImage;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private GameObject _highlightEffect;
        [SerializeField] private Transform _drawPoint;
        
        [Header("Settings")]
        [SerializeField] private float _shuffleAnimationDuration = 1f;
        [SerializeField] private AnimationCurve _shuffleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private int _count;
        private bool _isHighlighted;
        
        public int Count => _count;
        public Transform Transform => transform;
        public Vector3 DrawPosition => _drawPoint != null ? _drawPoint.position : transform.position + Vector3.up * 0.5f;
        
        private void Awake()
        {
            // Create draw point if not assigned
            if (_drawPoint == null)
            {
                var drawPointGO = new GameObject("DrawPoint");
                drawPointGO.transform.SetParent(transform);
                drawPointGO.transform.localPosition = Vector3.up * 0.5f;
                _drawPoint = drawPointGO.transform;
            }
            
            if (_highlightEffect != null)
                _highlightEffect.SetActive(false);
        }
        
        public void SetCardCount(int count)
        {
            _count = Mathf.Max(0, count);
            UpdateDeckVisual(_count);
        }

        public void UpdateDeckVisual(int remainingCards)
        {
            _count = remainingCards;
            
            // Update count display
            if (_countText != null)
            {
                _countText.text = _count.ToString();
            }
            
            // Update deck thickness/opacity based on card count
            if (_deckImage != null)
            {
                // Scale deck based on card count (thicker when more cards)
                float scaleY = Mathf.Lerp(0.5f, 2f, _count / 52f);
                _deckImage.transform.localScale = new Vector3(1, scaleY, 1);
                
                // Fade when getting low on cards
                float alpha = _count > 0 ? Mathf.Lerp(0.5f, 1f, _count / 10f) : 0f;
                var color = _deckImage.color;
                color.a = alpha;
                _deckImage.color = color;
            }
            
            // Show/hide deck based on card count
            gameObject.SetActive(_count > 0);
        }
        
        public async UniTask AnimateDrawAsync(CardView card)
        {
            if (card == null) return;
            
            // Start from deck position
            card.transform.position = DrawPosition;
            card.transform.rotation = transform.rotation;
            
            // Quick scale effect
            card.transform.localScale = Vector3.zero;
            
            float duration = 0.2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                card.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }
            
            card.transform.localScale = Vector3.one;
        }
        
        public async UniTask AnimateReturnAsync(CardView card)
        {
            if (card == null) return;
            
            Vector3 startPos = card.transform.position;
            Vector3 endPos = DrawPosition;
            Quaternion startRot = card.transform.rotation;
            Quaternion endRot = transform.rotation;
            
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float curveT = _shuffleCurve.Evaluate(t);
                
                card.transform.position = Vector3.Lerp(startPos, endPos, curveT);
                card.transform.rotation = Quaternion.Lerp(startRot, endRot, curveT);
                
                // Shrink as it approaches deck
                float scale = Mathf.Lerp(1f, 0f, t);
                card.transform.localScale = Vector3.one * scale;
                
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }
            
            card.transform.position = endPos;
            card.transform.localScale = Vector3.zero;
        }

        public async UniTask AnimateShuffleAsync()
        {
            if (_deckImage == null) return;
            
            Vector3 originalPos = _deckImage.transform.localPosition;
            float elapsed = 0f;
            
            while (elapsed < _shuffleAnimationDuration)
            {
                float t = elapsed / _shuffleAnimationDuration;
                
                // Shake effect
                float shakeX = Mathf.Sin(t * Mathf.PI * 8) * 0.1f * (1f - t);
                float shakeY = Mathf.Cos(t * Mathf.PI * 8) * 0.05f * (1f - t);
                
                _deckImage.transform.localPosition = originalPos + new Vector3(shakeX, shakeY, 0);
                
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }
            
            _deckImage.transform.localPosition = originalPos;
            
            Debug.Log("[SimpleCardDeck] Shuffle animation completed");
        }
        
        public void SetHighlight(bool highlighted)
        {
            _isHighlighted = highlighted;
            
            if (_highlightEffect != null)
            {
                _highlightEffect.SetActive(highlighted);
            }
            
            // Pulse animation when highlighted
            if (highlighted)
            {
                AnimatePulse().Forget();
            }
        }
        
        private async UniTaskVoid AnimatePulse()
        {
            if (_deckImage == null) return;
            
            Vector3 originalScale = _deckImage.transform.localScale;
            
            while (_isHighlighted)
            {
                // Scale up
                await ScaleToAsync(_deckImage.transform, originalScale * 1.1f, 0.3f);
                
                if (!_isHighlighted) break;
                
                // Scale down
                await ScaleToAsync(_deckImage.transform, originalScale, 0.3f);
                
                await UniTask.Delay(200);
            }
            
            _deckImage.transform.localScale = originalScale;
        }
        
        private async UniTask ScaleToAsync(Transform target, Vector3 targetScale, float duration)
        {
            Vector3 startScale = target.localScale;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                target.localScale = Vector3.Lerp(startScale, targetScale, t);
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }
            
            target.localScale = targetScale;
        }
    }
}