# Task Breakdown - CardWar Project

## Project Overview
Migration from Event-Driven to Command-Pipeline-Mediator Architecture with Command-Driven State Management

### Migration Status
- **Phase 1**: ✅ COMPLETED (Core Infrastructure)
- **Phase 2**: 🔄 IN PROGRESS (State Management)
- **Phase 3**: ⏳ PENDING (Game Commands)
- **Phase 4**: ⏳ PENDING (UI Integration)
- **Phase 5**: ⏳ PENDING (Service Refactoring)
- **Phase 6**: ⏳ PENDING (Cleanup & Optimization)

## Phase 1: Core Command Infrastructure ✅

### Status: COMPLETED
All core infrastructure implemented and reviewed. Testing blocked by service dependencies.

### 1.1 Command Foundation ✅
**Completed Files:**
- [x] `IGameCommand.cs` - Command interface
- [x] `BaseGameCommand.cs` - Abstract base with validation flow
- [x] `CommandResult.cs` - Result structure with success/failure
- [x] `GameContext.cs` - Immutable context with builder methods

### 1.2 Pipeline Implementation ✅
**Completed Files:**
- [x] `ICommandPipeline.cs` - Pipeline interface
- [x] `CommandPipeline.cs` - Pipeline with middleware support
- [x] Middleware capability for cross-cutting concerns

### 1.3 Mediator Pattern ✅
**Completed Files:**
- [x] `IGameMediator.cs` - Mediator interface
- [x] `GameMediator.cs` - Event publishing/subscription
- [x] Event subscription model

### 1.4 Basic Commands ✅
**Completed Files:**
- [x] `PauseGameCommand.cs` - System pause command
- [x] `ResumeGameCommand.cs` - System resume command

### Issues & Resolution
- ⚠️ ServiceLocator conflicts with existing implementation
- ⚠️ IGameStateService not found in current codebase
- ✅ Decision: Use existing ServiceLocator, create bridge for state service

## Phase 2: State Management System 🔄

### Status: IN PROGRESS
Implementing Command-Driven State Management to replace behavioral state machine.

### 2.1 State Infrastructure 🔄
**Current Task: Implementation**
- [ ] `ICommandStateManager.cs` - State management interface
  - GetCurrentState()
  - SetCurrentState()
  - CanTransition()
  - GetInvalidTransitionReason()
  
- [ ] `CommandStateManager.cs` - State manager implementation
  - Transition rule validation
  - State change tracking
  - Rule configuration
  
- [ ] `StateTransition.cs` - Transition data structure
  ```csharp
  public struct StateTransition
  {
      public GameState From { get; }
      public GameState To { get; }
      public string Reason { get; }
      public float Timestamp { get; }
  }
  ```

- [ ] `StateTransitionRules.cs` - Centralized rules
  - Valid transition paths
  - Conditional validations
  - Context requirements

### 2.2 State Transition Commands ⏳
**Next Task: Create transition commands**
- [ ] `StateTransitionCommand.cs` - Base class for transitions
  - Validation framework
  - Pre/post transition hooks
  - Event publishing
  
- [ ] `TransitionToMainMenuCommand.cs`
  - From: Any state
  - To: MainMenu
  - Validation: Cleanup checks
  
- [ ] `TransitionToInitializingCommand.cs`
  - From: MainMenu
  - To: Initializing
  - Validation: Player count = 2
  
- [ ] `TransitionToPlayingCommand.cs`
  - From: Initializing
  - To: Playing
  - Validation: Initialization complete
  
- [ ] `TransitionToGameOverCommand.cs`
  - From: Playing
  - To: GameOver
  - Validation: Victory condition met

### 2.3 Bridge Service ⏳
**Task: Maintain backward compatibility**
- [ ] `StateManagementBridge.cs` - IGameStateService implementation
  - Converts old API to commands
  - Maintains event compatibility
  - Wraps new state manager
  
- [ ] `LegacyEventAdapter.cs` - Event translation
  - Old events to new commands
  - New events to old handlers
  
- [ ] `StateCompatibilityLayer.cs` - Gradual migration
  - Feature flags for old/new
  - Parallel execution paths

### 2.4 Testing & Validation ⏳
- [ ] State transition unit tests
- [ ] Bridge compatibility tests
- [ ] Integration tests with existing code
- [ ] Performance benchmarks

### Deliverables for Phase 2
1. Working state management through commands
2. All states transitionable via commands
3. Old code still works through bridge
4. Comprehensive test coverage

## Phase 3: Game Command Migration ⏳

### Status: PENDING
Convert all game actions to commands.

### 3.1 Card Management Commands
- [ ] `DealCardsCommand.cs` - Initial card dealing
  - Validation: Game state, player count
  - Execution: Distribute 26 cards each
  - Events: CardsDealt
  
- [ ] `DrawCardCommand.cs` - Draw from deck
  - Validation: Has cards remaining
  - Execution: Move card to play area
  - Events: CardDrawn
  
- [ ] `PlayCardCommand.cs` - Play card to table
  - Validation: Player turn, card available
  - Execution: Place card in battle area
  - Events: CardPlayed

### 3.2 Battle Commands
- [ ] `CompareCardsCommand.cs` - Compare card values
  - Validation: Both cards played
  - Execution: Determine winner
  - Events: RoundWinner, TieDetected
  
- [ ] `CollectCardsCommand.cs` - Winner takes cards
  - Validation: Round complete
  - Execution: Move cards to winner
  - Events: CardsCollected

### 3.3 War Sequence Commands
- [ ] `InitiateWarCommand.cs` - Start war on tie
  - Validation: Tie detected, cards available
  - Execution: Place 3 face-down, 1 face-up
  - Events: WarStarted
  
- [ ] `ResolveWarCommand.cs` - Resolve war
  - Validation: War cards played
  - Execution: Determine war winner
  - Events: WarResolved, WarTied
  
- [ ] `CollectWarCardsCommand.cs` - Collect all war cards
  - Validation: War complete
  - Execution: Move all war cards to winner
  - Events: WarCardsCollected

### 3.4 Game Flow Commands
- [ ] `StartGameCommand.cs` - Initialize new game
  - Validation: Main menu state
  - Execution: Setup game, deal cards
  - Events: GameStarted
  
- [ ] `CheckVictoryCommand.cs` - Check win conditions
  - Validation: After each round
  - Execution: Check card counts
  - Events: VictoryAchieved
  
- [ ] `EndGameCommand.cs` - End current game
  - Validation: Victory or forfeit
  - Execution: Clean up, show results
  - Events: GameEnded
  
- [ ] `ResetGameCommand.cs` - Reset for new game
  - Validation: Game over state
  - Execution: Clear all game data
  - Events: GameReset

### Deliverables for Phase 3
1. All game actions as commands
2. Consistent validation patterns
3. Comprehensive event coverage
4. Unit tests for each command

## Phase 4: UI Integration ⏳

### Status: PENDING
Connect UI to command pipeline.

### 4.1 UI Command Dispatchers
- [ ] `ButtonCommandDispatcher.cs` - Button to command
  - Maps UI buttons to commands
  - Handles click events
  - Executes through pipeline
  
- [ ] `InputCommandDispatcher.cs` - Input to command
  - Keyboard/mouse input handling
  - Input validation
  - Command parameter mapping
  
- [ ] `DragDropCommandDispatcher.cs` - Drag actions
  - Card dragging to commands
  - Visual feedback
  - Command on drop

### 4.2 UI State Observers
- [ ] `StateChangeListener.cs` - UI state updates
  - Subscribe to state events
  - Update UI based on state
  - Enable/disable controls
  
- [ ] `CommandResultHandler.cs` - Result display
  - Show success/failure
  - Display messages
  - Animation triggers
  
- [ ] `UIUpdateMediator.cs` - Batch UI updates
  - Collect UI changes
  - Apply in batches
  - Performance optimization

### 4.3 UI Command Feedback
- [ ] `CommandProgressIndicator.cs` - Show progress
- [ ] `CommandQueueDisplay.cs` - Show pending commands
- [ ] `CommandHistoryPanel.cs` - Show command history

### Deliverables for Phase 4
1. UI fully integrated with commands
2. No direct game logic in UI
3. Responsive command feedback
4. Smooth user experience

## Phase 5: Service Layer Refactoring ⏳

### Status: PENDING
Align services with command pattern.

### 5.1 Service Updates
- [ ] Update `IAssetService` - Command-friendly loading
- [ ] Update `IAudioService` - Event-driven sounds
- [ ] Update `IGameControllerService` - Command dispatching
- [ ] Create `ICommandService` - Command utilities

### 5.2 Service Registration
- [ ] Update service registration order
- [ ] Add command services to ServiceLocator
- [ ] Implement lazy initialization
- [ ] Add service health checks

### Deliverables for Phase 5
1. Services aligned with commands
2. Improved service lifecycle
3. Better error handling
4. Performance optimizations

## Phase 6: Cleanup & Optimization ⏳

### Status: PENDING
Remove old code and optimize new system.

### 6.1 Legacy Removal
- [ ] Remove old state machine classes
- [ ] Remove event system (keep mediator)
- [ ] Remove bridge service (when safe)
- [ ] Clean up unused interfaces

### 6.2 Performance Optimization
- [ ] Implement command pooling
- [ ] Add context caching
- [ ] Optimize pipeline execution
- [ ] Profile memory usage

### 6.3 Documentation & Polish
- [ ] Update all documentation
- [ ] Add inline documentation
- [ ] Create developer guide
- [ ] Performance benchmarks

### Deliverables for Phase 6
1. Clean, optimized codebase
2. No legacy code remaining
3. Performance improvements
4. Complete documentation

## Current Sprint (Phase 2.1)

### Today's Tasks
1. **Create ICommandStateManager interface**
   - Define state management contract
   - Transition validation methods
   - State tracking methods

2. **Implement CommandStateManager**
   - Transition rule dictionary
   - Validation logic
   - State change tracking

3. **Create StateTransition struct**
   - Immutable transition data
   - Timestamp and reason

4. **Define transition rules**
   - Valid state paths
   - Context requirements

### Tomorrow's Tasks
1. Create StateTransitionCommand base class
2. Implement first transition command
3. Test state transitions

### This Week's Goals
- Complete Phase 2 State Management
- Have working state transitions via commands
- Bridge service operational
- All tests passing

## Development Workflow

### For Each Task
1. **Review Requirements**
   - Check documentation
   - Understand dependencies
   - Review templates in AGENTS.md

2. **Implementation**
   - Create one class per file
   - Follow naming conventions
   - No comments in code
   - Self-documenting

3. **Testing**
   - Unit test the component
   - Integration test with pipeline
   - Verify backward compatibility

4. **Documentation**
   - Update task status
   - Note any issues
   - Document decisions

### Code Review Checklist
- [ ] Single class per file
- [ ] No comments in code
- [ ] Follows naming conventions
- [ ] Uses ServiceLocator correctly
- [ ] Immutable GameContext
- [ ] Proper validation
- [ ] Meaningful error messages
- [ ] Comprehensive logging
- [ ] Unit tests included

## Risk Management

### Current Risks
1. **Service Dependencies**
   - Risk: Missing services block testing
   - Mitigation: Create minimal implementations

2. **Breaking Changes**
   - Risk: Old code stops working
   - Mitigation: Bridge pattern, gradual migration

3. **Performance Impact**
   - Risk: Commands slower than direct calls
   - Mitigation: Pooling, optimization phase

### Mitigation Strategies
- Incremental migration
- Parallel systems
- Comprehensive testing
- Performance monitoring
- Rollback capability

## Success Metrics

### Code Quality
- ✅ 100% single class per file
- ⏳ 80% test coverage
- ⏳ 0 circular dependencies
- ⏳ <30 lines per method
- ⏳ <3 levels of nesting

### Performance
- ⏳ <5ms command execution
- ⏳ <10% memory overhead
- ⏳ 60 FPS maintained
- ⏳ <100ms state transition

### Architecture
- ✅ Clear separation of concerns
- ✅ Testable components
- ⏳ Extensible design
- ⏳ Maintainable code

## Notes for Developers

### Critical First Step
**DELETE ALL OLD STATE MACHINE CODE FIRST**
- Remove entire StateMachine folder
- Delete all BaseState-derived classes
- Clean up any references in existing code
- This prevents confusion and conflicts

### Priority Order
1. Remove old state machine code
2. Get Phase 2 working (Dual State Management)
3. Test thoroughly with existing code via bridge
4. Begin Phase 3 (Game Commands)
5. UI integration can happen in parallel
6. Optimization comes last

### Common Issues & Solutions
- **ServiceLocator not found**: Use existing implementation
- **Old state references**: Use bridge pattern temporarily
- **Command fails validation**: Check both AppState and GameState
- **Animation blocking**: Check IsAnimating flag
- **Events not firing**: Verify mediator subscription

### Getting Help
1. Check AGENTS.md for patterns
2. Review Architecture-Migration-Plan.md
3. Look at completed Phase 1 code
4. Follow existing patterns

## Appendix: File Structure

### Current Structure
```
Assets/
├── Scripts/
│   ├── Commands/
│   │   ├── Base/
│   │   │   ├── IGameCommand.cs ✅
│   │   │   ├── BaseGameCommand.cs ✅
│   │   │   └── CommandResult.cs ✅
│   │   ├── State/
│   │   │   └── [State commands] ⏳
│   │   ├── Gameplay/
│   │   │   └── [Game commands] ⏳
│   │   └── System/
│   │       ├── PauseGameCommand.cs ✅
│   │       └── ResumeGameCommand.cs ✅
│   ├── Core/
│   │   ├── GameContext.cs ✅
│   │   ├── ICommandPipeline.cs ✅
│   │   ├── CommandPipeline.cs ✅
│   │   ├── IGameMediator.cs ✅
│   │   └── GameMediator.cs ✅
│   ├── Services/
│   │   ├── State/
│   │   │   ├── ICommandStateManager.cs ⏳
│   │   │   └── CommandStateManager.cs ⏳
│   │   └── Bridge/
│   │       └── StateManagementBridge.cs ⏳
│   └── [Existing code...]
└── Docs/
    ├── CardWar Development Guidelines.md ✅
    ├── Architecture-Migration-Plan.md ✅
    ├── AGENTS.md ✅
    └── Task-Breakdown.md ✅
```

## Final Notes

This is a living document. Update task status as work progresses. Mark items with:
- ⏳ PENDING - Not started
- 🔄 IN PROGRESS - Currently working
- ✅ COMPLETED - Done and tested
- ⚠️ BLOCKED - Has dependencies
- ❌ CANCELLED - No longer needed