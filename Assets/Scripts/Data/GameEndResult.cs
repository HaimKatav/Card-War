public class GameEndResult
{
    public bool PlayerWon { get; set; }
    public int TotalRounds { get; set; }
    public string WinnerName => PlayerWon ? "Player" : "Opponent";
}