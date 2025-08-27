namespace CardWar.Core.Enums
{
    // Core Game Enums
    public enum CardSuit
    {
        Hearts,
        Diamonds,
        Clubs,
        Spades
    }

    public enum CardRank
    {
        Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7, Eight = 8,
        Nine = 9, Ten = 10, Jack = 11, Queen = 12, King = 13, Ace = 14
    }

    public enum GameResult
    {
        PlayerWins,
        OpponentWins,
        War
    }

    // Player Enums
    public enum PlayerPosition
    {
        Bottom,
        Top,
        Left,
        Right
    }

    public enum AIDifficulty
    {
        Easy,
        Normal,
        Hard,
        Instant
    }

    public enum PlayerInputType
    {
        TapDeck,
        TapCard,
        Swipe,
        Button
    }
}