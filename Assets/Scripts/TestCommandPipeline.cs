using UnityEngine;
using Cysharp.Threading.Tasks;
using CardWar.Core.Pipeline;
using CardWar.Core.Mediator;
using CardWar.Core.Context;
using CardWar.Core.Commands.System;
using CardWar.Common.States;
using CardWar.Services;
using CardWar.Services.State;

namespace CardWar.Tests
{
    public class TestCommandPipeline : MonoBehaviour
    {
        private ICommandPipeline _pipeline;
        private IGameMediator _mediator;
        private IAppStateManager _appStateManager;
        private IGameStateManager _gameStateManager;
        private IGameStateService _bridgeService;
        
        private async void Start()
        {
            await RunTestsAsync();
        }
        
        private async UniTask RunTestsAsync()
        {
            Debug.Log($"[{GetType().Name}] Starting Command Pipeline Tests...");
            
            await InitializeServices();
            
            if (!await TestServiceRegistration())
                return;
                
            if (!await TestStateTransitionCommands())
                return;
                
            if (!await TestBridgeCompatibility())
                return;
                
            Debug.Log($"[{GetType().Name}] ALL TESTS PASSED - Ready for Phase 2.2");
        }
        
        private async UniTask InitializeServices()
        {
            Debug.Log($"[{GetType().Name}] Initializing test services...");
            
            _pipeline = new CommandPipeline();
            _mediator = new GameMediator();
            
            await UniTask.Delay(100);
            
            _appStateManager = await ServiceLocator.Get<IAppStateManager>();
            _gameStateManager = await ServiceLocator.Get<IGameStateManager>();
            _bridgeService = await ServiceLocator.Get<IGameStateService>();
            
            Debug.Log($"[{GetType().Name}] Services initialized");
        }
        
        private async UniTask<bool> TestServiceRegistration()
        {
            Debug.Log($"[{GetType().Name}] Testing service registration...");
            
            if (_appStateManager == null)
            {
                Debug.LogError($"[{GetType().Name}] FAILED: IAppStateManager not registered");
                return false;
            }
            
            if (_gameStateManager == null)
            {
                Debug.LogError($"[{GetType().Name}] FAILED: IGameStateManager not registered");
                return false;
            }
            
            if (_bridgeService == null)
            {
                Debug.LogError($"[{GetType().Name}] FAILED: IGameStateService bridge not registered");
                return false;
            }
            
            Debug.Log($"[{GetType().Name}] Service registration test PASSED");
            return true;
        }
        
        private async UniTask<bool> TestStateTransitionCommands()
        {
            Debug.Log($"[{GetType().Name}] Testing state transition commands...");
            
            _appStateManager.SetCurrentAppState(AppState.InGame, null);
            _gameStateManager.SetCurrentGameState(GameState.PlayerTurn, null);
            
            var context = GameContext.Create(AppState.InGame, GameState.PlayerTurn);
            
            Debug.Log($"[{GetType().Name}] Testing PauseGameCommand...");
            var pauseCommand = new PauseGameCommand();
            var pauseResult = await _pipeline.ExecuteAsync(pauseCommand, context);
            
            if (!pauseResult.IsSuccess)
            {
                Debug.LogError($"[{GetType().Name}] FAILED: PauseGameCommand failed - {pauseResult.ErrorMessage}");
                return false;
            }
            
            if (pauseResult.Context.GameState != GameState.Paused)
            {
                Debug.LogError($"[{GetType().Name}] FAILED: Game state not changed to Paused");
                return false;
            }
            
            Debug.Log($"[{GetType().Name}] PauseGameCommand PASSED");
            
            Debug.Log($"[{GetType().Name}] Testing ResumeGameCommand...");
            var resumeCommand = new ResumeGameCommand();
            var resumeResult = await _pipeline.ExecuteAsync(resumeCommand, pauseResult.Context);
            
            if (!resumeResult.IsSuccess)
            {
                Debug.LogError($"[{GetType().Name}] FAILED: ResumeGameCommand failed - {resumeResult.ErrorMessage}");
                return false;
            }
            
            if (resumeResult.Context.GameState != GameState.PlayerTurn)
            {
                Debug.LogError($"[{GetType().Name}] FAILED: Game state not changed back to PlayerTurn");
                return false;
            }
            
            Debug.Log($"[{GetType().Name}] ResumeGameCommand PASSED");
            Debug.Log($"[{GetType().Name}] State transition commands test PASSED");
            return true;
        }
        
        private async UniTask<bool> TestBridgeCompatibility()
        {
            Debug.Log($"[{GetType().Name}] Testing bridge backward compatibility...");
            
            var initialState = _bridgeService.CurrentState;
            Debug.Log($"[{GetType().Name}] Initial bridge state: {initialState}");
            
            bool eventFired = false;
            GameState newState = GameState.WaitingToStart;
            
            void OnStateChanged(GameState state)
            {
                eventFired = true;
                newState = state;
            }
            
            _bridgeService.GameStateChanged += OnStateChanged;
            
            _bridgeService.ChangeState(GameState.Paused);
            
            await UniTask.Delay(100);
            
            if (!eventFired)
            {
                Debug.LogError($"[{GetType().Name}] FAILED: Bridge event not fired");
                _bridgeService.GameStateChanged -= OnStateChanged;
                return false;
            }
            
            if (newState != GameState.Paused)
            {
                Debug.LogError($"[{GetType().Name}] FAILED: Bridge state not changed to Paused");
                _bridgeService.GameStateChanged -= OnStateChanged;
                return false;
            }
            
            _bridgeService.GameStateChanged -= OnStateChanged;
            
            Debug.Log($"[{GetType().Name}] Bridge compatibility test PASSED");
            return true;
        }
        
        private void OnDestroy()
        {
            _pipeline?.Dispose();
            _mediator?.Dispose();
        }
    }
}
