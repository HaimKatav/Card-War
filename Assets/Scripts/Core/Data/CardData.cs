using System;
using CardWar.Core.Enums;

namespace CardWar.Core.Data
{
    [Serializable]
    public class CardData : IEquatable<CardData>
    {
        public CardSuit Suit { get; }
        public CardRank Rank { get; }
        public int Value => (int)Rank;
        public string CardId => $"{Rank}_{Suit}";
        
        public CardData(CardSuit suit, CardRank rank)
        {
            Suit = suit;
            Rank = rank;
        }
        
        public bool Equals(CardData other)
        {
            if (other == null) return false;
            return Suit == other.Suit && Rank == other.Rank;
        }
        
        public override bool Equals(object obj)
        {
            return Equals(obj as CardData);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Suit, Rank);
        }
        
        public override string ToString()
        {
            return $"{Rank} of {Suit}";
        }
    }
}