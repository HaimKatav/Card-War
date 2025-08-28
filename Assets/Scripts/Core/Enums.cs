namespace CardWar.Core.Enums
{
    public enum CardSuit
    {
        Hearts = 0,
        Diamonds = 1,
        Clubs = 2,
        Spades = 3
    }

    public enum CardRank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }

    public enum GameState
    {
        Idle,
        Initializing,
        Playing,
        War,
        RoundComplete,
        GameOver,
        Paused
    }

    public enum GameResult
    {
        PlayerWin,
        OpponentWin,
        War
    }

    public enum PlayerType
    {
        Human,
        Computer
    }
    
    public enum PanelType
    {
        MainMenu,
        Game, 
        Pause,
        Settings,
        GameOver,
        War,
        Loading
    }
    
    public enum ButtonType
    {
        Play,
        Pause,
        Settings,
        Back,
        Restart,
        MainMenu,
        Quit
    }
    
    public enum ButtonState
    {
        Normal,
        Highlighted, 
        Pressed,
        Disabled
    }
    
    public enum StatusType
    {
        War,
        Victory,
        Defeat,
        Draw,
        Connecting,
        Error
    }
    
    public enum SFXType
    {
        CardFlip,
        CardPlace,
        War,
        Victory,
        Defeat,
        ButtonClick,
        Error,
        Deal
    }
    
    public enum MusicType
    {
        MainMenu,
        Gameplay,
        Victory,
        Defeat
    }
    
    public enum EffectType
    {
        War,
        Victory,
        CardDeal,
        Sparkles
    }
}