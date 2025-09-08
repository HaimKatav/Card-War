# Unity CardWar Command Architecture - Task Breakdown

## Implementation Roadmap
Professional Unity implementation broken into discrete, testable deliverables with clear acceptance criteria and dependencies.

---

## Phase 1: Foundation Layer (Week 1-2)

### Task 1.1: Core Command Infrastructure
**Priority:** Critical | **Estimated Effort:** 2 days | **Dependencies:** None

**Objective:** Implement foundational command pattern interfaces and base classes

**Files to Create:**
```
Assets/Scripts/CardWar/Core/Commands/Base/
├── IGameCommand.cs
├── BaseGameCommand.cs
├── CommandResult.cs
├── CommandPriority.cs
└── GameException.cs
```

**Implementation Requirements:**
```csharp
// IGameCommand.cs - Core interface
public interface IGameCommand
{
    string CommandName { get; }
    CommandPriority Priority { get; }
    UniTask<CommandResult> ExecuteAsync(GameContext context);
    bool CanExecute(GameContext context);
    void OnError(GameException error);
}

// CommandResult.cs - Result wrapper with Unity-safe patterns
public readonly struct CommandResult
{
    public bool IsSuccess { get; }
    public object Data { get; }
    public string Message { get; }
    public CommandResultType Type { get; }
    
    // Factory methods for common results
    public static CommandResult Success(object data = null);
    public static CommandResult Failure(string message);
    public static CommandResult ValidationFailed(string message);
    public static CommandResult Error(Exception exception);
    public static CommandResult Cancelled();
}
```

**Acceptance Criteria:**
- [ ] All interfaces compile without errors in Unity 2022.3+
- [ ] BaseGameCommand provides logging, error handling, and performance tracking
- [ ] CommandResult supports all common game operation outcomes
- [ ] Unit tests verify base functionality
- [ ] Zero GC allocations in hot paths
- [ ] Documentation covers usage patterns

**Testing Requirements:**
- Create test scene with command execution
- Verify async patterns work with Unity lifecycle
- Test error handling and logging output
- Validate memory allocation patterns

---

### Task 1.2: Game Context System
**Priority:** Critical | **Estimated Effort:** 1.5 days | **Dependencies:** Task 1.1

**Objective:** Implement immutable context system for state sharing

**Files to Create:**
```
Assets/Scripts/CardWar/Core/Context/
├── GameContext.cs
├── RoundContext.cs
├── WarContext.cs
└── ContextExtensions.cs
```

**Implementation Requirements:**
```csharp
// GameContext.cs - Immutable context with factory methods
public class GameContext : ICloneable
{
    // Core state (init-only properties)
    public GameState CurrentState { get; init; }
    public GameState PreviousState { get; init; }
    public DateTime StateChangedAt { get; init; }
    
    // Game data
    public Player[] Players { get; init; }
    public GameBoard Board { get; init; }
    public RoundData CurrentRound { get; init; }
    
    // Immutable update methods
    public GameContext WithState(GameState newState);
    public GameContext WithProperty(string key, object value);
    public GameContext WithRound(RoundData round);
    
    // Validation
    public bool IsValid { get; }
    
    // Factory methods
    public static GameContext Create(GameSettings settings, Player[] players);
}
```

**Acceptance Criteria:**
- [ ] Context objects are immutable after creation
- [ ] Factory methods handle all required initialization
- [ ] Validation logic prevents invalid states
- [ ] Clone operations are efficient
- [ ] Thread-safe for async operations
- [ ] Integration with existing Player/GameBoard classes

**Testing Requirements:**
- Test immutability guarantees
- Verify factory method edge cases
- Validate state transition logic
- Performance test context cloning

---

### Task 1.3: Basic Command Pipeline
**Priority:** Critical | **Estimated Effort:** 2 days | **Dependencies:** Task 1.1, 1.2

**Objective:** Create command pipeline with middleware support

**Files to Create:**
```
Assets/Scripts/CardWar/Core/Pipeline/
├── ICommandPipeline.cs
├── CommandPipeline.cs
├── PipelineBuilder.cs
├── IMiddleware.cs
├── ValidationMiddleware.cs
├── LoggingMiddleware.cs
└── PerformanceMiddleware.cs
```

**Implementation Requirements:**
```csharp
// ICommandPipeline.cs
public interface ICommandPipeline : IDisposable
{
    UniTask<CommandResult> ExecuteAsync<T>(T command, GameContext context) where T : IGameCommand;
    UniTask<CommandResult[]> ExecuteBatchAsync(IGameCommand[] commands, GameContext context);
    ICommandPipeline UseMiddleware<T>() where T : IMiddleware;
    PipelineHealth Health { get; }
    void CancelAll();
}

// CommandPipeline.cs - Core implementation
public class CommandPipeline : ICommandPipeline
{
    private readonly List<IMiddleware> _middlewares = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly PerformanceTracker _performanceTracker = new();
    
    public async UniTask<CommandResult> ExecuteAsync<T>(T command, GameContext context) where T : IGameCommand
    {
        // Middleware pre-execution
        // Command execution with error handling
        // Middleware post-execution
        // Performance tracking
    }
}
```

**Acceptance Criteria:**
- [ ] Pipeline executes commands with middleware chain
- [ ] Supports cancellation via CancellationToken
- [ ] Provides performance metrics and health status
- [ ] Handles exceptions gracefully without crashing Unity
- [ ] Middleware can modify execution flow
- [ ] Batch execution for multiple commands

**Testing Requirements:**
- Test middleware execution order
- Verify cancellation scenarios
- Test error propagation through pipeline
- Performance test with various command loads

---

### Task 1.4: Dependency Injection Setup
**Priority:** High | **Estimated Effort:** 1 day | **Dependencies:** Task 1.3

**Objective:** Create Unity-compatible DI container for command architecture

**Files to Create:**
```
Assets/Scripts/CardWar/Core/DI/
├── IServiceContainer.cs
├── ServiceContainer.cs
├── ServiceInstaller.cs
└── DIExtensions.cs
```

**Implementation Requirements:**
```csharp
// ServiceContainer.cs - Unity-compatible DI
public class ServiceContainer : IServiceContainer, IDisposable
{
    private readonly Dictionary<Type, object> _singletons = new();
    private readonly Dictionary<Type, Func<object>> _factories = new();
    
    public void RegisterSingleton<T>(T instance);
    public void RegisterSingleton<TInterface, TImplementation>() 
        where TImplementation : class, TInterface;
    public void RegisterFactory<T>(Func<T> factory);
    
    public T Resolve<T>();
    public bool TryResolve<T>(out T service);
}

// ServiceInstaller.cs - Bootstrap DI for Unity
[CreateAssetMenu(fileName = "ServiceInstaller", menuName = "CardWar/Service Installer")]
public class ServiceInstaller : ScriptableObject
{
    public void InstallServices(IServiceContainer container)
    {
        // Register core services
        container.RegisterSingleton<ICommandPipeline, CommandPipeline>();
        container.RegisterSingleton<IGameMediator, GameMediator>();
        // etc.
    }
}
```

**Acceptance Criteria:**
- [ ] Container supports singleton and factory registration
- [ ] Integrates with Unity ScriptableObject system
- [ ] Provides clear error messages for missing dependencies
- [ ] Supports disposal of managed services
- [ ] Thread-safe for async operations
- [ ] Can resolve complex dependency graphs

**Testing Requirements:**
- Test service registration and resolution
- Verify circular dependency detection
- Test disposal of registered services
- Integration test with Unity lifecycle

---

## Phase 2: Core Commands (Week 3-4)

### Task 2.1: Game Initialization Commands
**Priority:** Critical | **Estimated Effort:** 2 days | **Dependencies:** Phase 1

**Objective:** Implement commands for game setup and loading

**Files to Create:**
```
Assets/Scripts/CardWar/Core/Commands/System/
├── InitializeGameCommand.cs
├── LoadAssetsCommand.cs
├── SetupPlayersCommand.cs
├── SetupBoardCommand.cs
└── ValidateGameStateCommand.cs
```

**Implementation Requirements:**
```csharp
// LoadAssetsCommand.cs
public class LoadAssetsCommand : BaseGameCommand
{
    private readonly IAssetService _assetService;
    
    public override string CommandName => "LoadAssets";
    public override CommandPriority Priority => CommandPriority.System;
    
    public LoadAssetsCommand(IAssetService assetService, ILogger logger) : base(logger)
    {
        _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
    }
    
    protected override async UniTask<CommandResult> ExecuteCore(GameContext context)
    {
        // Load UI Manager
        var uiManagerPrefab = await _assetService.LoadAssetAsync<UIManager>(
            context.Settings.UI_MANAGER_ASSET_PATH);
            
        if (uiManagerPrefab == null)
            return CommandResult.Failure("Failed to load UI Manager");
            
        // Load Game Board
        var boardPrefab = await _assetService.LoadAssetAsync<GameBoardController>(
            context.Settings.PLAY_AREA_ASSET_PATH);
            
        if (boardPrefab == null)
            return CommandResult.Failure("Failed to load Game Board");
            
        // Return loaded assets
        var loadedAssets = new LoadedAssets
        {
            UIManager = uiManagerPrefab,
            GameBoard = boardPrefab
        };
        
        return CommandResult.Success(loadedAssets);
    }
    
    public override bool CanExecute(GameContext context) =>
        context.IsValid && context.Settings != null;
}
```

**Acceptance Criteria:**
- [ ] Commands integrate with existing Asset management
- [ ] Progress reporting during asset loading
- [ ] Proper error handling for missing assets
- [ ] Can execute independently and in sequence
- [ ] Validate loaded assets before completion
- [ ] Support for asset loading cancellation

**Testing Requirements:**
- Test with missing asset paths
- Verify progress reporting accuracy
- Test cancellation during loading
- Integration test with actual Unity assets

---

### Task 2.2: Game Round Commands
**Priority:** Critical | **Estimated Effort:** 3 days | **Dependencies:** Task 2.1

**Objective:** Convert round logic to command pattern

**Files to Create:**
```
Assets/Scripts/CardWar/Core/Commands/Game/
├── PlayRoundCommand.cs
├── DrawCardsCommand.cs
├── CompareCardsCommand.cs
├── AwardCardsCommand.cs
└── UpdateGameStateCommand.cs
```

**Implementation Requirements:**
```csharp
// PlayRoundCommand.cs - Main round orchestration
public class PlayRoundCommand : BaseGameCommand
{
    private readonly IWarServer _warServer;
    private readonly IAnimationService _animationService;
    
    public override string CommandName => "PlayRound";
    
    protected override async UniTask<CommandResult> ExecuteCore(GameContext context)
    {
        // 1. Draw cards from server
        var battleRequest = new BattleRequest
        {
            RoundNumber = context.CurrentRound?.RoundNumber + 1 ?? 1
        };
        
        var battleResult = await _warServer.ProcessBattle(battleRequest);
        if (battleResult == null)
            return CommandResult.Failure("Server battle failed");
            
        // 2. Update context with battle data
        var roundData = new RoundData
        {
            RoundNumber = battleRequest.RoundNumber,
            PlayerCards = battleResult.PlayerCards,
            OpponentCards = battleResult.OpponentCards,
            Winner = battleResult.Winner,
            IsWar = battleResult.IsWar,
            StartedAt = DateTime.UtcNow
        };
        
        var updatedContext = context.WithRound(roundData);
        
        // 3. Animate card reveal
        await _animationService.AnimateCardReveal(
            battleResult.PlayerCards,
            battleResult.OpponentCards
        );
        
        // 4. Handle war scenario if needed
        if (battleResult.IsWar)
        {
            return CommandResult.Success(new RoundResult
            {
                IsWar = true,
                Context = updatedContext
            });
        }
        
        // 5. Complete normal round
        var completedRound = roundData with { 
            IsComplete = true, 
            CompletedAt = DateTime.UtcNow 
        };
        
        return CommandResult.Success(new RoundResult
        {
            IsComplete = true,
            Context = updatedContext.WithRound(completedRound)
        });
    }
    
    public override bool CanExecute(GameContext context) =>
        context.IsValid && 
        context.CurrentState == GameState.Playing &&
        !context.CurrentRound?.IsComplete == true;
}
```

**Acceptance Criteria:**
- [ ] Integrates with existing FakeWarServer
- [ ] Supports both normal rounds and war scenarios
- [ ] Provides animated feedback through IAnimationService
- [ ] Updates game context appropriately
- [ ] Handles server communication errors
- [ ] Maintains compatibility with existing round logic

**Testing Requirements:**
- Test normal round completion
- Test war scenario triggering
- Test server communication failures
- Verify context updates are correct
- Integration test with animation system

---

### Task 2.3: War Handling Commands
**Priority:** High | **Estimated Effort:** 2 days | **Dependencies:** Task 2.2

**Objective:** Implement war scenario command handling

**Files to Create:**
```
Assets/Scripts/CardWar/Core/Commands/War/
├── HandleWarCommand.cs
├── PlaceWarCardsCommand.cs
├── ResolveWarCommand.cs
└── CheckWarEndCommand.cs
```

**Implementation Requirements:**
```csharp
// HandleWarCommand.cs
public class HandleWarCommand : BaseGameCommand
{
    private readonly IWarServer _warServer;
    private readonly IAnimationService _animationService;
    
    public override string CommandName => "HandleWar";
    
    protected override async UniTask<CommandResult> ExecuteCore(GameContext context)
    {
        if (!context.CurrentRound.IsWar)
            return CommandResult.ValidationFailed("Not in war state");
            
        // 1. Place war cards (3 face down, 1 face up)
        var warRequest = new WarRequest
        {
            RoundNumber = context.CurrentRound.RoundNumber,
            WarNumber = (context.CurrentWar?.WarNumber ?? 0) + 1
        };
        
        var warResult = await _warServer.ProcessWar(warRequest);
        if (warResult == null)
            return CommandResult.Failure("War processing failed");
            
        // 2. Animate war card placement
        await _animationService.AnimateWarCardPlacement(
            warResult.PlayerWarCards,
            warResult.OpponentWarCards
        );
        
        // 3. Update war context
        var warData = new WarData
        {
            WarNumber = warRequest.WarNumber,
            PlayerWarCards = warResult.PlayerWarCards,
            OpponentWarCards = warResult.OpponentWarCards,
            IsResolved = !warResult.IsAnotherWar,
            Winner = warResult.Winner
        };
        
        var updatedContext = context.WithWar(warData);
        
        // 4. Check for another war
        if (warResult.IsAnotherWar)
        {
            return CommandResult.Success(new WarResult
            {
                IsAnotherWar = true,
                Context = updatedContext
            });
        }
        
        // 5. War resolved, award all cards
        await _animationService.AnimateCardCollection(
            warResult.AllCards,
            warResult.Winner
        );
        
        return CommandResult.Success(new WarResult
        {
            IsResolved = true,
            Winner = warResult.Winner,
            Context = updatedContext
        });
    }
    
    public override bool CanExecute(GameContext context) =>
        context.IsValid &&
        context.CurrentRound?.IsWar == true &&
        context.CurrentState == GameState.Playing;
}
```

**Acceptance Criteria:**
- [ ] Handles single and multiple war scenarios
- [ ] Integrates with existing war server logic
- [ ] Provides proper animation feedback
- [ ] Updates context with war data
- [ ] Handles insufficient cards for war
- [ ] Maintains game state consistency

**Testing Requirements:**
- Test single war scenario
- Test multiple consecutive wars
- Test insufficient cards edge case
- Verify animation integration
- Test context state management

---

## Phase 3: Orchestrators (Week 5-6)

### Task 3.1: Load Game Orchestrator
**Priority:** Critical | **Estimated Effort:** 2 days | **Dependencies:** Phase 2

**Objective:** Create orchestrator for complete game loading workflow

**Files to Create:**
```
Assets/Scripts/CardWar/Core/Orchestrators/
├── BaseOrchestrator.cs
├── LoadGameOrchestrator.cs
└── OrchestratorResult.cs
```

**Implementation Requirements:**
```csharp
// LoadGameOrchestrator.cs
public class LoadGameOrchestrator : BaseOrchestrator
{
    public LoadGameOrchestrator(
        IGameMediator mediator, 
        ICommandPipeline pipeline, 
        ILogger logger) : base(mediator, pipeline, logger) { }
    
    public override async UniTask<OrchestratorResult> ExecuteAsync(GameContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            NotifyProgress("Starting game load", 0f);
            
            // Step 1: Validate initial state
            var validateCommand = new ValidateGameStateCommand();
            var validateResult = await ExecuteCommand(validateCommand, context);
            
            if (!validateResult.IsSuccess)
                return OrchestratorResult.Failure($"Validation failed: {validateResult.Message}");
                
            NotifyProgress("Validation complete", 0.1f);
            
            // Step 2: Load assets
            var loadAssetsCommand = new LoadAssetsCommand();
            var assetsResult = await ExecuteCommand(loadAssetsCommand, context);
            
            if (!assetsResult.IsSuccess)
                return OrchestratorResult.Failure($"Asset loading failed: {assetsResult.Message}");
                
            var loadedAssets = (LoadedAssets)assetsResult.Data;
            NotifyProgress("Assets loaded", 0.4f);
            
            // Step 3: Setup players
            var setupPlayersCommand = new SetupPlayersCommand();
            var playersResult = await ExecuteCommand(setupPlayersCommand, context);
            
            if (!playersResult.IsSuccess)
                return OrchestratorResult.Failure($"Player setup failed: {playersResult.Message}");
                
            NotifyProgress("Players initialized", 0.6f);
            
            // Step 4: Setup game board
            var setupBoardCommand = new SetupBoardCommand(loadedAssets.GameBoard);
            var boardResult = await ExecuteCommand(setupBoardCommand, context);
            
            if (!boardResult.IsSuccess)
                return OrchestratorResult.Failure($"Board setup failed: {boardResult.Message}");
                
            NotifyProgress("Board initialized", 0.8f);
            
            // Step 5: Initialize UI
            var setupUICommand = new SetupUICommand(loadedAssets.UIManager);
            var uiResult = await ExecuteCommand(setupUICommand, context);
            
            if (!uiResult.IsSuccess)
                return OrchestratorResult.Failure($"UI setup failed: {uiResult.Message}");
                
            NotifyProgress("Game load complete", 1f);
            NotifyStepComplete("GameLoaded", new { Duration = stopwatch.Elapsed });
            
            stopwatch.Stop();
            return OrchestratorResult.Success(
                data: new { LoadedAssets = loadedAssets },
                duration: stopwatch.Elapsed
            );
        }
        catch (OperationCanceledException)
        {
            Logger.Log("[LoadGameOrchestrator] Load cancelled");
            return OrchestratorResult.Failure("Game load was cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError($"[LoadGameOrchestrator] Unexpected error: {ex}");
            return OrchestratorResult.Failure($"Unexpected error: {ex.Message}");
        }
    }
}
```

**Acceptance Criteria:**
- [ ] Orchestrates complete game loading sequence
- [ ] Provides detailed progress reporting
- [ ] Handles cancellation gracefully
- [ ] Integrates with existing game loading logic
- [ ] Reports success/failure with meaningful messages
- [ ] Supports retry on recoverable failures

**Testing Requirements:**
- Test complete successful load sequence
- Test failure scenarios at each step
- Verify progress reporting accuracy
- Test cancellation during various steps
- Integration test with Unity asset loading

---

### Task 3.2: Round Orchestrator
**Priority:** Critical | **Estimated Effort:** 2.5 days | **Dependencies:** Task 3.1

**Objective:** Create orchestrator for round execution including war handling

**Files to Create:**
```
Assets/Scripts/CardWar/Core/Orchestrators/
├── RoundOrchestrator.cs
├── WarOrchestrator.cs
└── GameEndOrchestrator.cs
```

**Implementation Requirements:**
```csharp
// RoundOrchestrator.cs
public class RoundOrchestrator : BaseOrchestrator
{
    private readonly WarOrchestrator _warOrchestrator;
    
    public RoundOrchestrator(
        IGameMediator mediator,
        ICommandPipeline pipeline,
        WarOrchestrator warOrchestrator,
        ILogger logger) : base(mediator, pipeline, logger)
    {
        _warOrchestrator = warOrchestrator ?? throw new ArgumentNullException(nameof(warOrchestrator));
    }
    
    public override async UniTask<OrchestratorResult> ExecuteAsync(GameContext context)
    {
        try
        {
            NotifyProgress("Starting round", 0f);
            
            // Step 1: Play round
            var playRoundCommand = new PlayRoundCommand();
            var roundResult = await ExecuteCommand(playRoundCommand, context);
            
            if (!roundResult.IsSuccess)
                return OrchestratorResult.Failure($"Round failed: {roundResult.Message}");
                
            var roundData = (RoundResult)roundResult.Data;
            var updatedContext = roundData.Context;
            
            NotifyProgress("Cards drawn and compared", 0.5f);
            
            // Step 2: Handle war if needed
            if (roundData.IsWar)
            {
                var warResult = await _warOrchestrator.ExecuteAsync(updatedContext);
                if (!warResult.IsSuccess)
                    return OrchestratorResult.Failure($"War handling failed: {warResult.Message}");
                    
                updatedContext = ((WarResult)warResult.Data).Context;
            }
            
            NotifyProgress("Round resolution complete", 0.8f);
            
            // Step 3: Update game state
            var updateStateCommand = new UpdateGameStateCommand();
            var stateResult = await ExecuteCommand(updateStateCommand, updatedContext);
            
            if (!stateResult.IsSuccess)
                return OrchestratorResult.Failure($"State update failed: {stateResult.Message}");
                
            NotifyProgress("Round complete", 1f);
            
            // Step 4: Check for game end
            if (IsGameOver(updatedContext))
            {
                NotifyStepComplete("GameEnded", new { Winner = GetWinner(updatedContext) });
            }
            else
            {
                NotifyStepComplete("RoundComplete", new { RoundNumber = updatedContext.CurrentRound.RoundNumber });
            }
            
            return OrchestratorResult.Success(updatedContext);
        }
        catch (Exception ex)
        {
            Logger.LogError($"[RoundOrchestrator] Error: {ex}");
            return OrchestratorResult.Failure($"Round orchestration failed: {ex.Message}");
        }
    }
    
    private bool IsGameOver(GameContext context) =>
        context.Players[0].CardCount == 0 || context.Players[1].CardCount == 0;
        
    private Player GetWinner(GameContext context) =>
        context.Players.FirstOrDefault(p => p.CardCount > 0);
}
```

**Acceptance Criteria:**
- [ ] Orchestrates complete round including war scenarios
- [ ] Integrates with war orchestrator for complex scenarios
- [ ] Detects game end conditions
- [ ] Provides appropriate progress and completion notifications
- [ ] Handles errors gracefully without corrupting game state
- [ ] Maintains performance for frequent round execution

**Testing Requirements:**
- Test normal round completion
- Test war scenario handling
- Test game end detection
- Verify state consistency after rounds
- Performance test for many consecutive rounds

---

## Phase 4: Manager Integration (Week 7-8)

### Task 4.1: GameManager Refactoring
**Priority:** Critical | **Estimated Effort:** 2 days | **Dependencies:** Phase 3

**Objective:** Update GameManager to use orchestrators instead of direct events

**Files to Modify:**
```
Assets/Scripts/Managers/GameManager.cs
```

**Implementation Changes:**
```csharp
public class GameManager : BaseService, IGameStateService
{
    // Remove event subscriptions, add orchestrator dependencies
    private LoadGameOrchestrator _loadGameOrchestrator;
    private IGameMediator _mediator;
    private ICommandPipeline _pipeline;
    
    // Replace direct method calls with orchestrator execution
    private async UniTaskVoid LoadGame()
    {
        Debug.Log("[GameManager] Entering LoadingGame state");
        ChangeState(GameState.LoadingGame);
        
        var context = CreateInitialGameContext();
        var result = await _loadGameOrchestrator.ExecuteAsync(context);
        
        if (!result.IsSuccess)
        {
            Debug.LogError($"[GameManager] Failed to load game: {result.Message}");
            ChangeState(GameState.MainMenu);
            return;
        }
        
        Debug.Log("[GameManager] Game loaded successfully");
        ChangeState(GameState.Playing);
    }
    
    // Subscribe to mediator notifications instead of direct events
    private void RegisterMediatorHandlers()
    {
        _mediator.Subscribe<ProgressNotification>(HandleLoadingProgress);
        _mediator.Subscribe<StepCompleteNotification>(HandleStepComplete);
        _mediator.Subscribe<GameEndNotification>(HandleGameEnd);
    }
}
```

**Acceptance Criteria:**
- [ ] Removes all direct event subscriptions
- [ ] Uses orchestrators for complex operations
- [ ] Subscribes to mediator notifications
- [ ] Maintains existing public interface
- [ ] Preserves all current functionality
- [ ] Improves error handling and logging

---

### Task 4.2: GameController Integration
**Priority:** Critical | **Estimated Effort:** 2 days | **Dependencies:** Task 4.1

**Objective:** Update GameController to use round orchestrator

**Files to Modify:**
```
Assets/Scripts/Game/GameController.cs
```

**Implementation Changes:**
```csharp
public class GameController : MonoBehaviour
{
    private RoundOrchestrator _roundOrchestrator;
    private GameContext _currentContext;
    
    // Replace direct round handling with orchestrator
    private async void OnDrawButtonPressed()
    {
        if (_isProcessingRound || !_isGameActive)
            return;
            
        _isProcessingRound = true;
        
        try
        {
            var result = await _roundOrchestrator.ExecuteAsync(_currentContext);
            
            if (result.IsSuccess)
            {
                _currentContext = (GameContext)result.Data;
                Debug.Log($"[GameController] Round completed successfully");
            }
            else
            {
                Debug.LogError($"[GameController] Round failed: {result.Message}");
                // Handle round failure
            }
        }
        finally
        {
            _isProcessingRound = false;
        }
    }
}
```

**Acceptance Criteria:**
- [ ] Uses round orchestrator for round execution
- [ ] Maintains game context state
- [ ] Preserves existing button handling
- [ ] Handles orchestrator errors appropriately
- [ ] Maintains performance for round execution
- [ ] Integrates with existing UI feedback

---

### Task 4.3: Final Integration Testing
**Priority:** High | **Estimated Effort:** 2 days | **Dependencies:** Task 4.2

**Objective:** Comprehensive testing of complete integrated system

**Testing Scenarios:**
1. **Complete Game Flow Testing**
   - Start from main menu
   - Load game through orchestrator
   - Play multiple rounds including wars
   - Complete game to end state
   - Return to main menu

2. **Error Scenario Testing**
   - Asset loading failures
   - Server communication errors
   - Animation system failures
   - Memory pressure scenarios
   - Network interruption simulation

3. **Performance Testing**
   - Frame rate impact during command execution
   - Memory allocation patterns
   - GC pressure from context objects
   - Large number of consecutive rounds

4. **Cancellation Testing**
   - Cancel during game loading
   - App focus loss during operations
   - User-initiated cancellation

**Acceptance Criteria:**
- [ ] All existing game functionality preserved
- [ ] No performance regression from baseline
- [ ] Graceful error handling in all scenarios
- [ ] Proper cleanup and disposal
- [ ] Comprehensive logging for debugging
- [ ] Ready for production deployment

---

## Success Metrics

### Functional Requirements
- ✅ Game plays identically to current version
- ✅ All existing features work (pause, resume, war, game end)
- ✅ App focus handling maintained
- ✅ Loading progress reporting preserved
- ✅ Error scenarios handled gracefully

### Technical Requirements
- ✅ Event coupling reduced by >90%
- ✅ Async operations properly coordinated
- ✅ Clear separation of concerns achieved
- ✅ Testability improved significantly
- ✅ Memory usage maintained or improved

### Performance Requirements
- ✅ No frame rate degradation
- ✅ Startup time maintained
- ✅ Memory allocation patterns optimized
- ✅ Responsive UI during operations

This task breakdown provides concrete, actionable steps for implementing the Command-Pipeline-Mediator architecture in Unity with professional development standards.