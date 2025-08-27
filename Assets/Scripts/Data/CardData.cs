[System.Serializable]
public class CardData
{
    public CardSuit Suit { get; }
    public CardRank Rank { get; }
    public int Value { get; }

    public CardData(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
        Value = (int)rank;
    }

    public override string ToString()
    {
        return $"{Rank} of {Suit}";
    }
}