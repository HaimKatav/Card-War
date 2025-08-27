using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class CardView : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image _cardImage;
    [SerializeField] private TextMeshProUGUI _rankText;
    [SerializeField] private TextMeshProUGUI _suitText;
    [SerializeField] private Image _suitIcon;

    private CardData _cardData;

    public CardData CardData => _cardData;

    public void Setup(CardData cardData)
    {
        _cardData = cardData;
        UpdateVisuals();
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

        // Set background card image (placeholder for now)
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

    // Memory Pool support
    public class Pool : MonoMemoryPool<CardView>
    {
        protected override void Reinitialize(CardView item)
        {
            // Reset card state when returned to pool
            item._cardData = null;
            item.gameObject.SetActive(true);
        }
        
        protected override void OnDespawned(CardView item)
        {
            item.gameObject.SetActive(false);
        }
    }
}