using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardWar.Core.Data;
using CardWar.View.Cards;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Simple implementation of card placement slot
    /// </summary>
    public class SimpleCardSlot : MonoBehaviour, ICardSlot
    {
        [Header("Visual Components")]
        [SerializeField] private Image _slotBackground;
        [SerializeField] private Transform _cardAnchor;
        [SerializeField] private Transform _concealedCardsParent;
        [SerializeField] private GameObject _highlightEffect;
        [SerializeField] private TextMeshProUGUI _cardCountText;
        
        [Header("War Display")]
        [SerializeField] private GameObject _warStackIndicator;
        [SerializeField] private TextMeshProUGUI _warStackCountText;
        
        [Header("Settings")]
        [SerializeField] private float _cardSpacing = 0.2f;
        [SerializeField] private float _placeAnimationDuration = 0.5f;
        [SerializeField] private AnimationCurve _placementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private CardView _currentCard;
        private List<CardView> _concealedCards;
        private bool _isHighlighted;
        
        public Transform Transform => transform;
        public Vector3 Position => _cardAnchor != null ? _cardAnchor.position : transform.position;
        CardView ICardSlot.CurrentCard => CurrentCard;

        List<CardView> ICardSlot.ConcealedCards => ConcealedCards;

        public CardView CurrentCard => _currentCard;
        public List<CardView> ConcealedCards => _concealedCards;
        public bool HasCard => _currentCard != null || (_concealedCards != null && _concealedCards.Count > 0);
        
        private void Awake()
        {
            _concealedCards = new List<CardView>();
            
            // Create card anchor if not assigned
            if (_cardAnchor == null)
            {
                var anchorGO = new GameObject("CardAnchor");
                anchorGO.transform.SetParent(transform);
                anchorGO.transform.localPosition = Vector3.zero;
                _cardAnchor = anchorGO.transform;
            }
            
            // Create concealed cards parent if not assigned
            if (_concealedCardsParent == null)
            {
                var parentGO = new GameObject("ConcealedCards");
                parentGO.transform.SetParent(transform);
                parentGO.transform.localPosition = Vector3.zero;
                _concealedCardsParent = parentGO.transform;
            }
            
            if (_highlightEffect != null)
                _highlightEffect.SetActive(false);
            
            if (_warStackIndicator != null)
                _warStackIndicator.SetActive(false);
            
            HideCardCount();
        }
        
        public async UniTask PlaceCardAsync(CardView card, bool animate = true)
        {
            if (card == null) return;
            
            // Remove previous card if exists
            if (_currentCard != null)
            {
                await RemoveCardAsync(false);
            }
            
            _currentCard = card;
            card.transform.SetParent(_cardAnchor);
            
            if (animate)
            {
                await AnimateCardPlacement(card);
            }
            else
            {
                card.transform.localPosition = Vector3.zero;
                card.transform.localRotation = Quaternion.identity;
                card.transform.localScale = Vector3.one;
            }
        }

        UniTask ICardSlot.PlaceCardAsync(CardView card, bool animate)
        {
            return PlaceCardAsync(card, animate);
        }

        public async UniTask RemoveCardAsync(bool animate = true)
        {
            if (_currentCard == null) return;
            
            if (animate)
            {
                await AnimateCardRemoval(_currentCard);
            }
            
            _currentCard = null;
        }

        CardView ICardSlot.RemoveCardImmediate()
        {
            return RemoveCardImmediate();
        }

        UniTask ICardSlot.PlaceConcealedCardAsync(CardView card, int index)
        {
            return PlaceConcealedCardAsync(card, index);
        }

        public CardView RemoveCardImmediate()
        {
            var card = _currentCard;
            _currentCard = null;
            return card;
        }
        
        public async UniTask PlaceConcealedCardAsync(CardView card, int index)
        {
            if (card == null) return;
            
            card.transform.SetParent(_concealedCardsParent);
            _concealedCards.Add(card);
            
            // Position based on index (stack slightly offset)
            Vector3 targetPos = new Vector3(
                index * _cardSpacing * 0.3f,
                index * _cardSpacing * 0.1f,
                -index * 0.01f // Z-order
            );
            
            // Animate placement
            Vector3 startPos = card.transform.position;
            float elapsed = 0f;
            
            while (elapsed < _placeAnimationDuration)
            {
                float t = elapsed / _placeAnimationDuration;
                float curveT = _placementCurve.Evaluate(t);
                
                card.transform.localPosition = Vector3.Lerp(
                    card.transform.InverseTransformPoint(startPos),
                    targetPos,
                    curveT);
                
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }
            
            card.transform.localPosition = targetPos;
            
            // Update count display
            ShowCardCount(_concealedCards.Count);
        }
        
        public async UniTask PlaceWarStackIndicator(int totalCards)
        {
            if (_warStackIndicator != null)
            {
                _warStackIndicator.SetActive(true);
                
                if (_warStackCountText != null)
                {
                    _warStackCountText.text = $"War! ({totalCards} cards)";
                }
                
                // Animate indicator appearance
                _warStackIndicator.transform.localScale = Vector3.zero;
                
                float duration = 0.3f;
                float elapsed = 0f;
                
                while (elapsed < duration)
                {
                    float t = elapsed / duration;
                    _warStackIndicator.transform.localScale = Vector3.Lerp(
                        Vector3.zero,
                        Vector3.one,
                        t);
                    
                    elapsed += Time.deltaTime;
                    await UniTask.Yield();
                }
                
                _warStackIndicator.transform.localScale = Vector3.one;
            }
        }
        
        public async UniTask ClearWarCards()
        {
            // Clear concealed cards
            foreach (var card in _concealedCards)
            {
                if (card != null)
                {
                    // Cards will be returned to pool by PlayerController
                    card.transform.SetParent(null);
                }
            }
            _concealedCards.Clear();
            
            // Hide war indicator
            if (_warStackIndicator != null)
            {
                _warStackIndicator.SetActive(false);
            }
            
            HideCardCount();
            
            await UniTask.CompletedTask;
        }

        List<CardView> ICardSlot.GetAllCards()
        {
            return GetAllCards();
        }

        public List<CardView> GetAllCards()
        {
            var allCards = new List<CardView>(_concealedCards);
            
            if (_currentCard != null)
            {
                allCards.Add(_currentCard);
            }
            
            return allCards;
        }
        
        public void SetHighlight(bool highlighted)
        {
            _isHighlighted = highlighted;
            
            if (_highlightEffect != null)
            {
                _highlightEffect.SetActive(highlighted);
            }
            
            if (_slotBackground != null)
            {
                var color = _slotBackground.color;
                color.a = highlighted ? 0.5f : 0.2f;
                _slotBackground.color = color;
            }
        }
        
        public void ShowCardCount(int count)
        {
            if (_cardCountText != null)
            {
                _cardCountText.gameObject.SetActive(count > 0);
                
                if (count > 3)
                {
                    _cardCountText.text = $"Total: {count}";
                }
                else
                {
                    _cardCountText.text = "";
                }
            }
        }
        
        private void HideCardCount()
        {
            if (_cardCountText != null)
            {
                _cardCountText.gameObject.SetActive(false);
            }
        }
        
        private async UniTask AnimateCardPlacement(CardView card)
        {
            Vector3 startPos = card.transform.position;
            Vector3 endPos = _cardAnchor.TransformPoint(Vector3.zero);
            
            float elapsed = 0f;
            
            while (elapsed < _placeAnimationDuration)
            {
                float t = elapsed / _placeAnimationDuration;
                float curveT = _placementCurve.Evaluate(t);
                
                card.transform.position = Vector3.Lerp(startPos, endPos, curveT);
                card.transform.localRotation = Quaternion.Lerp(
                    card.transform.localRotation,
                    Quaternion.identity,
                    curveT);
                
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }
            
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;
        }
        
        private async UniTask AnimateCardRemoval(CardView card)
        {
            if (card == null) return;
            
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 startScale = card.transform.localScale;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                card.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }
            
            card.transform.localScale = Vector3.zero;
        }
    }
}