using CardWar.Core.Enums;
using CardWar.Services.Network;

namespace CardWar.Core.Data
{
    [System.Serializable]
    public class GameRoundResultData
    {
        public CardData PlayerCard { get; set; }
        public CardData OpponentCard { get; set; }
        public GameResult Result { get; set; }
        public int CardsWon { get; set; }
        public bool IsGameEnded { get; set; }
        
        // NEW: War resolution data
        /// <summary>
        /// Complete war data if this round resulted in a war
        /// </summary>
        public WarData WarData { get; set; }
        
        /// <summary>
        /// Whether this round triggered a war scenario
        /// </summary>
        public bool IsWarRound => WarData != null;
        
        /// <summary>
        /// Required pool size for this round (increased during wars)
        /// </summary>
        public int RequiredPoolSize { get; set; } = 16; // Default minimum
        
        /// <summary>
        /// Estimated animation duration for this round
        /// </summary>
        public float AnimationDuration { get; set; } = 2.0f; // Default duration
    }
}