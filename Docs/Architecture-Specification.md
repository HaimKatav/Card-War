# Architecture Migration Plan

## Executive Summary
Migration from Event-Driven Architecture to Command-Pipeline-Mediator Pattern with Command-Driven State Management.

### Key Changes
- **Event System** → **Command Pipeline** (actions as commands)
- **State Machine with Behavior** → **State-as-Data with Commands** (state transitions as commands)
- **Direct Service Calls** → **Mediated Communication** (decoupled components)
- **Scattered Logic** → **Centralized Command Validation** (business rules in one place)

## Current Architecture Analysis

### Existing Components (To Preserve)
- ServiceLocator pattern (working, don't modify)
- Unity MonoBehaviour initialization pattern
- Asset management system
- UI binding system
- Game flow structure

### Problem Areas (To Replace)
- Event-driven state machine with behavioral states
- Tight coupling between game components
- Scattered state transition logic
- Difficult to test state transitions
- No clear audit trail for game actions

## New Architecture Design

### Core Components

#### 1. Command System
```csharp
// All game actions are commands
IGameCommand
├── BaseGameCommand (abstract base)
├── StateTransitionCommand (state changes)
├── GameActionCommand (gameplay actions)
└── SystemCommand (pause, resume, etc.)
```

#### 2. State Management
```csharp
// State-as-Data approach
GameState (enum) - Pure data, no behavior
GameContext - Immutable game state snapshot
ICommandStateManager - Validates transitions
StateTransitionCommand - Executes transitions
```

#### 3. Pipeline & Mediation
```csharp
ICommandPipeline - Processes commands
IGameMediator - Broadcasts events
CommandResult - Execution results
```

## Migration Phases

### Phase 1: Core Infrastructure ✅ COMPLETED
**Status**: Implementation complete, testing blocked by dependencies

#### 1.1 Command Foundation ✅
- [x] IGameCommand interface
- [x] BaseGameCommand abstract class
- [x] CommandResult structure
- [x] GameContext immutable class

#### 1.2 Pipeline Implementation ✅
- [x] ICommandPipeline interface
- [x] CommandPipeline implementation
- [x] Pipeline middleware support

#### 1.3 Mediator Pattern ✅
- [x] IGameMediator interface
- [x] GameMediator implementation
- [x] Event subscription model

#### 1.4 Basic Commands ✅
- [x] PauseGameCommand
- [x] ResumeGameCommand

### Phase 2: State Management System (CURRENT)
**Target**: Replace behavioral state machine with command-driven states

#### 2.1 State Infrastructure
- [ ] ICommandStateManager interface
- [ ] CommandStateManager implementation
- [ ] StateTransition structure
- [ ] Transition validation rules

#### 2.2 State Transition Commands
- [ ] StateTransitionCommand base class
- [ ] TransitionToMainMenuCommand
- [ ] TransitionToInitializingCommand
- [ ] TransitionToPlayingCommand
- [ ] TransitionToGameOverCommand

#### 2.3 Bridge Service
- [ ] StateManagementBridge (IGameStateService)
- [ ] Event compatibility layer
- [ ] Legacy state machine wrapper
- [ ] Gradual migration support

#### 2.4 Testing & Validation
- [ ] State transition tests
- [ ] Bridge compatibility tests
- [ ] Performance benchmarks

### Phase 3: Game Command Migration
**Target**: Convert all game actions to commands

#### 3.1 Card Commands
- [ ] DealCardsCommand
- [ ] DrawCardCommand
- [ ] PlayCardCommand
- [ ] CompareCardsCommand

#### 3.2 War Commands
- [ ] InitiateWarCommand
- [ ] ResolveWarCommand
- [ ] CollectWarCardsCommand

#### 3.3 Game Flow Commands
- [ ] StartGameCommand
- [ ] EndGameCommand
- [ ] CheckVictoryCommand
- [ ] ResetGameCommand

### Phase 4: UI Integration
**Target**: Connect UI to command pipeline

#### 4.1 UI Command Dispatchers
- [ ] ButtonCommandDispatcher
- [ ] InputCommandDispatcher
- [ ] AnimationCommandTrigger

#### 4.2 UI State Observers
- [ ] StateChangeListener
- [ ] CommandResultHandler
- [ ] UIUpdateMediator

### Phase 5: Service Layer Refactoring
**Target**: Align services with command pattern

#### 5.1 Service Interfaces
- [ ] Align with command expectations
- [ ] Remove direct state manipulation
- [ ] Add command-friendly methods

#### 5.2 Service Registration
- [ ] Update ServiceLocator usage
- [ ] Command-aware service lifecycle
- [ ] Lazy initialization support

### Phase 6: Cleanup & Optimization
**Target**: Remove old architecture, optimize new system

#### 6.1 Legacy Removal
- [ ] Remove old state machine
- [ ] Remove event system
- [ ] Remove bridge service
- [ ] Clean up unused interfaces

#### 6.2 Performance Optimization
- [ ] Command pooling
- [ ] Context caching
- [ ] Pipeline optimization
- [ ] Memory profiling

## Implementation Strategy

### Gradual Migration Approach
1. **Parallel Systems**: New commands work alongside old events
2. **Bridge Pattern**: Old code continues working during migration
3. **Incremental Conversion**: One subsystem at a time
4. **Testing at Each Step**: Ensure nothing breaks
5. **Feature Flag Control**: Toggle between old/new systems

### Code Example: Migration Pattern
```csharp
// Old System (preserved during migration)
public class GameManager : MonoBehaviour
{
    private IGameStateService _stateService; // Works through bridge
    
    void Start()
    {
        _stateService = ServiceLocator.Get<IGameStateService>();
        _stateService.OnStateChanged += HandleStateChanged;
    }
}

// Bridge Implementation
public class StateManagementBridge : IGameStateService
{
    private readonly ICommandPipeline _pipeline;
    private readonly IGameMediator _mediator;
    
    public event Action<GameState, GameState> OnStateChanged;
    
    public void ChangeState(GameState newState)
    {
        // Convert old API call to new command
        var command = CreateTransitionCommand(newState);
        _pipeline.ExecuteAsync(command).ContinueWith(result =>
        {
            if (result.IsSuccess)
            {
                // Trigger old event for compatibility
                OnStateChanged?.Invoke(_lastState, newState);
            }
        });
    }
}

// New System (gradually taking over)
public class NewGameFlow
{
    private readonly ICommandPipeline _pipeline;
    
    public async UniTask StartGame()
    {
        var command = new TransitionToPlayingCommand();
        var result = await _pipeline.ExecuteAsync(command);
        // Direct command usage, no events
    }
}
```

## State Transition Mapping

### Old State Machine States
```csharp
// BEFORE: Single state system with behavior
public class PlayingState : BaseState
{
    public override void Enter() { /* setup logic */ }
    public override void Update() { /* game loop */ }
    public override void Exit() { /* cleanup */ }
}

// Old states to REMOVE:
- BaseState (abstract class with behavior)
- UninitializedState
- MainMenuState
- InitializingState
- PlayingState
- PausedState
- GameOverState
```

### New Dual State System
```csharp
// AFTER: Dual state enums, no behavior
public enum AppState
{
    Initializing,
    MainMenu,
    LoadingGame,
    InGame,
    GameOver
}

public enum GameState
{
    WaitingToStart,
    PlayerTurn,
    OpponentTurn,
    ResolvingBattle,
    War,
    CollectingCards,
    CheckingVictory,
    Paused,
    Error
}

// App state transitions as commands
public class TransitionToInGameCommand : AppStateTransitionCommand
{
    protected override AppState TargetState => AppState.InGame;
    
    protected override async UniTask<ValidationResult> ValidateTransition(
        GameContext context, 
        IAppStateManager stateManager)
    {
        // Validation that was in CanTransition
        return ValidationResult.Valid();
    }
    
    protected override async UniTask<GameContext> ApplyStateChange(GameContext context)
    {
        // Logic that was in Enter()
        return context
            .WithAppState(AppState.InGame)
            .WithGameState(GameState.WaitingToStart);
    }
}

// Game state transitions as commands
public class TransitionToWarCommand : GameStateTransitionCommand
{
    protected override GameState TargetState => GameState.War;
    
    protected override async UniTask<ValidationResult> ValidateTransition(
        GameContext context,
        IGameStateManager stateManager)
    {
        if (context.AppState != AppState.InGame)
            return ValidationResult.Invalid("Must be in game");
            
        if (context.GameState != GameState.ResolvingBattle)
            return ValidationResult.Invalid("War only after battle resolution");
            
        if (!context.IsTie)
            return ValidationResult.Invalid("War only on tie");
            
        return ValidationResult.Valid();
    }
}
```

### Migration Mapping
| Old State | New AppState | New GameState | Notes |
|-----------|--------------|---------------|-------|
| UninitializedState | Initializing | N/A | App loading |
| MainMenuState | MainMenu | N/A | Menu screen |
| InitializingState | LoadingGame | N/A | Game assets loading |
| PlayingState | InGame | PlayerTurn/OpponentTurn | Active gameplay |
| PausedState | InGame | Paused | Game paused |
| GameOverState | GameOver | N/A | Results screen |

## Risk Mitigation

### Identified Risks
1. **ServiceLocator Conflicts**: New implementation conflicts with existing
   - **Mitigation**: Use existing ServiceLocator, don't replace
   
2. **Breaking Existing Functionality**: Old code stops working
   - **Mitigation**: Bridge pattern maintains compatibility
   
3. **Performance Degradation**: Commands slower than direct calls
   - **Mitigation**: Command pooling, optimization phase
   
4. **Complex Migration**: Too many moving parts
   - **Mitigation**: Incremental phases, testing at each step

### Rollback Strategy
- Feature flags for each phase
- Parallel systems during migration
- Complete test coverage before switching
- Ability to revert to old system quickly

## Success Metrics

### Technical Metrics
- **Test Coverage**: >80% for all commands
- **Performance**: <5ms command execution time
- **Memory**: <10% increase in memory usage
- **Bugs**: <5 critical bugs during migration

### Architecture Metrics
- **Decoupling**: No direct dependencies between game systems
- **Testability**: All commands unit testable
- **Maintainability**: Single responsibility for each command
- **Extensibility**: New features as new commands

## Timeline Estimates

### Phase Durations
- **Phase 1**: ✅ Completed
- **Phase 2**: 2-3 days (State Management)
- **Phase 3**: 3-4 days (Game Commands)
- **Phase 4**: 2 days (UI Integration)
- **Phase 5**: 2 days (Service Refactoring)
- **Phase 6**: 1-2 days (Cleanup)

**Total Estimate**: 10-14 days

## Current Status & Next Steps

### Completed ✅
- Phase 1.1: Core command infrastructure
- Basic command implementations
- Pipeline and mediator setup

### In Progress 🔄
- Phase 2.1: State management infrastructure
- Documentation updates
- Service dependency resolution

### Blocked ⚠️
- Testing of Phase 1 (service dependencies)
- Full integration (needs state management)

### Immediate Actions
1. Fix ServiceLocator usage in commands
2. Implement ICommandStateManager
3. Create StateManagementBridge
4. Test Phase 1 components
5. Begin Phase 2.2 implementation

## Command Catalog

### System Commands
```csharp
PauseGameCommand - Pauses game
ResumeGameCommand - Resumes from pause
SaveGameCommand - Saves game state
LoadGameCommand - Loads saved state
```

### State Commands
```csharp
TransitionToMainMenuCommand - Go to main menu
TransitionToInitializingCommand - Start initialization
TransitionToPlayingCommand - Begin gameplay
TransitionToGameOverCommand - End game
```

### Game Commands
```csharp
DealCardsCommand - Initial deal
DrawCardCommand - Draw from deck
PlayCardCommand - Play to table
CompareCardsCommand - Compare values
InitiateWarCommand - Start war sequence
ResolveWarCommand - Resolve war
CollectCardsCommand - Collect won cards
CheckVictoryCommand - Check win condition
```

## Notes for Developers

### When Creating New Commands
1. Extend appropriate base class
2. Implement validation logic
3. Keep execution logic simple
4. Use ServiceLocator for dependencies
5. Return meaningful CommandResult
6. Log important steps
7. Write unit tests

### When Migrating Old Code
1. Identify the action/behavior
2. Create corresponding command
3. Move validation to ValidateAsync
4. Move execution to ExecuteAsyncCore
5. Use bridge for compatibility
6. Test both old and new paths
7. Remove old code when stable

### Best Practices
- Commands are immutable
- Context is immutable (use WithXXX methods)
- Validation before execution
- Meaningful error messages
- Comprehensive logging
- Pool frequently used commands
- Test in isolation