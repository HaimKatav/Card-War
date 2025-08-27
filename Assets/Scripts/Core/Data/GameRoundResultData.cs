namespace CardWar.Core.Data
{
    public class GameEndResultData
    {
        public bool PlayerWon { get; set; }
        public int TotalRounds { get; set; }
        public string WinnerName => PlayerWon ? "Player" : "Opponent";
    }
}