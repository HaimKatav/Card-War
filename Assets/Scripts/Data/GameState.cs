[System.Serializable]
public class GameState
{
    public int PlayerCardsCount { get; set; }
    public int OpponentCardsCount { get; set; }
    public bool IsGameActive { get; set; }
    public CardData LastPlayerCard { get; set; }
    public CardData LastOpponentCard { get; set; }
    public int RoundsPlayed { get; set; }
}