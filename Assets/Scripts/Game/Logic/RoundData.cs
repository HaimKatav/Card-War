using System.Collections.Generic;
using CardWar.Core;
using CardWar.Game.Logic;

namespace CardWar.Common
{
    public class RoundData
    {
        public int RoundNumber { get; set; }
        public CardData PlayerCard { get; set; }
        public CardData OpponentCard { get; set; }
        public RoundResult Result { get; set; }
        public int PlayerCardsRemaining { get; set; }
        public int OpponentCardsRemaining { get; set; }
        public bool IsWar { get; set; }
        public bool IsGameOver { get; set; }
        public bool HasChainedWar { get; set; }
        public bool WarEndedInDraw { get; set; }
        public int TotalCardsInPot { get; set; }
        public int WarDepth { get; set; }
        
        public List<CardData> PlayerWarCards { get; set; } = new ();
        public List<CardData> OpponentWarCards { get; set; } = new();
    }
}