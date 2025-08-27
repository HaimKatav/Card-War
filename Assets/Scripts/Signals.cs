public class StartGameSignal { }
public class RoundStartedSignal { }
public class RoundCompletedSignal 
{ 
    public GameRoundResult Result { get; }
    public RoundCompletedSignal(GameRoundResult result) { Result = result; }
}
public class GameEndedSignal 
{ 
    public GameEndResult Result { get; }
    public GameEndedSignal(GameEndResult result) { Result = result; }
}
public class NetworkErrorSignal 
{ 
    public string ErrorMessage { get; }
    public NetworkErrorSignal(string errorMessage) { ErrorMessage = errorMessage; }
}
public class GameUIControllerReadySignal 
{ 
    public IGameUIController GameUIController { get; }
    public GameUIControllerReadySignal(IGameUIController gameUIController) { GameUIController = gameUIController; }
}