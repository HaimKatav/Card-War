# Unity CardWar Command-Pipeline-Mediator Architecture Specification

## Executive Summary
Professional Unity implementation of Command-Pipeline-Mediator pattern for CardWar game, replacing event-driven architecture with structured async command execution while maintaining Unity performance standards.

## Core Architecture Patterns

### 1. Command Pattern Implementation

#### Interface Design
```csharp
namespace CardWar.Core.Commands
{
    /// <summary>
    /// Core command interface for all game operations
    /// Supports async execution with Unity-safe patterns
    /// </summary>
    public interface IGameCommand
    {
        /// <summary>Command identifier for logging and debugging</summary>
        string CommandName { get; }
        
        /// <summary>Execute command with given context</summary>
        UniTask<CommandResult> ExecuteAsync(GameContext context);
        
        /// <summary>Pre-execution validation</summary>
        bool CanExecute(GameContext context);
        
        /// <summary>Error handling callback</summary>
        void OnError(GameException error);
        
        /// <summary>Command priority for pipeline ordering</summary>
        CommandPriority Priority { get; }
    }
    
    public enum CommandPriority
    {
        System = 0,     // Boot, shutdown commands
        Critical = 1,   // Game state transitions
        Normal = 2,     // Regular game operations
        Background = 3  // Non-essential operations
    }
}
```

#### Base Implementation
```csharp
public abstract class BaseGameCommand : IGameCommand
{
    private readonly ILogger _logger;
    private readonly string _commandId;
    
    public abstract string CommandName { get; }
    public virtual CommandPriority Priority => CommandPriority.Normal;
    
    protected BaseGameCommand(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _commandId = Guid.NewGuid().ToString("N")[..8]; // Short ID for tracking
    }
    
    public async UniTask<CommandResult> ExecuteAsync(GameContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Pre-execution logging
            _logger.Log($"[{CommandName}:{_commandId}] Starting execution");
            
            // Validation
            if (!CanExecute(context))
            {
                var error = $"Command {CommandName} validation failed";
                _logger.LogWarning($"[{CommandName}:{_commandId}] {error}");
                return CommandResult.ValidationFailed(error);
            }
            
            // Execute core logic
            var result = await ExecuteCore(context);
            
            // Performance tracking
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 100) // Warn on slow commands
            {
                _logger.LogWarning($"[{CommandName}:{_commandId}] Slow execution: {stopwatch.ElapsedMilliseconds}ms");
            }
            
            _logger.Log($"[{CommandName}:{_commandId}] Completed in {stopwatch.ElapsedMilliseconds}ms");
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.Log($"[{CommandName}:{_commandId}] Cancelled");
            return CommandResult.Cancelled();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var gameException = new GameException($"{CommandName} execution failed", ex);
            OnError(gameException);
            _logger.LogError($"[{CommandName}:{_commandId}] Failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
            return CommandResult.Error(gameException);
        }
    }
    
    protected abstract UniTask<CommandResult> ExecuteCore(GameContext context);
    public abstract bool CanExecute(GameContext context);
    public virtual void OnError(GameException error) { }
}
```

### 2. Pipeline Architecture

#### Pipeline Interface
```csharp
namespace CardWar.Core.Pipeline
{
    public interface ICommandPipeline : IDisposable
    {
        /// <summary>Execute command through pipeline with middleware</summary>
        UniTask<CommandResult> ExecuteAsync<T>(T command, GameContext context) where T : IGameCommand;
        
        /// <summary>Execute multiple commands in sequence</summary>
        UniTask<CommandResult[]> ExecuteBatchAsync(IGameCommand[] commands, GameContext context);
        
        /// <summary>Add middleware to pipeline</summary>
        ICommandPipeline UseMiddleware<T>() where T : IMiddleware;
        
        /// <summary>Pipeline health status</summary>
        PipelineHealth Health { get; }
        
        /// <summary>Cancel all pending operations</summary>
        void CancelAll();
    }
    
    public struct PipelineHealth
    {
        public bool IsHealthy { get; init; }
        public int PendingCommands { get; init; }
        public TimeSpan AverageExecutionTime { get; init; }
        public int FailedCommands { get; init; }
    }
}
```

#### Middleware System
```csharp
public interface IMiddleware
{
    /// <summary>Execute before command</summary>
    UniTask<MiddlewareResult> BeforeExecute<T>(T command, GameContext context) where T : IGameCommand;
    
    /// <summary>Execute after command</summary>
    UniTask AfterExecute<T>(T command, GameContext context, CommandResult result) where T : IGameCommand;
}

public struct MiddlewareResult
{
    public bool ShouldContinue { get; init; }
    public CommandResult Result { get; init; }
    
    public static MiddlewareResult Continue() => new() { ShouldContinue = true };
    public static MiddlewareResult Stop(CommandResult result) => new() { ShouldContinue = false, Result = result };
}

// Built-in middleware implementations
public class ValidationMiddleware : IMiddleware
{
    public async UniTask<MiddlewareResult> BeforeExecute<T>(T command, GameContext context) where T : IGameCommand
    {
        if (!command.CanExecute(context))
        {
            return MiddlewareResult.Stop(CommandResult.ValidationFailed($"{command.CommandName} validation failed"));
        }
        return MiddlewareResult.Continue();
    }
    
    public async UniTask AfterExecute<T>(T command, GameContext context, CommandResult result) where T : IGameCommand
    {
        // Post-execution validation if needed
    }
}

public class PerformanceMiddleware : IMiddleware
{
    private readonly Dictionary<string, PerformanceMetrics> _metrics = new();
    
    public async UniTask<MiddlewareResult> BeforeExecute<T>(T command, GameContext context) where T : IGameCommand
    {
        // Start performance tracking
        return MiddlewareResult.Continue();
    }
    
    public async UniTask AfterExecute<T>(T command, GameContext context, CommandResult result) where T : IGameCommand
    {
        // Record performance metrics
    }
}
```

### 3. Orchestrator Architecture

#### Base Orchestrator Design
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
        
        /// <summary>Main orchestration logic</summary>
        public abstract UniTask<OrchestratorResult> ExecuteAsync(GameContext context);
        
        /// <summary>Execute command with error handling</summary>
        protected async UniTask<CommandResult> ExecuteCommand<T>(T command, GameContext context) where T : IGameCommand
        {
            CancellationToken.ThrowIfCancellationRequested();
            return await Pipeline.ExecuteAsync(command, context);
        }
        
        /// <summary>Execute multiple commands in parallel</summary>
        protected async UniTask<CommandResult[]> ExecuteParallel(IGameCommand[] commands, GameContext context)
        {
            var tasks = commands.Select(cmd => Pipeline.ExecuteAsync(cmd, context)).ToArray();
            return await UniTask.WhenAll(tasks);
        }
        
        /// <summary>Notify orchestration progress</summary>
        protected void NotifyProgress(string step, float progress = 0f)
        {
            var notification = new ProgressNotification(step, progress);
            Mediator.Publish(notification);
            Logger.Log($"[{GetType().Name}] Progress: {step} ({progress:P})");
        }
        
        /// <summary>Notify step completion</summary>
        protected void NotifyStepComplete(string step, object data = null)
        {
            var notification = new StepCompleteNotification(step, data);
            Mediator.Publish(notification);
        }
        
        public virtual void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
    
    public struct OrchestratorResult
    {
        public bool IsSuccess { get; init; }
        public string Message { get; init; }
        public object Data { get; init; }
        public TimeSpan Duration { get; init; }
        
        public static OrchestratorResult Success(object data = null, TimeSpan duration = default) =>
            new() { IsSuccess = true, Data = data, Duration = duration };
            
        public static OrchestratorResult Failure(string message, TimeSpan duration = default) =>
            new() { IsSuccess = false, Message = message, Duration = duration };
    }
}
```

### 4. Game Context Architecture

#### Context Design
```csharp
namespace CardWar.Core.Context
{
    /// <summary>
    /// Immutable game context carrying state through command pipeline
    /// </summary>
    public class GameContext : ICloneable
    {
        // Core state
        public GameState CurrentState { get; init; }
        public GameState PreviousState { get; init; }
        public DateTime StateChangedAt { get; init; }
        
        // Game data
        public Player[] Players { get; init; }
        public GameBoard Board { get; init; }
        public RoundData CurrentRound { get; init; }
        public WarData CurrentWar { get; init; }
        
        // System data
        public GameSettings Settings { get; init; }
        public IReadOnlyDictionary<string, object> Properties { get; init; }
        
        // Validation
        public bool IsValid =>
            CurrentState != GameState.Invalid &&
            Players?.Length > 0 &&
            Board != null &&
            Settings != null;
        
        /// <summary>Create new context with updated state</summary>
        public GameContext WithState(GameState newState) =>
            this with 
            { 
                PreviousState = CurrentState,
                CurrentState = newState,
                StateChangedAt = DateTime.UtcNow
            };
        
        /// <summary>Create new context with updated property</summary>
        public GameContext WithProperty(string key, object value)
        {
            var newProps = new Dictionary<string, object>(Properties) { [key] = value };
            return this with { Properties = newProps };
        }
        
        /// <summary>Create new context with updated round</summary>
        public GameContext WithRound(RoundData round) =>
            this with { CurrentRound = round };
        
        /// <summary>Create new context with updated war</summary>
        public GameContext WithWar(WarData war) =>
            this with { CurrentWar = war };
        
        public object Clone() => this with { };
        
        // Factory methods
        public static GameContext Create(GameSettings settings, Player[] players, GameBoard board) =>
            new()
            {
                CurrentState = GameState.Initializing,
                StateChangedAt = DateTime.UtcNow,
                Players = players ?? throw new ArgumentNullException(nameof(players)),
                Board = board ?? throw new ArgumentNullException(nameof(board)),
                Settings = settings ?? throw new ArgumentNullException(nameof(settings)),
                Properties = new Dictionary<string, object>()
            };
    }
    
    public class RoundData
    {
        public int RoundNumber { get; init; }
        public Card[] PlayerCards { get; init; }
        public Card[] OpponentCards { get; init; }
        public Player Winner { get; init; }
        public bool IsComplete { get; init; }
        public bool IsWar { get; init; }
        public DateTime StartedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
    }
    
    public class WarData
    {
        public int WarNumber { get; init; }
        public Card[] PlayerWarCards { get; init; }
        public Card[] OpponentWarCards { get; init; }
        public bool IsResolved { get; init; }
        public Player Winner { get; init; }
    }
}
```

### 5. Mediator Architecture

#### Mediator Interface
```csharp
namespace CardWar.Core.Mediator
{
    public interface IGameMediator : IDisposable
    {
        /// <summary>Send request and await response</summary>
        UniTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request);
        
        /// <summary>Publish notification (fire-and-forget)</summary>
        void Publish<T>(T notification) where T : INotification;
        
        /// <summary>Subscribe to notifications</summary>
        IDisposable Subscribe<T>(Func<T, UniTask> handler) where T : INotification;
        
        /// <summary>Register request handler</summary>
        void RegisterHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
            where TRequest : IRequest<TResponse>;
    }
    
    public interface IRequest<TResponse>
    {
        Guid RequestId { get; }
        DateTime Timestamp { get; }
    }
    
    public interface INotification
    {
        string EventType { get; }
        DateTime Timestamp { get; }
        object Payload { get; }
    }
    
    public interface IRequestHandler<TRequest, TResponse> 
        where TRequest : IRequest<TResponse>
    {
        UniTask<TResponse> HandleAsync(TRequest request);
    }
}
```

## Unity-Specific Considerations

### Performance Optimization
1. **Command Pooling**: Reuse command instances for frequent operations
2. **Context Pooling**: Pool context objects to reduce GC pressure
3. **Async Scheduling**: Use UniTask.Yield() to spread work across frames
4. **Memory Management**: Implement IDisposable consistently

### Integration Patterns
1. **MonoBehaviour Lifecycle**: Initialize during Start(), cleanup in OnDestroy()
2. **ScriptableObject Configuration**: Use for game settings and data
3. **Asset Management**: Wrap asset loading in commands
4. **Animation Integration**: Create animation commands for UI feedback

### Error Handling Strategy
1. **Graceful Degradation**: Fall back to simpler operations on failure
2. **User Feedback**: Show meaningful error messages through UI
3. **Recovery Mechanisms**: Automatic retry for transient failures
4. **Debug Information**: Comprehensive logging for development builds

### Testing Architecture
1. **Unit Tests**: Test individual commands in isolation
2. **Integration Tests**: Test orchestrator workflows
3. **Performance Tests**: Validate frame rate impact
4. **UI Tests**: Automated UI interaction testing

## Migration Strategy

### Phase 1: Foundation
- Implement core interfaces and base classes
- Create basic pipeline with minimal middleware
- Set up dependency injection container
- Add comprehensive logging

### Phase 2: Command Implementation
- Convert existing game operations to commands
- Implement validation and error handling
- Add performance monitoring
- Create unit tests for each command

### Phase 3: Orchestrator Development
- Build orchestrators for complex workflows
- Integrate with existing managers
- Add progress reporting and cancellation
- Implement integration tests

### Phase 4: UI Integration
- Connect UI to mediator notifications
- Replace direct event handling
- Add loading indicators and feedback
- Validate complete user workflows

This specification provides the technical foundation for implementing a professional Unity Command-Pipeline-Mediator architecture.