using System;

namespace CardWar.Core.Data
{
    [Serializable]
    public class GameStateData
    {
        public int PlayerCardsCount { get; set; }
        public int OpponentCardsCount { get; set; }
        public bool IsGameActive { get; set; }
        public int RoundsPlayed { get; set; }
        public DateTime GameStartTime { get; set; }
        public int TotalWars { get; set; }
    }
}