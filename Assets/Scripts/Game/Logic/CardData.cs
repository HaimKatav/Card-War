using CardWar.Common;

namespace CardWar.Game.Logic
{
    public class CardData
    {
        public Suit Suit { get; set; }
        public Rank Rank { get; set; }
        public int Value => (int)Rank;
        public string CardKey => $"{GetRankString()}_{GetSuitString()}";

        private string GetRankString()
        {
            switch (Rank)
            {
                case Rank.Jack: return "jack";
                case Rank.Queen: return "queen";
                case Rank.King: return "king";
                case Rank.Ace: return "ace";
                default: return ((int)Rank).ToString();
            }
        }

        private string GetSuitString()
        {
            return Suit.ToString().ToLower();
        }
    }
}