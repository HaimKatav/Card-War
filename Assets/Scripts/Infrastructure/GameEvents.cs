using CardWar.Core.Data;
using CardWar.Core.Enums;

namespace CardWar.Infrastructure.Events
{
    public class GameStartEvent { }
    
    public class GameEndEvent 
    { 
        public int WinnerPlayerNumber { get; }
        public GameEndEvent(int winnerPlayerNumber) 
        { 
            WinnerPlayerNumber = winnerPlayerNumber; 
        }
    }
    
    public class RoundStartEvent { }
    
    public class RoundCompleteEvent 
    { 
        public GameRoundResultData Result { get; }
        public RoundCompleteEvent(GameRoundResultData result) 
        { 
            Result = result; 
        }
    }
    
    public class WarStartEvent 
    { 
        public WarData WarData { get; }
        public WarStartEvent(WarData warData) 
        { 
            WarData = warData; 
        }
    }
    
    public class GameStateChangedEvent 
    { 
        public GameState NewState { get; }
        public GameStateChangedEvent(GameState newState) 
        { 
            NewState = newState; 
        }
    }
    
    public class PlayerActionEvent 
    { 
        public string ActionType { get; }
        public PlayerActionEvent(string actionType) 
        { 
            ActionType = actionType; 
        }
    }
}