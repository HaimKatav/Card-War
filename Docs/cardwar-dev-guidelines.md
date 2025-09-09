# CardWar Development Guidelines

## Core Principles

### 1. Dependency Injection First
- **ServiceLocator for Commands** - Commands retrieve services at runtime via ServiceLocator.Get<T>()
- **Initialize method for MonoBehaviours** - Pass dependencies through Initialize(params) method
- **No circular dependencies** - If A needs B, B should never need A directly
- **No constructor injection in commands** - Use ServiceLocator pattern consistently

### 2. Code Organization

#### File Structure
- **One class per file** - No exceptions
- **Code flow should be understood without comments** - No exceptions
- **Create tools not just code - Reused code is always made into a method** - No exceptions
- **Separate artifacts** - Each class must be delivered in its own artifact
- **Logical namespaces** - Follow the folder structure (CardWar.Core, CardWar.Commands, etc.)

#### Class Structure with Regions
```csharp
public class ExampleClass : MonoBehaviour
{
    // Fields (no region)
    private IDIService _diService;
    private bool _isInitialized;
    
    // Properties (no region)
    public bool IsReady => _isInitialized;
    
    #region Initialization
    public void Initialize(IDIService diService) { }
    #endregion
    
    #region Unity Lifecycle
    private void Awake() { }
    private void Start() { }
    private void Update() { }
    private void OnDestroy() { }
    #endregion
    
    #region Public Methods
    public void DoSomething() { }
    #endregion
    
    #region Private Methods
    private void ProcessInternal() { }
    #endregion
    
    #region Event Handlers
    private void HandleStateChanged() { }
    #endregion
    
    #region Cleanup
    public void Dispose() { }
    #endregion
}
```

### 3. State Management Architecture

#### Command-Driven State Management (State-as-Data)
We use a Command-driven approach instead of traditional state machines with behavior. This decision provides:
- **Better testability** - Each state transition is a discrete command
- **Clear audit trail** - Every state change is tracked through commands
- **Separation of concerns** - State logic separated from state data
- **Easier migration** - Old code can continue working during transition

#### Core Principles
- **States are just enums + context** - No behavior in state classes
- **ALL state transitions go through commands** - No direct state manipulation
- **Commands validate state transitions** - Business rules in command validation
- **Mediator broadcasts state changes** - Decoupled notification system
- **Transition rules are centralized** - Single source of truth for valid transitions
- **Immutable context** - GameContext uses builder pattern with WithXXX() methods

#### Implementation Pattern
```csharp
// State enum - pure data
public enum GameState
{
    Uninitialized,
    MainMenu,
    Initializing,
    Playing,
    Paused,
    GameOver
}

// State transition command
public class TransitionToPlayingCommand : StateTransitionCommand
{
    protected override GameState TargetState => GameState.Playing;
    protected override string TransitionReason => "Starting game";
    
    protected override async UniTask<ValidationResult> ValidateTransition(
        GameContext context, 
        ICommandStateManager stateManager)
    {
        var currentState = stateManager.GetCurrentState();
        
        if (currentState != GameState.MainMenu && currentState != GameState.Initializing)
        {
            return ValidationResult.Invalid($"Cannot transition to Playing from {currentState}");
        }
        
        if (context.Players?.Count != 2)
        {
            return ValidationResult.Invalid("Need exactly 2 players to start");
        }
        
        return ValidationResult.Valid();
    }
    
    protected override async UniTask<GameContext> ApplyStateChange(GameContext context)
    {
        return context
            .WithState(GameState.Playing)
            .WithTimestamp(Time.time)
            .WithMetadata("game_started", true);
    }
}
```

#### State Transition Rules
```csharp
// Centralized transition rules in ICommandStateManager
public class CommandStateManager : ICommandStateManager
{
    private readonly Dictionary<(GameState from, GameState to), Func<GameContext, bool>> _transitionRules;
    
    public bool CanTransition(GameState from, GameState to, GameContext context)
    {
        var key = (from, to);
        if (!_transitionRules.ContainsKey(key))
            return false;
            
        return _transitionRules[key](context);
    }
}
```

#### Bridge Pattern for Migration
```csharp
// Bridge service allows old event-driven code to work
public class StateManagementBridge : IGameStateService
{
    private readonly IGameMediator _mediator;
    private readonly ICommandPipeline _pipeline;
    
    // Old interface method
    public void ChangeState(GameState newState)
    {
        // Convert to command
        var command = CreateTransitionCommand(newState);
        _pipeline.ExecuteAsync(command).Forget();
    }
    
    // Event for backward compatibility
    public event Action<GameState, GameState> OnStateChanged;
}
```

### 4. Command Pipeline Architecture

#### Command Execution Flow
1. **Command Creation** - Commands are lightweight, stateless objects
2. **Pipeline Processing** - Middleware can intercept/modify execution
3. **Validation** - Commands validate preconditions
4. **Execution** - Core logic runs in ExecuteAsyncCore
5. **Result Handling** - CommandResult carries success/failure + data
6. **Mediation** - Mediator broadcasts events to subscribers

#### Command Implementation Pattern
```csharp
public abstract class BaseGameCommand : IGameCommand
{
    protected IGameMediator Mediator => ServiceLocator.Get<IGameMediator>();
    protected ICommandStateManager StateManager => ServiceLocator.Get<ICommandStateManager>();
    
    public async UniTask<CommandResult> ExecuteAsync(GameContext context)
    {
        try
        {
            var validation = await ValidateAsync(context);
            if (!validation.IsValid)
                return CommandResult.Failure(validation.ErrorMessage);
                
            var result = await ExecuteAsyncCore(context);
            
            if (result.IsSuccess)
                await OnSuccessAsync(context, result);
                
            return result;
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Command execution failed: {ex.Message}");
        }
    }
    
    protected abstract UniTask<CommandResult> ExecuteAsyncCore(GameContext context);
}
```

### 5. Naming Conventions
- **Commands**: `VerbNounCommand` (e.g., `DrawCardsCommand`, `ValidateInputCommand`)
- **Private fields**: `_fieldName` (underscore prefix)
- **Properties**: `PropertyName` (PascalCase)
- **Methods**: `VerbNoun()` format
- **Events**: `OnEventName`
- **Interfaces**: `IServiceName`
- **Async methods**: `MethodNameAsync()`

### 6. Logging Standards
```csharp
// Use consistent format
Debug.Log($"[{GetType().Name}] Action performed");
Debug.LogWarning($"[{GetType().Name}] Warning: issue description");
Debug.LogError($"[{GetType().Name}] Error: error description");

// Command logging
Debug.Log($"[{GetType().Name}] Executing command with context: {context.State}");
Debug.Log($"[{GetType().Name}] Command result: {result.IsSuccess} - {result.Message}");
```

### 7. Service Registration Order
1. **Core Services First** - ServiceLocator (self), ICommandStateManager
2. **Command Infrastructure** - ICommandPipeline, IGameMediator
3. **Manager Services** - IAssetService, IAudioService, IUIService
4. **Game Services** - IGameControllerService
5. **Bridge Services** - StateManagementBridge (for backward compatibility)

### 8. Configuration Management
- **All paths in GameSettings** - No hardcoded paths in code
- **ScriptableObjects for data** - Use for all configuration
- **State transition rules in config** - Externalize transition validation
- **Resource paths structure**:
  ```
  Resources/
  ├── Settings/
  │   ├── GameSettings.asset
  │   ├── StateTransitionRules.asset
  │   └── NetworkSettings.asset
  └── GameplaySprites/
      └── Cards/
  ```

### 9. Testing Approach

#### Command Testing
```csharp
// Test commands in isolation
[Test]
public async Task DrawCardsCommand_ValidatesPlayerHasCards()
{
    var context = new GameContext()
        .WithPlayer(new Player { CardCount = 0 });
    var command = new DrawCardsCommand();
    
    var result = await command.ExecuteAsync(context);
    
    Assert.IsFalse(result.IsSuccess);
    Assert.Contains("No cards", result.Message);
}
```

#### State Transition Testing
```csharp
// Test state transitions
[Test]
public async Task StateTransition_FromMenuToPlaying_RequiresTwoPlayers()
{
    var context = new GameContext()
        .WithState(GameState.MainMenu)
        .WithPlayers(new List<Player> { new Player() }); // Only one player
    
    var command = new TransitionToPlayingCommand();
    var result = await command.ExecuteAsync(context);
    
    Assert.IsFalse(result.IsSuccess);
    Assert.Contains("2 players", result.Message);
}
```

### 10. Migration Strategy

#### Phase 1: Infrastructure (Current)
- Implement core command pipeline ✅
- Create GameMediator ✅
- Setup CommandStateManager
- Build bridge service for old code

#### Phase 2: State Commands
- Convert each state transition to command
- Maintain old event system through bridge
- Test both paths work

#### Phase 3: Game Commands
- Convert game actions to commands
- Migrate UI to use command pipeline
- Remove old action handlers

#### Phase 4: Cleanup
- Remove old state machine
- Remove bridge service
- Clean up event handlers

### 11. Common Patterns

#### State Transition Command
```csharp
public abstract class StateTransitionCommand : BaseGameCommand
{
    protected abstract GameState TargetState { get; }
    protected abstract string TransitionReason { get; }
    
    protected override async UniTask<CommandResult> ExecuteAsyncCore(GameContext context)
    {
        var stateManager = ServiceLocator.Get<ICommandStateManager>();
        
        if (!stateManager.CanTransition(context.State, TargetState, context))
        {
            return CommandResult.Failure($"Invalid transition from {context.State} to {TargetState}");
        }
        
        var newContext = await ApplyStateChange(context);
        
        await Mediator.PublishAsync(new StateChangedEvent
        {
            OldState = context.State,
            NewState = TargetState,
            Reason = TransitionReason,
            Context = newContext
        });
        
        return CommandResult.Success(newContext);
    }
    
    protected abstract UniTask<GameContext> ApplyStateChange(GameContext context);
}
```

#### Game Action Command
```csharp
public abstract class GameActionCommand : BaseGameCommand
{
    protected override async UniTask<ValidationResult> ValidateAsync(GameContext context)
    {
        if (context.State != GameState.Playing)
        {
            return ValidationResult.Invalid("Action only valid during gameplay");
        }
        
        return await ValidateGameRules(context);
    }
    
    protected abstract UniTask<ValidationResult> ValidateGameRules(GameContext context);
}
```

### 12. Performance Considerations

#### Command Pooling
```csharp
// Pool frequently used commands
public class CommandPool<T> where T : IGameCommand, new()
{
    private readonly Stack<T> _pool = new Stack<T>();
    
    public T Rent()
    {
        return _pool.Count > 0 ? _pool.Pop() : new T();
    }
    
    public void Return(T command)
    {
        if (command is IPoolableCommand poolable)
            poolable.Reset();
        _pool.Push(command);
    }
}
```

#### Context Optimization
- Use struct for small data in context
- Lazy load heavy objects
- Cache frequently accessed services
- Minimize context mutations

## War Card Game Specific Rules

### Game Commands
- `DealCardsCommand` - Initial card distribution
- `PlayCardCommand` - Play a card from hand
- `CompareCardsCommand` - Determine round winner
- `InitiateWarCommand` - Handle tie scenario
- `CollectCardsCommand` - Winner takes cards
- `CheckVictoryCommand` - Check win conditions

### State Transitions

#### App State Flow
```
Initializing -> MainMenu (assets loaded)
MainMenu -> LoadingGame (start pressed)
LoadingGame -> InGame (game ready)
InGame -> GameOver (game ends)
GameOver -> MainMenu (restart)
```

#### Game State Flow (within InGame)
```
WaitingToStart -> PlayerTurn (game begins)
PlayerTurn -> ResolvingBattle (card played)
ResolvingBattle -> War (tie detected)
ResolvingBattle -> CollectingCards (winner determined)
War -> ResolvingBattle (war cards played)
CollectingCards -> CheckingVictory (cards collected)
CheckingVictory -> PlayerTurn (no winner yet)
CheckingVictory -> (triggers AppState.GameOver)
Any GameState -> Paused (pause command)
Paused -> Previous GameState (resume command)
Any GameState -> Error (error occurred)
```

### Migration Notes
- **Remove all old state machine code** - Delete BaseState, PlayingState, etc.
- **Remove behavioral state classes** - States are now pure enums
- **Update UI bindings** - Subscribe to new state change events
- **Test with bridge** - Ensure old code works during migration

## Code Quality Standards

### Must Have
- **Self-documenting code** - No comments needed
- **Command validation** - All preconditions checked
- **Immutable data flow** - Context never mutated directly
- **Consistent patterns** - Same approach throughout
- **Error handling** - Try-catch only in ExecuteAsync wrapper

### Must Avoid
- **Direct state manipulation** - Always use commands
- **Mutable context** - Use builder pattern
- **Deep command nesting** - Flatten command chains
- **Circular command dependencies** - One-way flow
- **Synchronous blocking** - Use UniTask consistently