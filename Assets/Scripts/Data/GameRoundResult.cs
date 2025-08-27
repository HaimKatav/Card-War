[System.Serializable]
public class GameRoundResult
{
    public CardData PlayerCard { get; set; }
    public CardData OpponentCard { get; set; }
    public GameResult Result { get; set; }
    public int CardsWon { get; set; }
    public bool IsGameEnded { get; set; }
}