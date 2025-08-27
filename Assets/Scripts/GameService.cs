using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

public class GameService : IGameService, IInitializable
{
    private readonly IFakeServerService _fakeServer;
    private readonly IUIService _uiService;
    private readonly SignalBus _signalBus;
    
    private GameState _currentGameState;
    private bool _isProcessingRound;
    private IGameUIController _gameUIController;

    public bool IsGameActive => _currentGameState?.IsGameActive ?? false;

    [Inject]
    public GameService(IFakeServerService fakeServer, IUIService uiService, SignalBus signalBus)
    {
        _fakeServer = fakeServer;
        _uiService = uiService;
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        Debug.Log("GameService: Initializing...");
        
        // Subscribe to signals
        _signalBus.Subscribe<NetworkErrorSignal>(OnNetworkError);
        _signalBus.Subscribe<GameUIControllerReadySignal>(OnGameUIControllerReady);
    }

    public async void StartNewGame()
    {
        Debug.Log("GameService: Starting new game...");
        
        _uiService.ShowLoading(true);

        try
        {
            // Create GameUIController if it doesn't exist
            await _uiService.CreateGameUIControllerAsync();
            
            // Start the game
            var response = await _fakeServer.StartNewGame();
            
            if (response.Success)
            {
                await RefreshGameState();
                _uiService.ShowGameplay();
                _signalBus.Fire<RoundStartedSignal>();
            }
            else
            {
                HandleServerError(response.ErrorMessage);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"GameService: Failed to start game - {ex.Message}");
            HandleServerError("Failed to connect to server");
        }
        finally
        {
            _uiService.ShowLoading(false);
        }
    }

    public async UniTask<GameRoundResult> PlayRound()
    {
        if (_isProcessingRound || !IsGameActive)
        {
            Debug.LogWarning("GameService: Cannot play round - game not active or already processing");
            return null;
        }

        _isProcessingRound = true;
        _uiService.ShowLoading(true);

        try
        {
            Debug.Log("GameService: Playing round...");
            var response = await _fakeServer.DrawCard();
            
            if (response.Success)
            {
                var result = response.Data;
                await ProcessRoundResult(result);
                
                // Update game state
                await RefreshGameState();
                
                return result;
            }
            else
            {
                HandleServerError(response.ErrorMessage);
                return null;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"GameService: Round failed - {ex.Message}");
            HandleServerError("Network error occurred");
            return null;
        }
        finally
        {
            _isProcessingRound = false;
            _uiService.ShowLoading(false);
        }
    }

    public GameState GetCurrentGameState()
    {
        return _currentGameState;
    }



    private async UniTask RefreshGameState()
    {
        try
        {
            var response = await _fakeServer.GetGameState();
            if (response.Success)
            {
                _currentGameState = response.Data;
                Debug.Log($"GameService: Game state updated - Player: {_currentGameState.PlayerCardsCount}, Opponent: {_currentGameState.OpponentCardsCount}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"GameService: Failed to refresh game state - {ex.Message}");
        }
    }

    private async UniTask ProcessRoundResult(GameRoundResult result)
    {
        Debug.Log($"GameService: Processing round result - {result.Result}");
        
        _gameUIController.UpdatePlayerCard(result.PlayerCard);
        _gameUIController.UpdateOpponentCard(result.OpponentCard);
        _gameUIController.ShowRoundResult(result);
        
        // Fire round completed signal
        _signalBus.Fire(new RoundCompletedSignal(result));

        // Check if game ended
        if (result.IsGameEnded)
        {
            await HandleGameEnd(result);
        }
    }

    private async UniTask HandleGameEnd(GameRoundResult finalRound)
    {
        Debug.Log("GameService: Game ended");

        // Determine winner based on final game state
        await RefreshGameState();

        var gameEndResult = new GameEndResult
        {
            PlayerWon = _currentGameState.PlayerCardsCount > _currentGameState.OpponentCardsCount,
            TotalRounds = _currentGameState.RoundsPlayed
        };

        _gameUIController.ShowGameResult(gameEndResult);

        _signalBus.Fire(new GameEndedSignal(gameEndResult));
    }



    private void HandleServerError(string errorMessage)
    {
        Debug.LogError($"GameService: Server error - {errorMessage}");
        _signalBus.Fire(new NetworkErrorSignal(errorMessage));
        
        // Reset to main menu or show retry option
        _uiService.ShowMainMenu();
    }

    private void OnNetworkError(NetworkErrorSignal signal)
    {
        Debug.LogError($"GameService: Network error received - {signal.ErrorMessage}");
        // Could show retry dialog or other error handling UI
    }

    private void OnGameUIControllerReady(GameUIControllerReadySignal signal)
    {
        Debug.Log("GameService: GameUIController ready signal received");
        _gameUIController = signal.GameUIController;
    }

    public void Dispose()
    {
        _signalBus?.TryUnsubscribe<NetworkErrorSignal>(OnNetworkError);
        _signalBus?.TryUnsubscribe<GameUIControllerReadySignal>(OnGameUIControllerReady);
    }
}