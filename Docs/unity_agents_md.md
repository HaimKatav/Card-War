# AGENTS.md - Unity CardWar Command-Pipeline-Mediator Architecture

## Project Context
Unity 2022.3+ LTS CardWar game implementing Command-Pipeline-Mediator pattern with UniTask async operations. This is a **professional Unity project** with strict architectural constraints.

## Critical Unity Patterns

### MonoBehaviour Initialization
```csharp
// CORRECT: Dependency injection through Initialize method
public class GameController : MonoBehaviour
{
    private IGameMediator _mediator;
    private ICommandPipeline _pipeline;
    
    public void Initialize(IGameMediator mediator, ICommandPipeline pipeline)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        // Setup after dependencies are injected
    }
}

// WRONG: Never use FindObjectOfType or GetComponent for dependencies
```

### Async Operations with UniTask
```csharp
// CORRECT: UniTask for Unity async operations
public async UniTask<CommandResult> ExecuteAsync(GameContext context)
{
    await UniTask.Delay(100); // Unity-safe delay
    return CommandResult.Success();
}

// Use .Forget() for fire-and-forget operations
InitializeAsync().Forget();
```

### Unity Execution Order
1. **Bootstrap Phase**: ClientInstaller initializes DI container
2. **Service Registration**: Core services registered in specific order
3. **Manager Initialization**: GameManager, UIManager via Initialize()
4. **Game State**: State machine transitions begin

## Architecture Implementation Rules

### Command Pattern (Unity-Specific)
```csharp
namespace CardWar.Core.Commands
{
    public interface IGameCommand
    {
        UniTask<CommandResult> ExecuteAsync(GameContext context);
        bool CanExecute(GameContext context);
        string CommandName { get; }
        void OnError(GameException error);
    }
    
    public abstract class BaseGameCommand : IGameCommand
    {
        protected ILogger Logger { get; }
        protected GameContext Context { get; private set; }
        
        public abstract string CommandName { get; }
        
        protected BaseGameCommand(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async UniTask<CommandResult> ExecuteAsync(GameContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            
            if (!CanExecute(context))
                return CommandResult.Failure($"{CommandName} cannot execute");
                
            try
            {
                Logger.Log($"[{CommandName}] Starting execution");
                var result = await ExecuteCore(context);
                Logger.Log($"[{CommandName}] Completed: {result.IsSuccess}");
                return result;
            }
            catch (Exception ex)
            {
                var gameEx = new GameException($"{CommandName} failed", ex);
                OnError(gameEx);
                return CommandResult.Error(gameEx);
            }
        }
        
        protected abstract UniTask<CommandResult> ExecuteCore(GameContext context);
        public abstract bool CanExecute(GameContext context);
        public virtual void OnError(GameException error) => Logger.LogError(error.Message);
    }
}
```

### Pipeline Implementation (Unity Performance Optimized)
```csharp
namespace CardWar.Core.Pipeline
{
    public class CommandPipeline : ICommandPipeline, IDisposable
    {
        private readonly List<IMiddleware> _middlewares;
        private readonly IGameErrorHandler _errorHandler;
        private readonly CancellationTokenSource _cancellationSource;
        
        public async UniTask<CommandResult> ExecuteAsync<T>(T command, GameContext context) 
            where T : IGameCommand
        {
            using var activity = Activity.StartActivity($"Pipeline.{command.CommandName}");
            
            try
            {
                // Pre-execution middleware
                foreach (var middleware in _middlewares)
                {
                    var middlewareResult = await middleware.BeforeExecute(command, context);
                    if (!middlewareResult.ShouldContinue)
                        return middlewareResult.Result;
                }
                
                // Execute command
                var result = await command.ExecuteAsync(context);
                
                // Post-execution middleware
                foreach (var middleware in _middlewares.AsEnumerable().Reverse())
                {
                    await middleware.AfterExecute(command, context, result);
                }
                
                return result;
            }
            catch (OperationCanceledException)
            {
                return CommandResult.Cancelled();
            }
            catch (Exception ex)
            {
                return await _errorHandler.HandleAsync(ex, command, context);
            }
        }
        
        public void Dispose()
        {
            _cancellationSource?.Dispose();
        }
    }
}
```

### Orchestrator Pattern (Unity Lifecycle Aware)
```csharp
namespace CardWar.Core.Orchestrators
{
    public abstract class BaseOrchestrator : IDisposable
    {
        protected IGameMediator Mediator { get; }
        protected ICommandPipeline Pipeline { get; }
        protected ILogger Logger { get; }
        
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        protected CancellationToken CancellationToken => _cancellationTokenSource.Token;
        
        protected BaseOrchestrator(IGameMediator mediator, ICommandPipeline pipeline, ILogger logger)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public abstract UniTask<OrchestratorResult> ExecuteAsync(GameContext context);
        
        protected async UniTask<T> ExecuteCommand<T>(IGameCommand command, GameContext context) 
            where T : CommandResult
        {
            CancellationToken.ThrowIfCancellationRequested();
            return await Pipeline.ExecuteAsync(command, context) as T;
        }
        
        protected void NotifyProgress(string step, float progress = 0f)
        {
            var notification = new ProgressNotification(step, progress);
            Mediator.Publish(notification);
        }
        
        public virtual void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
}
```

## File Organization Standards

### Namespace Structure
```
CardWar.Core.Commands      // Command implementations
├── Base/                  // IGameCommand, BaseGameCommand
├── Game/                  // Game-specific commands
├── System/                // System-level commands
└── War/                   // War-specific commands

CardWar.Core.Pipeline      // Pipeline execution
├── ICommandPipeline.cs
├── CommandPipeline.cs
├── PipelineBuilder.cs
└── Middleware/           // Pipeline middleware

CardWar.Core.Orchestrators // Workflow orchestration
├── Base/                 // Base orchestrator
├── Game/                 // Game orchestrators
└── System/               // System orchestrators

CardWar.Core.Context      // Context objects
CardWar.Core.Mediator     // Mediator pattern
CardWar.Core.ErrorHandling // Error handling
```

### Unity-Specific File Naming
- MonoBehaviours: `GameController.cs`, `UIManager.cs`
- ScriptableObjects: `GameSettings.cs`, `CardData.cs`
- Interfaces: `IGameService.cs`, `ICommandPipeline.cs`
- Commands: `LoadGameCommand.cs`, `PlayRoundCommand.cs`
- Orchestrators: `LoadGameOrchestrator.cs`, `RoundOrchestrator.cs`

## Unity Development Constraints

### Memory Management
```csharp
// REQUIRED: Implement IDisposable for long-lived objects
public class GameMediator : IGameMediator, IDisposable
{
    private readonly Dictionary<Type, List<object>> _handlers = new();
    private bool _disposed = false;
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _handlers.Clear();
            _disposed = true;
        }
    }
}

// REQUIRED: Null checks before operations
public bool CanExecute(GameContext context)
{
    return context?.CurrentState != null && context.IsValid;
}
```

### Unity Logging Format
```csharp
// REQUIRED: Consistent logging format
Debug.Log($"[{GetType().Name}] {message}");
Debug.LogWarning($"[{GetType().Name}] Warning: {warning}");
Debug.LogError($"[{GetType().Name}] Error: {error}");
```

### ScriptableObject Configuration
```csharp
[CreateAssetMenu(fileName = "GameSettings", menuName = "CardWar/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Asset Paths")]
    public string UI_MANAGER_ASSET_PATH = "Managers/UIManager";
    public string PLAY_AREA_ASSET_PATH = "Game/PlayArea";
    
    [Header("Game Configuration")]
    public int CARDS_PER_PLAYER = 26;
    public float ANIMATION_DURATION = 0.5f;
}
```

## Implementation Phases

### Phase 1: Foundation (Create These Files First)
1. `IGameCommand.cs` - Core command interface
2. `BaseGameCommand.cs` - Abstract base implementation
3. `CommandResult.cs` - Result wrapper
4. `GameContext.cs` - Shared state container
5. `ICommandPipeline.cs` - Pipeline interface
6. `CommandPipeline.cs` - Basic pipeline implementation

### Phase 2: Core Commands (Game Logic)
1. `LoadGameCommand.cs` - Game initialization
2. `PlayRoundCommand.cs` - Round execution
3. `HandleWarCommand.cs` - War scenario handling
4. `ValidateGameStateCommand.cs` - State validation
5. `UpdateUICommand.cs` - UI updates

### Phase 3: Orchestrators (Workflow Management)
1. `LoadGameOrchestrator.cs` - Coordinate game loading
2. `RoundOrchestrator.cs` - Manage round flow
3. `WarOrchestrator.cs` - Handle war scenarios
4. `PauseOrchestrator.cs` - Pause/resume flow

### Phase 4: Integration (Unity Managers)
1. Update `GameManager.cs` - Use orchestrators
2. Update `GameController.cs` - Remove direct events
3. Update `UIManager.cs` - Subscribe to mediator
4. Create `ServiceInstaller.cs` - DI setup

## Testing Requirements

### Unit Testing Pattern
```csharp
[Test]
public async Task LoadGameCommand_ValidContext_ReturnsSuccess()
{
    // Arrange
    var mockAssetService = new Mock<IAssetService>();
    var command = new LoadGameCommand(mockAssetService.Object);
    var context = CreateValidGameContext();
    
    // Act
    var result = await command.ExecuteAsync(context);
    
    // Assert
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.Data, Is.Not.Null);
}
```

### Integration Testing Pattern
```csharp
[Test]
public async Task RoundOrchestrator_NormalBattle_UpdatesGameState()
{
    // Arrange
    var orchestrator = CreateRoundOrchestrator();
    var context = CreateGameContextWithPlayers();
    
    // Act
    var result = await orchestrator.ExecuteAsync(context);
    
    // Assert
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(context.CurrentRound.IsComplete, Is.True);
}
```

## Performance Guidelines

### Unity-Specific Optimizations
- **Pool Commands**: Reuse command instances for frequent operations
- **Batch UI Updates**: Group UI changes in single frame
- **Async State Management**: Use UniTask for non-blocking operations
- **Memory Allocation**: Minimize allocations in Update() loops
- **Cancellation Tokens**: Support cancellation for long-running operations

### Debug Performance Tracking
```csharp
#if UNITY_EDITOR
[Conditional("UNITY_EDITOR")]
private void TrackPerformance(string operation, System.Action action)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    action();
    stopwatch.Stop();
    
    if (stopwatch.ElapsedMilliseconds > 16) // Frame budget exceeded
    {
        Debug.LogWarning($"[Performance] {operation} took {stopwatch.ElapsedMilliseconds}ms");
    }
}
#endif
```

## Error Handling Strategy

### Unity Exception Hierarchy
```csharp
public class GameException : Exception
{
    public GameErrorType ErrorType { get; }
    public GameContext Context { get; }
    
    public GameException(string message, GameErrorType errorType, GameContext context = null) 
        : base(message)
    {
        ErrorType = errorType;
        Context = context;
    }
}

public enum GameErrorType
{
    ValidationFailed,
    AssetLoadFailed,
    NetworkError,
    StateTransitionFailed,
    AnimationFailed
}
```

### Recovery Strategies
```csharp
public class GameErrorHandler : IGameErrorHandler
{
    public async UniTask<CommandResult> HandleAsync(Exception exception, IGameCommand command, GameContext context)
    {
        switch (exception)
        {
            case GameException gameEx when gameEx.ErrorType == GameErrorType.AssetLoadFailed:
                return await AttemptAssetRecovery(gameEx, context);
                
            case OperationCanceledException:
                return CommandResult.Cancelled();
                
            default:
                Debug.LogError($"[GameErrorHandler] Unhandled exception in {command.CommandName}: {exception}");
                return CommandResult.Error(exception);
        }
    }
}
```

## Build Configuration

### Development Build
- Enable debug logging
- Include performance profiling
- Validate all state transitions
- Enable command history tracking

### Release Build
- Disable debug features
- Optimize command pipeline
- Remove development-only validations
- Enable error telemetry only

## Integration Points

### Existing Codebase Integration
1. **GameManager**: Replace event subscriptions with orchestrator calls
2. **GameController**: Wrap existing logic in commands
3. **FakeWarServer**: Adapt server calls into commands
4. **GameBoardController**: Create animation commands
5. **UIManager**: Subscribe to mediator notifications

### Backwards Compatibility
- Maintain existing public interfaces during transition
- Gradually replace internal implementations
- Keep original event system as fallback during migration
- Provide adapter classes for smooth transition

This architecture ensures Unity best practices while implementing the Command-Pipeline-Mediator pattern professionally.