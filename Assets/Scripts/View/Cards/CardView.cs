using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Zenject;
using CardWar.Core.Data;
using CardWar.Core.Enums;

namespace CardWar.Gameplay.Cards
{
    public class CardView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image _cardImage;
        [SerializeField] private Image _cardBackImage;
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private TextMeshProUGUI _suitText;
        [SerializeField] private Image _suitIcon;
        
        [Header("Visual States")]
        [SerializeField] private GameObject _frontFace;
        [SerializeField] private GameObject _backFace;

        private CardData _cardData;
        private Sprite _backSprite;
        private bool _isFaceUp;

        public CardData CardData => _cardData;
        public bool IsFaceUp => _isFaceUp;

        private void Awake()
        {
            // Ensure we start with back face showing
            ShowBackFace();
        }

        public void Setup(CardData cardData)
        {
            _cardData = cardData;
            UpdateVisuals();
        }

        public void SetBackSprite(Sprite backSprite)
        {
            _backSprite = backSprite;
            if (_cardBackImage != null)
            {
                _cardBackImage.sprite = backSprite;
            }
        }

        public void ShowFrontFace()
        {
            _isFaceUp = true;
            if (_frontFace != null) _frontFace.SetActive(true);
            if (_backFace != null) _backFace.SetActive(false);
        }

        public void ShowBackFace()
        {
            _isFaceUp = false;
            if (_frontFace != null) _frontFace.SetActive(false);
            if (_backFace != null) _backFace.SetActive(true);
        }

        public void SetFaceUp(bool faceUp, bool immediate = false)
        {
            if (immediate)
            {
                if (faceUp)
                    ShowFrontFace();
                else
                    ShowBackFace();
            }
            else
            {
                // Animation will handle the face change
                _isFaceUp = faceUp;
            }
        }

        private void UpdateVisuals()
        {
            if (_cardData == null) return;

            // Update text elements
            if (_rankText != null)
                _rankText.text = GetRankDisplayText(_cardData.Rank);
            
            if (_suitText != null)
                _suitText.text = GetSuitSymbol(_cardData.Suit);

            // Update colors based on suit
            Color suitColor = GetSuitColor(_cardData.Suit);
            if (_suitText != null)
                _suitText.color = suitColor;
            if (_rankText != null)
                _rankText.color = suitColor;

            // Set background card image
            if (_cardImage != null)
                _cardImage.color = Color.white;
        }

        private string GetRankDisplayText(CardRank rank)
        {
            return rank switch
            {
                CardRank.Jack => "J",
                CardRank.Queen => "Q",
                CardRank.King => "K",
                CardRank.Ace => "A",
                _ => ((int)rank).ToString()
            };
        }

        private string GetSuitSymbol(CardSuit suit)
        {
            return suit switch
            {
                CardSuit.Hearts => "♥",
                CardSuit.Diamonds => "♦",
                CardSuit.Clubs => "♣",
                CardSuit.Spades => "♠",
                _ => "?"
            };
        }

        private Color GetSuitColor(CardSuit suit)
        {
            return suit switch
            {
                CardSuit.Hearts => Color.red,
                CardSuit.Diamonds => Color.red,
                CardSuit.Clubs => Color.black,
                CardSuit.Spades => Color.black,
                _ => Color.gray
            };
        }

        // Reset method for pooling
        public void ResetCard()
        {
            _cardData = null;
            ShowBackFace();
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            
            if (_rankText != null) _rankText.text = "";
            if (_suitText != null) _suitText.text = "";
        }

        // Memory Pool support
        public class Pool : MonoMemoryPool<CardView>
        {
            protected override void Reinitialize(CardView item)
            {
                item.ResetCard();
                item.gameObject.SetActive(true);
            }
            
            protected override void OnDespawned(CardView item)
            {
                item.gameObject.SetActive(false);
            }

            protected override void OnCreated(CardView item)
            {
                base.OnCreated(item);
                // Initial setup if needed
            }

            protected override void OnDestroyed(CardView item)
            {
                // Cleanup if needed
                base.OnDestroyed(item);
            }
        }

        public async UniTask FlipToFront(CardData data)
        {
            await transform
                .DOLocalRotate(new Vector3(0f, 90f, 0f), 0.15f)
                .AsyncWaitForCompletion();
            Setup(data);
            ShowFrontFace();
            await transform
                .DOLocalRotate(Vector3.zero, 0.15f)
                .AsyncWaitForCompletion();
        }

        public async UniTask MoveToPosition(Vector3 target)
        {
            await transform.DOMove(target, 0.5f)
                .SetEase(Ease.OutQuad)
                .AsyncWaitForCompletion();
        }

        public void ShowWinPile(int cardCount)
        {
            transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
        }
    }
}