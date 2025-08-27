using System;
using CardWar.Gameplay.Players;
using CardWar.Services.UI;
using CardWar.Core.Data;
using CardWar.Core.Events;
using CardWar.Services.Network;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace CardWar.Core.GameLogic
{
    public class GameService : IGameService, IInitializable
    {
        private readonly IFakeServerService _fakeServer;
        private readonly IUIService _uiService;
        private readonly SignalBus _signalBus;
        private readonly PlayerControllerFactory _playerFactory;
        
        private GameStateData _currentGameStateData;
        private bool _isProcessingRound;
        private IGameUIController _gameUIController;
        private IPlayerController _localPlayer;
        private IPlayerController _aiPlayer;

        public bool IsGameActive => _currentGameStateData?.IsGameActive ?? false;

        [Inject]
        public GameService(
            IFakeServerService fakeServer, 
            IUIService uiService, 
            SignalBus signalBus,
            PlayerControllerFactory playerFactory)
        {
            _fakeServer = fakeServer;
            _uiService = uiService;
            _signalBus = signalBus;
            _playerFactory = playerFactory;
        }

        public void Initialize()
        {
            Debug.Log("GameService: Initializing...");
            
            // Subscribe to signals
            _signalBus.Subscribe<NetworkErrorEvent>(OnNetworkError);
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
                
                // Create players
                await CreatePlayersAsync();
                
                // Start the game
                var response = await _fakeServer.StartNewGame();
                
                if (response.Success)
                {
                    await RefreshGameState();
                    _uiService.ShowGameplay();
                    _signalBus.Fire<RoundStartedEvent>();
                }
                else
                {
                    HandleServerError(response.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameService: Failed to start game - {ex.Message}");
                HandleServerError("Failed to connect to server");
            }
            finally
            {
                _uiService.ShowLoading(false);
            }
        }
        
        private async UniTask CreatePlayersAsync()
        {
            try
            {
                // Get player areas from UI
                var playerArea = _gameUIController?.GetPlayerArea();
                var opponentArea = _gameUIController?.GetOpponentArea();
                
                if (playerArea == null || opponentArea == null)
                {
                    Debug.LogWarning("GameService: Player areas not found, using default positions");
                    // Create default areas if not found
                    playerArea = new GameObject("PlayerArea").transform;
                    opponentArea = new GameObject("OpponentArea").transform;
                }
                
                // Create both players
                (_localPlayer, _aiPlayer) = await _playerFactory.CreateBothPlayersAsync(
                    playerArea, 
                    opponentArea);
                    
                Debug.Log("GameService: Players created successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameService: Failed to create players - {ex.Message}");
                throw;
            }
        }

        public async UniTask<GameRoundResultData> PlayRound()
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
            catch (Exception ex)
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

        public GameStateData GetCurrentGameState()
        {
            return _currentGameStateData;
        }



        private async UniTask RefreshGameState()
        {
            try
            {
                var response = await _fakeServer.GetGameState();
                if (response.Success)
                {
                    _currentGameStateData = response.Data;
                    Debug.Log($"GameService: Game state updated - Player: {_currentGameStateData.PlayerCardsCount}, Opponent: {_currentGameStateData.OpponentCardsCount}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameService: Failed to refresh game state - {ex.Message}");
            }
        }

        private async UniTask ProcessRoundResult(GameRoundResultData resultData)
        {
            Debug.Log($"GameService: Processing round result - {resultData.Result}");
            
            // Handle war scenario
            if (resultData.IsWarRound && resultData.WarData != null)
            {
                await HandleWarScenario(resultData);
            }
            else
            {
                // Handle normal round
                _gameUIController.UpdatePlayerCard(resultData.PlayerCard);
                _gameUIController.UpdateOpponentCard(resultData.OpponentCard);
                _gameUIController.ShowRoundResult(resultData);
            }
            
            // Fire round completed signal
            _signalBus.Fire(new RoundCompletedEvent(resultData));

            // Check if game ended
            if (resultData.IsGameEnded)
            {
                await HandleGameEnd(resultData);
            }
        }
        
        private async UniTask HandleWarScenario(GameRoundResultData resultData)
        {
            Debug.Log($"GameService: Handling war scenario with {resultData.WarData.TotalCardsWon} cards");
            
            try
            {
                // Disable draw button during war
                _gameUIController.SetDrawButtonInteractable(false);
                
                // War animation simplified
                _gameUIController.ShowRoundResult(resultData);
                _gameUIController.SetDrawButtonInteractable(true);
                Debug.Log("GameService: War scenario completed successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameService: War scenario failed - {ex.Message}");
                // Re-enable draw button on error
                _gameUIController.SetDrawButtonInteractable(true);
            }
        }

        private async UniTask HandleGameEnd(GameRoundResultData finalRound)
        {
            Debug.Log("GameService: Game ended");

            // Determine winner based on final game state
            await RefreshGameState();

            var gameEndResult = new GameEndResultData
            {
                PlayerWon = _currentGameStateData.PlayerCardsCount > _currentGameStateData.OpponentCardsCount,
                TotalRounds = _currentGameStateData.RoundsPlayed
            };

            _gameUIController.ShowGameResult(gameEndResult);

            _signalBus.Fire(new GameEndedEvent(gameEndResult));
        }



        private void HandleServerError(string errorMessage)
        {
            Debug.LogError($"GameService: Server error - {errorMessage}");
            _signalBus.Fire(new NetworkErrorEvent(errorMessage));
            
            // Reset to main menu or show retry option
            _uiService.ShowMainMenu();
        }

        private void OnNetworkError(NetworkErrorEvent @event)
        {
            Debug.LogError($"GameService: Network error received - {@event.ErrorMessage}");
            // Could show retry dialog or other error handling UI
        }

        private void OnGameUIControllerReady(GameUIControllerReadySignal signal)
        {
            Debug.Log("GameService: GameUIController ready signal received");
            _gameUIController = signal.GameUIController;
        }

        public void Dispose()
        {
            _signalBus?.TryUnsubscribe<NetworkErrorEvent>(OnNetworkError);
            _signalBus?.TryUnsubscribe<GameUIControllerReadySignal>(OnGameUIControllerReady);
        }
    }
}