using CardWar.Core.Data;
using CardWar.Services.Network;

namespace CardWar.Core.Events
{
    public class StartGameEvent { }

    public class RoundStartedEvent { }

    public class RoundCompletedEvent 
    { 
        public GameRoundResultData resultData { get; }
        public RoundCompletedEvent(GameRoundResultData resultData) { this.resultData = resultData; }
    }

    public class GameEndedEvent 
    { 
        public GameEndResultData resultData { get; }
        public GameEndedEvent(GameEndResultData resultData) { this.resultData = resultData; }
    }

    public class NetworkErrorEvent 
    { 
        public string ErrorMessage { get; }
        public NetworkErrorEvent(string errorMessage) { ErrorMessage = errorMessage; }
    }

    public class GameUIControllerReadySignal 
    { 
        public IGameUIController GameUIController { get; }
        public GameUIControllerReadySignal(IGameUIController gameUIController) { GameUIController = gameUIController; }
    }

    public class ReturnToMenuEvent { }

    public class WarStartedEvent
    {
        public WarData WarData { get; }
        public WarStartedEvent(WarData warData) { WarData = warData; }
    }

    public class PoolResizeEvent
    {
        public int NewSize { get; }
        public PoolResizeEvent(int newSize) { NewSize = newSize; }
    }
}