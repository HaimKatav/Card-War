using CardWar.Common;
using UnityEngine;
using UnityEngine.UI;
using CardWar.Game.Logic;
using DG.Tweening;

namespace CardWar.Game.UI
{
    public class CardView : MonoBehaviour
    {
        [Header("Card Images")]
        [SerializeField] private Image _cardFront;
        [SerializeField] private Image _cardBack;
        
        [Header("Card Elements")]
        [SerializeField] private Text _rankText;
        [SerializeField] private Image _suitImage;
        
        [Header("Sprites")]
        [SerializeField] private Sprite _defaultBackSprite;
        
        private CardData _cardData;
        private bool _isFaceUp;
        private RectTransform _rectTransform;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            
            if (_rectTransform == null)
            {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }
            
            SetupCardImages();
        }

        private void SetupCardImages()
        {
            if (_cardFront == null)
            {
                GameObject frontObject = new GameObject("CardFront");
                frontObject.transform.SetParent(transform);
                _cardFront = frontObject.AddComponent<Image>();
                _cardFront.color = Color.white;
                
                RectTransform frontRect = frontObject.GetComponent<RectTransform>();
                frontRect.anchorMin = Vector2.zero;
                frontRect.anchorMax = Vector2.one;
                frontRect.sizeDelta = Vector2.zero;
                frontRect.anchoredPosition = Vector2.zero;
            }
            
            if (_cardBack == null)
            {
                GameObject backObject = new GameObject("CardBack");
                backObject.transform.SetParent(transform);
                _cardBack = backObject.AddComponent<Image>();
                _cardBack.color = new Color(0.2f, 0.3f, 0.5f);
                
                RectTransform backRect = backObject.GetComponent<RectTransform>();
                backRect.anchorMin = Vector2.zero;
                backRect.anchorMax = Vector2.one;
                backRect.sizeDelta = Vector2.zero;
                backRect.anchoredPosition = Vector2.zero;
            }
            
            if (_rankText == null)
            {
                GameObject textObject = new GameObject("RankText");
                textObject.transform.SetParent(_cardFront.transform);
                _rankText = textObject.AddComponent<Text>();
                _rankText.text = "";
                _rankText.alignment = TextAnchor.MiddleCenter;
                _rankText.fontSize = 24;
                _rankText.color = Color.black;
                
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;
            }
        }

        public void SetCardData(CardData cardData)
        {
            _cardData = cardData;
            UpdateCardDisplay();
        }

        public void SetCardSprite(Sprite sprite)
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

        private void UpdateCardDisplay()
        {
            if (_cardData == null) return;
            
            if (_rankText != null)
            {
                _rankText.text = GetRankDisplay(_cardData.Rank);
            }
            
            UpdateSuitDisplay();
        }

        private string GetRankDisplay(Rank rank)
        {
            switch (rank)
            {
                case Rank.Ace: return "A";
                case Rank.King: return "K";
                case Rank.Queen: return "Q";
                case Rank.Jack: return "J";
                default: return ((int)rank).ToString();
            }
        }

        private void UpdateSuitDisplay()
        {
            if (_cardData == null || _rankText == null) return;
            
            Color suitColor = (_cardData.Suit == Suit.Hearts || _cardData.Suit == Suit.Diamonds) ? 
                Color.red : Color.black;
            
            _rankText.color = suitColor;
            
            string suitSymbol = GetSuitSymbol(_cardData.Suit);
            if (!string.IsNullOrEmpty(suitSymbol))
            {
                _rankText.text += "\n" + suitSymbol;
            }
        }

        private string GetSuitSymbol(Suit suit)
        {
            switch (suit)
            {
                case Suit.Hearts: return "♥";
                case Suit.Diamonds: return "♦";
                case Suit.Clubs: return "♣";
                case Suit.Spades: return "♠";
                default: return "";
            }
        }

        public void FlipCard(bool faceUp, float duration = 0.3f)
        {
            if (_isFaceUp == faceUp) return;
            
            _isFaceUp = faceUp;
            
            if (duration <= 0)
            {
                ShowCardSide(_isFaceUp);
            }
            else
            {
                AnimateFlip(duration);
            }
        }

        private void AnimateFlip(float duration)
        {
            transform.DORotate(new Vector3(0, 90, 0), duration / 2)
                .OnComplete(() =>
                {
                    ShowCardSide(_isFaceUp);
                    transform.DORotate(Vector3.zero, duration / 2);
                });
        }

        private void ShowCardSide(bool showFront)
        {
            if (_cardFront != null)
                _cardFront.gameObject.SetActive(showFront);
                
            if (_cardBack != null)
                _cardBack.gameObject.SetActive(!showFront);
        }

        public void ResetCard()
        {
            _cardData = null;
            _isFaceUp = false;
            
            ShowCardSide(false);
            
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            
            if (_rankText != null)
            {
                _rankText.text = "";
            }
            
            DOTween.Kill(transform);
        }

        public CardData GetCardData()
        {
            return _cardData;
        }

        public bool IsFaceUp()
        {
            return _isFaceUp;
        }

        private void OnDestroy()
        {
            DOTween.Kill(transform);
        }
    }
}