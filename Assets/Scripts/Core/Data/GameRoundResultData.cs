using CardWar.Core.Enums;

namespace CardWar.Core.Data
{
    public class GameRoundResultData
    {
        public CardData PlayerCard { get; set; }
        public CardData OpponentCard { get; set; }
        public GameResult Result { get; set; }
        public int CardsWon { get; set; }
        public bool IsGameEnded { get; set; }
        public WarData WarDetails { get; set; }
    }
}