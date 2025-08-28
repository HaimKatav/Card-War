using System;
using UnityEngine;
using Zenject;
using Cysharp.Threading.Tasks;
using CardWar.Services.Game;
using CardWar.Core.Enums;
using CardWar.Infrastructure.Events;

namespace CardWar.Gameplay.Controllers
{
    public class GameController : IInitializable, ITickable, IDisposable
    {
        private readonly IGameService _gameService;
        private readonly SignalBus _eventBus;
        
        private bool _isGameActive;
        private bool _canPlayerInteract;
        private float _autoStartDelay = 1.0f;
        
        public GameController(IGameService gameService, SignalBus eventBus)
        {
            _gameService = gameService;
            _eventBus = eventBus;
        }
        
        public void Initialize()
        {
            Debug.Log("[GameController] Initializing");
            
            SubscribeToEvents();
            
            // Auto-start game after delay
            StartGameAfterDelay().Forget();
        }
        
        public void Tick()
        {
            if (!_isGameActive || !_canPlayerInteract) return;
            
            // Handle input (TODO: Replace with proper InputManager)
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                HandlePlayerInput();
            }
            
            #if UNITY_EDITOR
            // Debug controls
            if (Input.GetKeyDown(KeyCode.Space))
            {
                HandlePlayerInput();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame().Forget();
            }
            
            if (Input.GetKeyDown(KeyCode.P))
            {
                TogglePause();
            }
            #endif
        }
        
        private void SubscribeToEvents()
        {
            _gameService.OnGameStateChanged += OnGameStateChanged;
            _gameService.OnRoundComplete += OnRoundComplete;
            
            _eventBus.Subscribe<GameStartEvent>(OnGameStart);
            _eventBus.Subscribe<GameEndEvent>(OnGameEnd);
            _eventBus.Subscribe<RoundCompleteEvent>(OnRoundCompleteEvent);
            _eventBus.Subscribe<WarStartEvent>(OnWarStart);
        }
        
        private void UnsubscribeFromEvents()
        {
            _gameService.OnGameStateChanged -= OnGameStateChanged;
            _gameService.OnRoundComplete -= OnRoundComplete;
            
            _eventBus.TryUnsubscribe<GameStartEvent>(OnGameStart);
            _eventBus.TryUnsubscribe<GameEndEvent>(OnGameEnd);
            _eventBus.TryUnsubscribe<RoundCompleteEvent>(OnRoundCompleteEvent);
            _eventBus.TryUnsubscribe<WarStartEvent>(OnWarStart);
        }
        
        private async UniTaskVoid StartGameAfterDelay()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_autoStartDelay));
            await StartNewGame();
        }
        
        private async UniTask StartNewGame()
        {
            Debug.Log("[GameController] Starting new game");
            
            try
            {
                _canPlayerInteract = false;
                await _gameService.StartNewGame();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameController] Failed to start game: {ex.Message}");
                
                // TODO: Show error UI
            }
        }
        
        private async UniTask RestartGame()
        {
            Debug.Log("[GameController] Restarting game");
            
            _gameService.EndGame();
            
            // Small delay before starting new game
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            
            await StartNewGame();
        }
        
        private void HandlePlayerInput()
        {
            if (!_canPlayerInteract || _gameService.CurrentGameState != GameState.Playing)
            {
                Debug.Log($"[GameController] Input ignored. CanInteract: {_canPlayerInteract}, State: {_gameService.CurrentGameState}");
                return;
            }
            
            Debug.Log("[GameController] Processing player input");
            
            _canPlayerInteract = false;
            _eventBus.Fire(new PlayerActionEvent("DrawCard"));
            
            PlayNextRound().Forget();
        }
        
        private async UniTaskVoid PlayNextRound()
        {
            try
            {
                await _gameService.PlayNextRound();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameController] Error playing round: {ex.Message}");
                _canPlayerInteract = true;
                
                // TODO: Show error UI
            }
        }
        
        private void TogglePause()
        {
            if (_gameService.CurrentGameState == GameState.Playing)
            {
                _gameService.PauseGame();
            }
            else if (_gameService.CurrentGameState == GameState.Paused)
            {
                _gameService.ResumeGame();
            }
        }
        
        private void OnGameStateChanged(GameState newState)
        {
            Debug.Log($"[GameController] Game state changed to: {newState}");
            
            switch (newState)
            {
                case GameState.Playing:
                    _canPlayerInteract = true;
                    break;
                    
                case GameState.War:
                case GameState.RoundComplete:
                case GameState.Paused:
                    _canPlayerInteract = false;
                    break;
                    
                case GameState.GameOver:
                    _isGameActive = false;
                    _canPlayerInteract = false;
                    break;
            }
        }
        
        private void OnRoundComplete(Core.Data.GameRoundResultData result)
        {
            Debug.Log($"[GameController] Round complete. Result: {result.Result}, Cards remaining - Player: {_gameService.PlayerCardCount}, Opponent: {_gameService.OpponentCardCount}");
            
            // Re-enable interaction after round animation completes
            if (_gameService.CurrentGameState == GameState.Playing)
            {
                EnableInteractionAfterDelay().Forget();
            }
        }
        
        private async UniTaskVoid EnableInteractionAfterDelay()
        {
            // Wait for animations to complete (TODO: Get actual animation duration)
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
            
            if (_gameService.CurrentGameState == GameState.Playing)
            {
                _canPlayerInteract = true;
            }
        }
        
        private void OnGameStart()
        {
            Debug.Log("[GameController] Game started");
            _isGameActive = true;
            _canPlayerInteract = true;
        }
        
        private void OnGameEnd(GameEndEvent @event)
        {
            Debug.Log($"[GameController] Game ended. Winner: Player {@event.WinnerPlayerNumber}");
            _isGameActive = false;
            _canPlayerInteract = false;
            
            // Auto-restart after delay (TODO: Show game over screen instead)
            ShowGameOverAndRestart(@event.WinnerPlayerNumber).Forget();
        }
        
        private async UniTaskVoid ShowGameOverAndRestart(int winnerPlayerNumber)
        {
            // TODO: Show proper game over UI
            string winner = winnerPlayerNumber == 1 ? "Player" : "Opponent";
            Debug.Log($"[GameController] GAME OVER! {winner} wins!");
            
            // Wait before restarting
            await UniTask.Delay(TimeSpan.FromSeconds(3f));
            
            await RestartGame();
        }
        
        private void OnRoundCompleteEvent(RoundCompleteEvent @event)
        {
            // Additional round complete handling if needed
            // UI updates will be handled by UIManager listening to this Event
        }
        
        private void OnWarStart(WarStartEvent @event)
        {
            Debug.Log($"[GameController] WAR! {@event.WarData.AllWarRounds.Count} rounds of war");
            _canPlayerInteract = false;
            
            // Re-enable interaction after war animation
            EnableInteractionAfterWar(@event.WarData.EstimatedAnimationDuration).Forget();
        }
        
        private async UniTaskVoid EnableInteractionAfterWar(float animationDuration)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(animationDuration));
            
            if (_gameService.CurrentGameState == GameState.Playing)
            {
                _canPlayerInteract = true;
            }
        }
        
        public void Dispose()
        {
            Debug.Log("[GameController] Disposing");
            
            UnsubscribeFromEvents();
            
            if (_isGameActive)
            {
                _gameService.EndGame();
            }
        }
    }
}