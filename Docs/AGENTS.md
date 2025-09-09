# AGENTS.md - AI Code Generation Guidelines

## Purpose
This document provides patterns and templates for AI assistants (Codex, GitHub Copilot, ChatGPT) to generate consistent, high-quality code for the CardWar project following our Command-Pipeline-Mediator architecture.

## Core Architecture Context

### Architecture Pattern
We use **Command-Pipeline-Mediator** pattern with **State-as-Data** management:
- All actions are commands
- All state transitions are commands
- Commands are processed through a pipeline
- Mediator broadcasts events
- States have no behavior, only data

### Key Principles for AI Generation
1. **No comments in code** - Code must be self-documenting
2. **One class per file** - Always generate single class
3. **Use ServiceLocator for dependencies** - Never use constructor injection in commands
4. **Immutable GameContext** - Use WithXXX() builder methods
5. **Commands validate themselves** - Business rules in ValidateAsync

## Command Generation Templates

### Basic Game Command Template
```csharp
using System;
using Cysharp.Threading.Tasks;
using CardWar.Core;
using CardWar.Commands;

namespace CardWar.Commands.Gameplay
{
    public class [ActionName]Command : GameActionCommand
    {
        // Input parameters as properties
        public [Type] [Parameter] { get; set; }
        
        protected override async UniTask<ValidationResult> ValidateGameRules(GameContext context)
        {
            // Validate game-specific rules
            if ([condition that makes command invalid])
            {
                return ValidationResult.Invalid("[Clear error message]");
            }
            
            return ValidationResult.Valid();
        }
        
        protected override async UniTask<CommandResult> ExecuteAsyncCore(GameContext context)
        {
            try
            {
                // Get required services
                var [serviceName] = ServiceLocator.Get<I[ServiceName]>();
                
                // Perform the action
                [Action logic here]
                
                // Update context
                var newContext = context
                    .With[Property]([newValue])
                    .WithTimestamp(Time.time);
                
                // Publish event if needed
                await Mediator.PublishAsync(new [EventName]Event
                {
                    [EventData]
                });
                
                Debug.Log($"[{GetType().Name}] [Action] completed successfully");
                return CommandResult.Success(newContext);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to [action]: {ex.Message}");
                return CommandResult.Failure($"Failed to [action]: {ex.Message}");
            }
        }
    }
}
```

### State Transition Command Template
```csharp
using System;
using Cysharp.Threading.Tasks;
using CardWar.Core;
using CardWar.Commands;

namespace CardWar.Commands.State
{
    public class TransitionTo[StateName]Command : StateTransitionCommand
    {
        protected override GameState TargetState => GameState.[StateName];
        protected override string TransitionReason => "[Reason for transition]";
        
        protected override async UniTask<ValidationResult> ValidateTransition(
            GameContext context, 
            ICommandStateManager stateManager)
        {
            var currentState = stateManager.GetCurrentState();
            var validFromStates = new[] { GameState.[ValidState1], GameState.[ValidState2] };
            
            if (!validFromStates.Contains(currentState))
            {
                return ValidationResult.Invalid(
                    $"Cannot transition to {TargetState} from {currentState}");
            }
            
            // Additional validation
            if ([specific condition check])
            {
                return ValidationResult.Invalid("[Specific error message]");
            }
            
            return ValidationResult.Valid();
        }
        
        protected override async UniTask<GameContext> ApplyStateChange(GameContext context)
        {
            Debug.Log($"[{GetType().Name}] Transitioning to {TargetState}");
            
            // Perform any setup for the new state
            var newContext = context
                .WithState(TargetState)
                .WithTimestamp(Time.time);
            
            // Add any state-specific context updates
            [Additional context updates]
            
            return newContext;
        }
        
        protected override async UniTask OnTransitionComplete(GameContext oldContext, GameContext newContext)
        {
            Debug.Log($"[{GetType().Name}] State transition complete: {oldContext.State} -> {newContext.State}");
            
            // Any post-transition cleanup or initialization
            await base.OnTransitionComplete(oldContext, newContext);
        }
    }
}
```

### Complex Command with War Logic Template
```csharp
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using CardWar.Core;
using CardWar.Commands;

namespace CardWar.Commands.Gameplay
{
    public class [ComplexAction]Command : GameActionCommand
    {
        public [InputType] [InputParameter] { get; set; }
        
        private I[Service1] _service1;
        private I[Service2] _service2;
        
        protected override async UniTask<ValidationResult> ValidateGameRules(GameContext context)
        {
            // Multiple validation checks
            if (context.State != GameState.Playing)
            {
                return ValidationResult.Invalid("Game must be in Playing state");
            }
            
            if ([complex condition])
            {
                return ValidationResult.Invalid("[Detailed error message]");
            }
            
            // Validate nested conditions
            var [checkResult] = await Validate[SubCondition](context);
            if (!checkResult.IsValid)
            {
                return checkResult;
            }
            
            return ValidationResult.Valid();
        }
        
        protected override async UniTask<CommandResult> ExecuteAsyncCore(GameContext context)
        {
            InitializeServices();
            
            try
            {
                // Step 1: [First action]
                var [result1] = await [PerformStep1](context);
                if (![result1 success check])
                {
                    return CommandResult.Failure("[Step 1 failure message]");
                }
                
                // Step 2: [Second action]
                var [result2] = await [PerformStep2](context, [result1]);
                
                // Update context with results
                var newContext = context
                    .With[Property1]([result1])
                    .With[Property2]([result2])
                    .WithTimestamp(Time.time);
                
                // Publish complex event
                await PublishResults(context, newContext);
                
                Debug.Log($"[{GetType().Name}] [Complex action] completed");
                return CommandResult.Success(newContext);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] [Complex action] failed: {ex.Message}");
                return CommandResult.Failure($"[Complex action] failed: {ex.Message}");
            }
        }
        
        private void InitializeServices()
        {
            _service1 = ServiceLocator.Get<I[Service1]>();
            _service2 = ServiceLocator.Get<I[Service2]>();
        }
        
        private async UniTask<[ReturnType]> [PerformStep1](GameContext context)
        {
            // Step implementation
            [Step logic]
            return [result];
        }
        
        private async UniTask<[ReturnType]> [PerformStep2](GameContext context, [ParamType] param)
        {
            // Step implementation
            [Step logic]
            return [result];
        }
        
        private async UniTask PublishResults(GameContext oldContext, GameContext newContext)
        {
            await Mediator.PublishAsync(new [ComplexEvent]
            {
                OldContext = oldContext,
                NewContext = newContext,
                [AdditionalEventData]
            });
        }
        
        private async UniTask<ValidationResult> Validate[SubCondition](GameContext context)
        {
            // Sub-validation logic
            if ([sub condition])
            {
                return ValidationResult.Invalid("[Sub condition error]");
            }
            return ValidationResult.Valid();
        }
    }
}
```

## Service Generation Templates

### Command State Manager Template
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Core;

namespace CardWar.Services.State
{
    public class CommandStateManager : ICommandStateManager
    {
        private GameState _currentState = GameState.Uninitialized;
        private readonly Dictionary<(GameState from, GameState to), Func<GameContext, bool>> _transitionRules;
        private readonly IGameMediator _mediator;
        
        public CommandStateManager()
        {
            _mediator = ServiceLocator.Get<IGameMediator>();
            _transitionRules = InitializeTransitionRules();
        }
        
        public GameState GetCurrentState()
        {
            return _currentState;
        }
        
        public void SetCurrentState(GameState state)
        {
            var oldState = _currentState;
            _currentState = state;
            Debug.Log($"[{GetType().Name}] State changed: {oldState} -> {state}");
        }
        
        public bool CanTransition(GameState from, GameState to, GameContext context)
        {
            var key = (from, to);
            
            if (!_transitionRules.ContainsKey(key))
            {
                Debug.LogWarning($"[{GetType().Name}] No rule for transition: {from} -> {to}");
                return false;
            }
            
            var canTransition = _transitionRules[key](context);
            
            if (!canTransition)
            {
                Debug.Log($"[{GetType().Name}] Transition blocked by rules: {from} -> {to}");
            }
            
            return canTransition;
        }
        
        public string GetInvalidTransitionReason(GameState from, GameState to, GameContext context)
        {
            if (!_transitionRules.ContainsKey((from, to)))
            {
                return $"No transition defined from {from} to {to}";
            }
            
            // Detailed reason based on context
            return Get[Specific]ValidationError(from, to, context);
        }
        
        private Dictionary<(GameState, GameState), Func<GameContext, bool>> InitializeTransitionRules()
        {
            return new Dictionary<(GameState, GameState), Func<GameContext, bool>>
            {
                // Define all valid transitions
                { (GameState.Uninitialized, GameState.MainMenu), ctx => true },
                { (GameState.MainMenu, GameState.Initializing), ctx => ValidateGameStart(ctx) },
                { (GameState.Initializing, GameState.Playing), ctx => ValidateInitComplete(ctx) },
                { (GameState.Playing, GameState.Paused), ctx => true },
                { (GameState.Paused, GameState.Playing), ctx => true },
                { (GameState.Playing, GameState.GameOver), ctx => ValidateGameEnd(ctx) },
                { (GameState.GameOver, GameState.MainMenu), ctx => true }
            };
        }
        
        private bool ValidateGameStart(GameContext context)
        {
            return context.Players?.Count == 2;
        }
        
        private bool ValidateInitComplete(GameContext context)
        {
            return context.IsInitialized && 
                   context.Players?.All(p => p.IsReady) == true;
        }
        
        private bool ValidateGameEnd(GameContext context)
        {
            return context.GameResult != null || 
                   context.Players?.Any(p => p.CardCount == 0) == true;
        }
        
        private string Get[Specific]ValidationError(GameState from, GameState to, GameContext context)
        {
            // Return specific error messages based on validation failure
            [Specific error logic]
            return "Transition requirements not met";
        }
    }
}
```

## Bridge Service Template (Temporary - Remove After Migration)
```csharp
using System;
using Cysharp.Threading.Tasks;
using CardWar.Core;
using CardWar.Commands;

namespace CardWar.Services.Bridge
{
    // TEMPORARY: This bridge allows old code to work during migration
    // DELETE this entire class after migration is complete
    public class StateManagementBridge : IGameStateService  // Old interface
    {
        private readonly ICommandPipeline _pipeline;
        private readonly IGameMediator _mediator;
        private readonly IAppStateManager _appStateManager;
        private readonly IGameStateManager _gameStateManager;
        private GameState _lastGameState;
        
        // Old events for backward compatibility (will be removed)
        public event Action<GameState, GameState> OnStateChanged;
        public event Action<GameState> OnStateEnter;
        public event Action<GameState> OnStateExit;
        
        public StateManagementBridge()
        {
            _pipeline = ServiceLocator.Get<ICommandPipeline>();
            _mediator = ServiceLocator.Get<IGameMediator>();
            _appStateManager = ServiceLocator.Get<IAppStateManager>();
            _gameStateManager = ServiceLocator.Get<IGameStateManager>();
            
            SubscribeToMediatorEvents();
        }
        
        // Old interface property
        public GameState CurrentState => MapToOldState(
            _appStateManager.GetCurrentAppState(), 
            _gameStateManager.GetCurrentGameState());
        
        // Old interface method - converts to new command system
        public void ChangeState(GameState oldStateEnum)
        {
            Debug.Log($"[{GetType().Name}] Legacy state change request: {oldStateEnum}");
            
            var command = CreateCommandFromOldState(oldStateEnum);
            var context = BuildCurrentContext();
            
            _pipeline.ExecuteAsync(command, context).ContinueWith(result =>
            {
                if (result.IsCompletedSuccessfully && result.Result.IsSuccess)
                {
                    HandleStateChangeSuccess(_lastGameState, oldStateEnum);
                }
                else
                {
                    Debug.LogError($"[{GetType().Name}] State change failed: {result.Result.Message}");
                }
            }).Forget();
        }
        
        public bool CanChangeState(GameState oldStateEnum)
        {
            var (targetApp, targetGame) = MapFromOldState(oldStateEnum);
            var context = BuildCurrentContext();
            
            if (targetApp.HasValue)
            {
                return _appStateManager.CanTransition(
                    _appStateManager.GetCurrentAppState(), 
                    targetApp.Value, 
                    context);
            }
            
            if (targetGame.HasValue)
            {
                return _gameStateManager.CanTransition(
                    _gameStateManager.GetCurrentGameState(), 
                    targetGame.Value, 
                    context);
            }
            
            return false;
        }
        
        private IGameCommand CreateCommandFromOldState(GameState oldState)
        {
            return oldState switch
            {
                GameState.MainMenu => new TransitionToMainMenuCommand(),
                GameState.Initializing => new TransitionToLoadingGameCommand(),
                GameState.Playing => new TransitionToPlayerTurnCommand(),
                GameState.Paused => new TransitionToPausedCommand(),
                GameState.GameOver => new TransitionToGameOverCommand(),
                _ => throw new NotSupportedException($"Old state {oldState} not mapped")
            };
        }
        
        private (AppState? app, GameState? game) MapFromOldState(GameState oldState)
        {
            return oldState switch
            {
                GameState.Uninitialized => (AppState.Initializing, null),
                GameState.MainMenu => (AppState.MainMenu, null),
                GameState.Initializing => (AppState.LoadingGame, null),
                GameState.Playing => (null, GameState.PlayerTurn),
                GameState.Paused => (null, GameState.Paused),
                GameState.GameOver => (AppState.GameOver, null),
                _ => (null, null)
            };
        }
        
        private GameState MapToOldState(AppState appState, GameState gameState)
        {
            if (appState == AppState.InGame)
            {
                return gameState switch
                {
                    GameState.PlayerTurn => GameState.Playing,
                    GameState.OpponentTurn => GameState.Playing,
                    GameState.Paused => GameState.Paused,
                    _ => GameState.Playing
                };
            }
            
            return appState switch
            {
                AppState.Initializing => GameState.Uninitialized,
                AppState.MainMenu => GameState.MainMenu,
                AppState.LoadingGame => GameState.Initializing,
                AppState.GameOver => GameState.GameOver,
                _ => GameState.Uninitialized
            };
        }
        
        private void SubscribeToMediatorEvents()
        {
            _mediator.Subscribe<AppStateChangedEvent>(HandleAppStateChangedEvent);
            _mediator.Subscribe<GameStateChangedEvent>(HandleGameStateChangedEvent);
        }
        
        private void HandleAppStateChangedEvent(AppStateChangedEvent evt)
        {
            var oldMapped = MapToOldState(evt.OldState, _gameStateManager.GetCurrentGameState());
            var newMapped = MapToOldState(evt.NewState, _gameStateManager.GetCurrentGameState());
            
            if (oldMapped != newMapped)
            {
                OnStateExit?.Invoke(oldMapped);
                OnStateEnter?.Invoke(newMapped);
                OnStateChanged?.Invoke(oldMapped, newMapped);
                _lastGameState = oldMapped;
            }
        }
        
        private void HandleGameStateChangedEvent(GameStateChangedEvent evt)
        {
            var appState = _appStateManager.GetCurrentAppState();
            var oldMapped = MapToOldState(appState, evt.OldState);
            var newMapped = MapToOldState(appState, evt.NewState);
            
            if (oldMapped != newMapped)
            {
                OnStateExit?.Invoke(oldMapped);
                OnStateEnter?.Invoke(newMapped);
                OnStateChanged?.Invoke(oldMapped, newMapped);
                _lastGameState = oldMapped;
            }
        }
        
        private void HandleStateChangeSuccess(GameState oldState, GameState newState)
        {
            Debug.Log($"[{GetType().Name}] Legacy state change successful: {oldState} -> {newState}");
        }
        
        private GameContext BuildCurrentContext()
        {
            return new GameContext()
                .WithAppState(_appStateManager.GetCurrentAppState())
                .WithGameState(_gameStateManager.GetCurrentGameState())
                .WithTimestamp(Time.time);
        }
        
        public void Dispose()
        {
            _mediator.Unsubscribe<AppStateChangedEvent>(HandleAppStateChangedEvent);
            _mediator.Unsubscribe<GameStateChangedEvent>(HandleGameStateChangedEvent);
            OnStateChanged = null;
            OnStateEnter = null;
            OnStateExit = null;
        }
    }
}
```

## AI Generation Instructions

### When Asked to Create a Command
1. **Identify command type**: Game action, state transition, or system command
2. **Use appropriate template**: Select from templates above
3. **Follow naming convention**: VerbNounCommand format
4. **Implement validation**: All preconditions in ValidateAsync
5. **Use ServiceLocator**: Get dependencies at runtime
6. **Return CommandResult**: Success with data or Failure with message
7. **Add logging**: Log start, success, and failure
8. **Keep it simple**: One responsibility per command

### When Asked to Create a Service
1. **Define interface first**: I[ServiceName] with clear methods
2. **Use ServiceLocator**: Get other services in constructor or methods
3. **No state manipulation**: Services don't change state directly
4. **Support commands**: Methods should be command-friendly
5. **Add logging**: Log important operations
6. **Handle errors gracefully**: Return results, don't throw

### When Asked to Migrate Existing Code
1. **Identify the behavior**: What does the old code do?
2. **Choose command type**: Action or state transition?
3. **Extract validation**: Move checks to ValidateAsync
4. **Extract execution**: Move logic to ExecuteAsyncCore
5. **Preserve events**: Use bridge for compatibility
6. **Test both paths**: Ensure old and new work

## Common Patterns

### Pattern: Command Chaining
```csharp
public class ChainedCommand : BaseGameCommand
{
    protected override async UniTask<CommandResult> ExecuteAsyncCore(GameContext context)
    {
        var pipeline = ServiceLocator.Get<ICommandPipeline>();
        
        // Execute first command
        var result1 = await pipeline.ExecuteAsync(new FirstCommand(), context);
        if (!result1.IsSuccess)
            return result1;
            
        // Execute second command with updated context
        var result2 = await pipeline.ExecuteAsync(new SecondCommand(), result1.Data as GameContext);
        return result2;
    }
}
```

### Pattern: Conditional Command
```csharp
public class ConditionalCommand : BaseGameCommand
{
    protected override async UniTask<CommandResult> ExecuteAsyncCore(GameContext context)
    {
        var pipeline = ServiceLocator.Get<ICommandPipeline>();
        
        var command = context.SomeCondition 
            ? new OptionACommand() as IGameCommand
            : new OptionBCommand() as IGameCommand;
            
        return await pipeline.ExecuteAsync(command, context);
    }
}
```

### Pattern: Retry Command
```csharp
public class RetryableCommand : BaseGameCommand
{
    private const int MaxRetries = 3;
    
    protected override async UniTask<CommandResult> ExecuteAsyncCore(GameContext context)
    {
        for (int i = 0; i < MaxRetries; i++)
        {
            var result = await TryExecute(context);
            if (result.IsSuccess)
                return result;
                
            await UniTask.Delay(100 * (i + 1));
        }
        
        return CommandResult.Failure("Max retries exceeded");
    }
    
    private async UniTask<CommandResult> TryExecute(GameContext context)
    {
        // Actual execution logic
        return CommandResult.Success(context);
    }
}
```

## Code Quality Checklist

### For Every Generated File
- [ ] Single class in file
- [ ] No comments in code
- [ ] Self-documenting names
- [ ] Proper namespace
- [ ] Using statements at top
- [ ] Consistent formatting
- [ ] Region organization (if needed)
- [ ] Logging for important operations
- [ ] Error handling where appropriate
- [ ] Null checks for public methods

### For Commands
- [ ] Extends appropriate base class
- [ ] Validation in ValidateAsync
- [ ] Core logic in ExecuteAsyncCore
- [ ] ServiceLocator for dependencies
- [ ] Returns CommandResult
- [ ] Handles exceptions
- [ ] Updates context immutably
- [ ] Publishes events if needed

### For Services
- [ ] Implements interface
- [ ] Single responsibility
- [ ] No direct state changes
- [ ] Command-friendly methods
- [ ] Proper initialization
- [ ] Cleanup/Dispose if needed
- [ ] Thread-safe if shared

## Example Prompts for AI

### Good Prompts
✅ "Create a DrawCardCommand that validates the player has cards and draws one from their deck"
✅ "Create a StateTransitionCommand for moving from MainMenu to Initializing state"
✅ "Convert the existing PlayCard method to a PlayCardCommand following the template"
✅ "Create a bridge service that allows old event handlers to work with new commands"

### Bad Prompts (Too Vague)
❌ "Create a command"
❌ "Make the game work"
❌ "Fix the state system"
❌ "Add some services"

### Prompt Template
"Create a [CommandType]Command that [specific action/validation]. It should:
1. [Specific validation requirement]
2. [Specific execution requirement]
3. [Specific context update]
4. [Specific event to publish]
Follow the [template name] template from AGENTS.md"

## Notes for AI Systems

1. **Always check AGENTS.md first** for patterns and templates
2. **Never add comments** - code must be self-documenting
3. **One class per response** - never combine classes
4. **Use existing ServiceLocator** - don't create new DI systems
5. **GameContext is immutable** - use WithXXX() methods
6. **Commands are stateless** - no fields except input parameters
7. **Validation before execution** - always check preconditions
8. **Meaningful error messages** - help developers debug
9. **Consistent logging format** - [ClassName] Message
10. **Follow the architecture** - don't introduce new patterns