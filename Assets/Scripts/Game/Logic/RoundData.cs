using System.Collections.Generic;
using CardWar.Common;

namespace CardWar.Game.Logic
{
    public class RoundData
    {
        public int RoundNumber { get; set; }
        public CardData PlayerCard { get; set; }
        public CardData OpponentCard { get; set; }
        public bool IsWar { get; set; }
        public List<CardData> PlayerWarCards { get; set; }
        public List<CardData> OpponentWarCards { get; set; }
        public RoundResult Result { get; set; }
        public int PlayerCardsRemaining { get; set; }
        public int OpponentCardsRemaining { get; set; }
        public bool HasChainedWar { get; set; }
        public int WarDepth { get; set; }
        public int TotalCardsInPot { get; set; }

        public RoundData()
        {
            PlayerWarCards = new List<CardData>();
            OpponentWarCards = new List<CardData>();
            WarDepth = 0;
            TotalCardsInPot = 2;
        }
    }
}