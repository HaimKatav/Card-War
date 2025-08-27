using System.Collections.Generic;
using CardWar.Core.Data;

namespace CardWar.Services.Network
{
    [System.Serializable]
    public class WarData
    {
        /// <summary>
        /// Cards that initiated the war (the matching cards)
        /// </summary>
        public List<CardData> InitialWarCards { get; set; }
        
        /// <summary>
        /// All war rounds from start to finish with complete card data
        /// </summary>
        public List<WarRound> AllWarRounds { get; set; }
        
        /// <summary>
        /// Final card distribution after war completion
        /// </summary>
        public Dictionary<int, List<CardData>> FinalCardDistribution { get; set; }
        
        /// <summary>
        /// The winning player number (1 for player, 2 for opponent)
        /// </summary>
        public int WinningPlayerNumber { get; set; }
        
        /// <summary>
        /// Total cards won by the winner
        /// </summary>
        public int TotalCardsWon { get; set; }
        
        /// <summary>
        /// Whether players ran out of cards during war (requires shuffle)
        /// </summary>
        public bool RequiresShuffle { get; set; }
        
        /// <summary>
        /// Required pool size for displaying all war cards simultaneously
        /// </summary>
        public int RequiredPoolSize { get; set; }
        
        /// <summary>
        /// Estimated duration for complete war animation sequence
        /// </summary>
        public float EstimatedAnimationDuration { get; set; }

        public WarData()
        {
            InitialWarCards = new List<CardData>();
            AllWarRounds = new List<WarRound>();
            FinalCardDistribution = new Dictionary<int, List<CardData>>();
        }
    }

    [System.Serializable]
    public class WarRound
    {
        /// <summary>
        /// Concealed cards placed face down for this round
        /// </summary>
        public Dictionary<int, List<CardData>> ConcealedCards { get; set; }
        
        /// <summary>
        /// Fighting cards for this round
        /// </summary>
        public Dictionary<int, CardData> FightingCards { get; set; }
        
        /// <summary>
        /// Round number in the war sequence
        /// </summary>
        public int RoundNumber { get; set; }
        
        /// <summary>
        /// Whether this round resulted in another war
        /// </summary>
        public bool ResultedInWar { get; set; }
        
        /// <summary>
        /// Total cards accumulated in this war up to this round
        /// </summary>
        public int TotalCardsAccumulated { get; set; }

        public WarRound()
        {
            ConcealedCards = new Dictionary<int, List<CardData>>();
            FightingCards = new Dictionary<int, CardData>();
        }
    }
}