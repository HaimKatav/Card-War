using System.Collections.Generic;

namespace CardWar.Core.Data
{
    public class WarData
    {
        public List<CardData> InitialWarCards { get; set; } = new List<CardData>();
        public List<WarRound> AllWarRounds { get; set; } = new List<WarRound>();
        public int WinningPlayerNumber { get; set; }
        public int TotalCardsWon { get; set; }
        public float EstimatedAnimationDuration { get; set; } = 3.0f;
        public bool RequiresShuffle { get; set; }
    }
    
    public class WarRound
    {
        public int RoundNumber { get; set; }
        public List<CardData> PlayerCards { get; set; } = new List<CardData>();
        public List<CardData> OpponentCards { get; set; } = new List<CardData>();
        public List<CardData> AllCardsPlaced { get; set; } = new List<CardData>();
        public CardData PlayerFightingCard { get; set; }
        public CardData OpponentFightingCard { get; set; }
        public bool ResultedInWar { get; set; }
        public int WinnerPlayerNumber { get; set; }
        public float RoundAnimationDuration { get; set; } = 2.0f;
    }
}