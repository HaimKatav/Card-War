using CardWar.Common;

namespace CardWar.Game.Logic
{
    public class CardData
    {
        public Suit Suit { get; set; }
        public Rank Rank { get; set; }
        public string CardKey => $"{GetRankString()}_{GetSuitString()}";

        private string GetRankString()
        {
            return Rank switch
            {
                Rank.Jack or Rank.Queen or Rank.King or Rank.Ace => Rank.ToString().ToLower(),
                _ => ((int)Rank).ToString()
            };
        }

        private string GetSuitString()
        {
            return Suit.ToString().ToLower();
        }
    }
}