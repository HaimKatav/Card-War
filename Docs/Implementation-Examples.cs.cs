// Unity CardWar Command-Pipeline-Mediator Implementation Examples
// Professional Unity/.NET patterns for AI code generation reference

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using CardWar.Common;
using CardWar.Services;

namespace CardWar.Core.Commands.Examples
{
    // EXAMPLE 1: Complete Command Implementation
    // Shows Unity-specific patterns, error handling, and performance tracking
    public class LoadGameCommand : BaseGameCommand
    {
        private readonly IAssetService _assetService;
        private readonly IGameStateService _gameStateService;
        
        public override string CommandName => "LoadGame";
        public override CommandPriority Priority => CommandPriority.System;
        
        public LoadGameCommand(IAssetService assetService, IGameStateService gameStateService, ILogger logger) 
            : base(logger)
        {
            _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
            _gameStateService = gameStateService ?? throw new ArgumentNullException(nameof(gameStateService));
        }
        
        protected override async UniTask<CommandResult> ExecuteCore(GameContext context)
        {
            // Step 1: Validate preconditions
            if (context.CurrentState != GameState.LoadingGame)
            {
                return CommandResult.ValidationFailed("Game must be in LoadingGame state");
            }
            
            var loadedAssets = new Dictionary<string, UnityEngine.Object>();
            
            try
            {
                // Step 2: Load UI Manager with progress tracking
                Logger.Log($"[{CommandName}] Loading UI Manager");
                var uiManagerPrefab = await _assetService.LoadAssetAsync<UIManager>(
                    context.Settings.UI_MANAGER_ASSET_PATH);
                    
                if (uiManagerPrefab == null)
                {
                    return CommandResult.Failure("Failed to load UI Manager prefab");
                }
                
                loadedAssets["UIManager"] = uiManagerPrefab;
                
                // Step 3: Load Game Board
                Logger.Log($"[{CommandName}] Loading Game Board");
                var boardPrefab = await _assetService.LoadAssetAsync<GameBoardController>(
                    context.Settings.PLAY_AREA_ASSET_PATH);
                    
                if (boardPrefab == null)
                {
                    return CommandResult.Failure("Failed to load Game Board prefab");
                }
                
                loadedAssets["GameBoard"] = boardPrefab;
                
                // Step 4: Initialize components (Unity-specific pattern)
                var uiManager = UnityEngine.Object.Instantiate(uiManagerPrefab);
                var gameBoard = UnityEngine.Object.Instantiate(boardPrefab);
                
                // Step 5: Initialize with dependency injection pattern
                uiManager.Initialize(_gameStateService);
                await gameBoard.Initialize();
                
                // Step 6: Return success with loaded data
                var result = new LoadGameResult
                {
                    UIManager = uiManager,
                    GameBoard = gameBoard,
                    LoadedAssets = loadedAssets
                };
                
                Logger.Log($"[{CommandName}] Game loading completed successfully");
                return CommandResult.Success(result);
            }
            catch (UnityException unityEx)
            {
                // Unity-specific exception handling
                Logger.LogError($"[{CommandName}] Unity error during loading: {unityEx.Message}");
                return CommandResult.Error(new GameException("Unity asset loading failed", unityEx));
            }
            catch (System.IO.FileNotFoundException fileEx)
            {
                // Asset not found handling
                Logger.LogError($"[{CommandName}] Asset file not found: {fileEx.FileName}");
                return CommandResult.Error(new GameException($"Required asset not found: {fileEx.FileName}", fileEx));
            }
        }
        
        public override bool CanExecute(GameContext context)
        {
            return context != null && 
                   context.IsValid && 
                   context.Settings != null &&
                   context.CurrentState == GameState.LoadingGame;
        }
        
        public override void OnError(GameException error)
        {
            // Custom error handling for load failures
            Logger.LogError($"[{CommandName}] Load failed: {error.Message}");
            
            // Notify other systems of load failure
            if (error.InnerException is UnityException)
            {
                // Handle Unity-specific errors
                _gameStateService.ChangeState(GameState.MainMenu);
            }
        }
    }
    
    // Supporting data classes
    public class LoadGameResult
    {
        public UIManager UIManager { get; set; }
        public GameBoardController GameBoard { get; set; }
        public Dictionary<string, UnityEngine.Object> LoadedAssets { get; set; }
    }
}

namespace CardWar.Core.Pipeline.Examples
{
    // EXAMPLE 2: Unity-Optimized Command Pipeline
    // Shows performance tracking, cancellation, and Unity lifecycle integration
    public class UnityCommandPipeline : MonoBehaviour, ICommandPipeline
    {
        [SerializeField] private int _maxConcurrentCommands = 10;
        [SerializeField] private bool _enablePerformanceTracking = true;
        
        private readonly List<IMiddleware> _middlewares = new();
        private readonly Queue<PendingCommand> _commandQueue = new();
        private readonly Dictionary<string, PerformanceMetrics> _performanceMetrics = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        
        private int _activeCommands = 0;
        private bool _isDisposed = false;
        
        public PipelineHealth Health => new()
        {
            IsHealthy = !_isDisposed && _activeCommands < _maxConcurrentCommands,
            PendingCommands = _commandQueue.Count + _activeCommands,
            AverageExecutionTime = CalculateAverageExecutionTime(),
            FailedCommands = _performanceMetrics.Values.Sum(m => m.FailureCount)
        };
        
        // Unity lifecycle integration
        private void Awake()
        {
            // Register default middleware
            UseMiddleware<ValidationMiddleware>();
            UseMiddleware<LoggingMiddleware>();
            
            if (_enablePerformanceTracking)
            {
                UseMiddleware<PerformanceMiddleware>();
            }
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
        
        public async UniTask<CommandResult> ExecuteAsync<T>(T command, GameContext context) where T : IGameCommand
        {
            if (_isDisposed)
                return CommandResult.Error(new ObjectDisposedException(nameof(UnityCommandPipeline)));
                
            if (command == null)
                return CommandResult.ValidationFailed("Command cannot be null");
                
            if (context == null)
                return CommandResult.ValidationFailed("Context cannot be null");
            
            // Check capacity
            if (_activeCommands >= _maxConcurrentCommands)
            {
                Debug.LogWarning($"[UnityCommandPipeline] Pipeline at capacity, queueing command: {command.CommandName}");
                return await QueueCommand(command, context);
            }
            
            return await ExecuteCommandInternal(command, context);
        }
        
        private async UniTask<CommandResult> ExecuteCommandInternal<T>(T command, GameContext context) where T : IGameCommand
        {
            var stopwatch = Stopwatch.StartNew();
            var commandId = Guid.NewGuid().ToString("N")[..8];
            
            try
            {
                Interlocked.Increment(ref _activeCommands);
                
                // Pre-execution middleware
                foreach (var middleware in _middlewares)
                {
                    var middlewareResult = await middleware.BeforeExecute(command, context);
                    if (!middlewareResult.ShouldContinue)
                    {
                        RecordPerformance(command.CommandName, stopwatch.Elapsed, false);
                        return middlewareResult.Result;
                    }
                }
                
                // Execute command with cancellation support
                var result = await command.ExecuteAsync(context).AttachExternalCancellation(_cancellationTokenSource.Token);
                
                // Post-execution middleware
                foreach (var middleware in _middlewares.AsEnumerable().Reverse())
                {
                    await middleware.AfterExecute(command, context, result);
                }
                
                RecordPerformance(command.CommandName, stopwatch.Elapsed, result.IsSuccess);
                return result;
            }
            catch (OperationCanceledException)
            {
                RecordPerformance(command.CommandName, stopwatch.Elapsed, false);
                return CommandResult.Cancelled();
            }
            catch (Exception ex)
            {
                RecordPerformance(command.CommandName, stopwatch.Elapsed, false);
                Debug.LogError($"[UnityCommandPipeline] Unexpected error executing {command.CommandName}: {ex}");
                return CommandResult.Error(ex);
            }
            finally
            {
                Interlocked.Decrement(ref _activeCommands);
                
                // Process queued commands if capacity available
                if (_commandQueue.Count > 0 && _activeCommands < _maxConcurrentCommands)
                {
                    ProcessNextQueuedCommand().Forget();
                }
            }
        }
        
        private async UniTask<CommandResult> QueueCommand<T>(T command, GameContext context) where T : IGameCommand
        {
            var tcs = new UniTaskCompletionSource<CommandResult>();
            var pendingCommand = new PendingCommand
            {
                Command = command,
                Context = context,
                CompletionSource = tcs
            };
            
            _commandQueue.Enqueue(pendingCommand);
            return await tcs.Task;
        }
        
        private async UniTaskVoid ProcessNextQueuedCommand()
        {
            if (_commandQueue.Count == 0) return;
            
            var pendingCommand = _commandQueue.Dequeue();
            
            try
            {
                var result = await ExecuteCommandInternal(pendingCommand.Command, pendingCommand.Context);
                pendingCommand.CompletionSource.TrySetResult(result);
            }
            catch (Exception ex)
            {
                pendingCommand.CompletionSource.TrySetException(ex);
            }
        }
        
        public async UniTask<CommandResult[]> ExecuteBatchAsync(IGameCommand[] commands, GameContext context)
        {
            if (commands == null || commands.Length == 0)
                return Array.Empty<CommandResult>();
            
            var tasks = commands.Select(cmd => ExecuteAsync(cmd, context)).ToArray();
            return await UniTask.WhenAll(tasks);
        }
        
        public ICommandPipeline UseMiddleware<T>() where T : IMiddleware
        {
            var middleware = Activator.CreateInstance<T>();
            _middlewares.Add(middleware);
            return this;
        }
        
        public void CancelAll()
        {
            _cancellationTokenSource.Cancel();
            
            // Complete all queued commands with cancellation
            while (_commandQueue.Count > 0)
            {
                var pendingCommand = _commandQueue.Dequeue();
                pendingCommand.CompletionSource.TrySetResult(CommandResult.Cancelled());
            }
        }
        
        private void RecordPerformance(string commandName, TimeSpan duration, bool success)
        {
            if (!_enablePerformanceTracking) return;
            
            if (!_performanceMetrics.TryGetValue(commandName, out var metrics))
            {
                metrics = new PerformanceMetrics();
                _performanceMetrics[commandName] = metrics;
            }
            
            metrics.TotalExecutions++;
            metrics.TotalDuration = metrics.TotalDuration.Add(duration);
            
            if (!success)
                metrics.FailureCount++;
                
            if (duration > metrics.MaxDuration)
                metrics.MaxDuration = duration;
                
            if (duration < metrics.MinDuration || metrics.MinDuration == TimeSpan.Zero)
                metrics.MinDuration = duration;
        }
        
        private TimeSpan CalculateAverageExecutionTime()
        {
            if (_performanceMetrics.Count == 0) return TimeSpan.Zero;
            
            var totalDuration = _performanceMetrics.Values.Aggregate(TimeSpan.Zero, (sum, m) => sum.Add(m.TotalDuration));
            var totalExecutions = _performanceMetrics.Values.Sum(m => m.TotalExecutions);
            
            return totalExecutions > 0 ? TimeSpan.FromTicks(totalDuration.Ticks / totalExecutions) : TimeSpan.Zero;
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _isDisposed = true;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            // Clean up middleware
            foreach (var middleware in _middlewares.OfType<IDisposable>())
            {
                middleware.Dispose();
            }
            
            _middlewares.Clear();
            _performanceMetrics.Clear();
        }
        
        private class PendingCommand
        {
            public IGameCommand Command { get; set; }
            public GameContext Context { get; set; }
            public UniTaskCompletionSource<CommandResult> CompletionSource { get; set; }
        }
        
        private class PerformanceMetrics
        {
            public int TotalExecutions { get; set; }
            public int FailureCount { get; set; }
            public TimeSpan TotalDuration { get; set; }
            public TimeSpan MinDuration { get; set; }
            public TimeSpan MaxDuration { get; set; }
        }
    }
}

namespace CardWar.Core.Orchestrators.Examples
{
    // EXAMPLE 3: Professional Orchestrator Implementation
    // Shows complex workflow coordination with error recovery
    public class RoundOrchestrator : BaseOrchestrator
    {
        private readonly IWarServer _warServer;
        private readonly IAnimationService _animationService;
        private readonly WarOrchestrator _warOrchestrator;
        
        public RoundOrchestrator(
            IGameMediator mediator,
            ICommandPipeline pipeline,
            IWarServer warServer,
            IAnimationService animationService,
            WarOrchestrator warOrchestrator,
            ILogger logger) : base(mediator, pipeline, logger)
        {
            _warServer = warServer ?? throw new ArgumentNullException(nameof(warServer));
            _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
            _warOrchestrator = warOrchestrator ?? throw new ArgumentNullException(nameof(warOrchestrator));
        }
        
        public override async UniTask<OrchestratorResult> ExecuteAsync(GameContext context)
        {
            var orchestrationId = Guid.NewGuid().ToString("N")[..8];
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                Logger.Log($"[RoundOrchestrator:{orchestrationId}] Starting round orchestration");
                NotifyProgress("Initializing round", 0f);
                
                // Step 1: Validate round can be played
                var validateCommand = new ValidateRoundCommand();
                var validationResult = await ExecuteCommand<CommandResult>(validateCommand, context);
                
                if (!validationResult.IsSuccess)
                {
                    return OrchestratorResult.Failure($"Round validation failed: {validationResult.Message}");
                }
                
                NotifyProgress("Validation complete", 0.1f);
                
                // Step 2: Draw cards from server
                var drawCardsCommand = new DrawCardsCommand(_warServer);
                var drawResult = await ExecuteCommand<CommandResult>(drawCardsCommand, context);
                
                if (!drawResult.IsSuccess)
                {
                    // Retry once for server communication issues
                    Logger.LogWarning($"[RoundOrchestrator:{orchestrationId}] Draw failed, retrying...");
                    await UniTask.Delay(1000, cancellationToken: CancellationToken);
                    drawResult = await ExecuteCommand<CommandResult>(drawCardsCommand, context);
                    
                    if (!drawResult.IsSuccess)
                    {
                        return OrchestratorResult.Failure($"Card draw failed: {drawResult.Message}");
                    }
                }
                
                var roundData = (RoundData)drawResult.Data;
                var updatedContext = context.WithRound(roundData);
                
                NotifyProgress("Cards drawn", 0.3f);
                
                // Step 3: Animate card reveal
                try
                {
                    await _animationService.AnimateCardReveal(
                        roundData.PlayerCards,
                        roundData.OpponentCards,
                        CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw; // Re-throw cancellation
                }
                catch (Exception animEx)
                {
                    // Animation failures are non-critical, log and continue
                    Logger.LogWarning($"[RoundOrchestrator:{orchestrationId}] Animation failed: {animEx.Message}");
                }
                
                NotifyProgress("Cards revealed", 0.5f);
                
                // Step 4: Compare cards and determine outcome
                var compareCommand = new CompareCardsCommand();
                var compareResult = await ExecuteCommand<CommandResult>(compareCommand, updatedContext);
                
                if (!compareResult.IsSuccess)
                {
                    return OrchestratorResult.Failure($"Card comparison failed: {compareResult.Message}");
                }
                
                var comparisonData = (CardComparisonResult)compareResult.Data;
                NotifyProgress("Cards compared", 0.7f);
                
                // Step 5: Handle war scenario if needed
                if (comparisonData.IsWar)
                {
                    Logger.Log($"[RoundOrchestrator:{orchestrationId}] War scenario detected");
                    
                    var warResult = await _warOrchestrator.ExecuteAsync(updatedContext);
                    if (!warResult.IsSuccess)
                    {
                        return OrchestratorResult.Failure($"War handling failed: {warResult.Message}");
                    }
                    
                    var warData = (WarResult)warResult.Data;
                    updatedContext = warData.Context;
                    
                    NotifyProgress("War resolved", 0.9f);
                }
                else
                {
                    // Normal round completion
                    var awardCardsCommand = new AwardCardsCommand(comparisonData.Winner, comparisonData.AllCards);
                    var awardResult = await ExecuteCommand<CommandResult>(awardCardsCommand, updatedContext);
                    
                    if (!awardResult.IsSuccess)
                    {
                        return OrchestratorResult.Failure($"Card awarding failed: {awardResult.Message}");
                    }
                    
                    updatedContext = (GameContext)awardResult.Data;
                    NotifyProgress("Cards awarded", 0.9f);
                }
                
                // Step 6: Update game state and check for game end
                var updateStateCommand = new UpdateGameStateCommand();
                var stateResult = await ExecuteCommand<CommandResult>(updateStateCommand, updatedContext);
                
                if (!stateResult.IsSuccess)
                {
                    return OrchestratorResult.Failure($"State update failed: {stateResult.Message}");
                }
                
                var finalContext = (GameContext)stateResult.Data;
                NotifyProgress("Round complete", 1f);
                
                // Step 7: Determine if game has ended
                var gameEndResult = CheckForGameEnd(finalContext);
                if (gameEndResult.IsGameOver)
                {
                    NotifyStepComplete("GameEnded", new 
                    { 
                        Winner = gameEndResult.Winner.Name,
                        RoundNumber = finalContext.CurrentRound.RoundNumber,
                        Duration = stopwatch.Elapsed
                    });
                }
                else
                {
                    NotifyStepComplete("RoundComplete", new 
                    { 
                        RoundNumber = finalContext.CurrentRound.RoundNumber,
                        IsWar = comparisonData.IsWar,
                        Duration = stopwatch.Elapsed
                    });
                }
                
                stopwatch.Stop();
                Logger.Log($"[RoundOrchestrator:{orchestrationId}] Round orchestration completed in {stopwatch.ElapsedMilliseconds}ms");
                
                return OrchestratorResult.Success(
                    data: new RoundOrchestrationResult 
                    { 
                        Context = finalContext,
                        IsGameOver = gameEndResult.IsGameOver,
                        Winner = gameEndResult.Winner
                    },
                    duration: stopwatch.Elapsed
                );
            }
            catch (OperationCanceledException)
            {
                Logger.Log($"[RoundOrchestrator:{orchestrationId}] Round orchestration cancelled");
                return OrchestratorResult.Failure("Round was cancelled");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError($"[RoundOrchestrator:{orchestrationId}] Unexpected error: {ex}");
                return OrchestratorResult.Failure($"Round orchestration failed: {ex.Message}");
            }
        }
        
        private (bool IsGameOver, Player Winner) CheckForGameEnd(GameContext context)
        {
            var playersWithCards = context.Players.Where(p => p.CardCount > 0).ToArray();
            
            if (playersWithCards.Length == 1)
            {
                return (true, playersWithCards[0]);
            }
            
            return (false, null);
        }
        
        private class RoundOrchestrationResult
        {
            public GameContext Context { get; set; }
            public bool IsGameOver { get; set; }
            public Player Winner { get; set; }
        }
        
        private class CardComparisonResult
        {
            public bool IsWar { get; set; }
            public Player Winner { get; set; }
            public Card[] AllCards { get; set; }
        }
        
        private class WarResult
        {
            public GameContext Context { get; set; }
            public Player Winner { get; set; }
        }
    }
}

namespace CardWar.Core.Mediator.Examples
{
    // EXAMPLE 4: Unity-Integrated Mediator with Performance Optimization
    public class UnityGameMediator : MonoBehaviour, IGameMediator
    {
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private int _maxSubscribers = 100;
        
        private readonly Dictionary<Type, List<object>> _handlers = new();
        private readonly Dictionary<Type, List<IDisposable>> _subscriptions = new();
        private readonly Dictionary<Type, object> _requestHandlers = new();
        private bool _isDisposed = false;
        
        // Unity lifecycle integration
        private void Awake()
        {
            // Initialize with expected capacity
            DontDestroyOnLoad(gameObject);
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
        
        public async UniTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(UnityGameMediator));
                
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            
            var requestType = request.GetType();
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
            
            if (_requestHandlers.TryGetValue(handlerType, out var handlerObj))
            {
                var handler = (IRequestHandler<IRequest<TResponse>, TResponse>)handlerObj;
                
                if (_enableDebugLogging)
                {
                    Debug.Log($"[UnityGameMediator] Sending request: {requestType.Name}");
                }
                
                try
                {
                    return await handler.HandleAsync(request);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UnityGameMediator] Request handler failed for {requestType.Name}: {ex}");
                    throw;
                }
            }
            
            throw new InvalidOperationException($"No handler registered for request type: {requestType.Name}");
        }
        
        public void Publish<T>(T notification) where T : INotification
        {
            if (_isDisposed) return;
            
            if (notification == null)
            {
                Debug.LogWarning("[UnityGameMediator] Attempted to publish null notification");
                return;
            }
            
            var notificationType = typeof(T);
            
            if (_handlers.TryGetValue(notificationType, out var handlers))
            {
                if (_enableDebugLogging)
                {
                    Debug.Log($"[UnityGameMediator] Publishing notification: {notificationType.Name} to {handlers.Count} handlers");
                }
                
                // Execute handlers on main thread for Unity compatibility
                foreach (var handler in handlers.ToArray()) // ToArray to avoid modification during iteration
                {
                    try
                    {
                        if (handler is Func<T, UniTask> asyncHandler)
                        {
                            asyncHandler(notification).Forget();
                        }
                        else if (handler is Action<T> syncHandler)
                        {
                            syncHandler(notification);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[UnityGameMediator] Notification handler failed for {notificationType.Name}: {ex}");
                        // Continue with other handlers even if one fails
                    }
                }
            }
            else if (_enableDebugLogging)
            {
                Debug.Log($"[UnityGameMediator] No handlers for notification: {notificationType.Name}");
            }
        }
        
        public IDisposable Subscribe<T>(Func<T, UniTask> handler) where T : INotification
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(UnityGameMediator));
                
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            var notificationType = typeof(T);
            
            if (!_handlers.TryGetValue(notificationType, out var handlerList))
            {
                handlerList = new List<object>();
                _handlers[notificationType] = handlerList;
            }
            
            if (handlerList.Count >= _maxSubscribers)
            {
                Debug.LogWarning($"[UnityGameMediator] Maximum subscribers reached for {notificationType.Name}");
            }
            
            handlerList.Add(handler);
            
            // Return disposable subscription
            var subscription = new MediatorSubscription(() => 
            {
                if (_handlers.TryGetValue(notificationType, out var currentHandlers))
                {
                    currentHandlers.Remove(handler);
                    
                    if (currentHandlers.Count == 0)
                    {
                        _handlers.Remove(notificationType);
                    }
                }
            });
            
            // Track subscription for cleanup
            if (!_subscriptions.TryGetValue(notificationType, out var subscriptionList))
            {
                subscriptionList = new List<IDisposable>();
                _subscriptions[notificationType] = subscriptionList;
            }
            
            subscriptionList.Add(subscription);
            
            if (_enableDebugLogging)
            {
                Debug.Log($"[UnityGameMediator] Subscribed to {notificationType.Name} (Total: {handlerList.Count})");
            }
            
            return subscription;
        }
        
        public void RegisterHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
            where TRequest : IRequest<TResponse>
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(UnityGameMediator));
                
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            var handlerType = typeof(IRequestHandler<TRequest, TResponse>);
            
            if (_requestHandlers.ContainsKey(handlerType))
            {
                Debug.LogWarning($"[UnityGameMediator] Handler already registered for {typeof(TRequest).Name}, replacing");
            }
            
            _requestHandlers[handlerType] = handler;
            
            if (_enableDebugLogging)
            {
                Debug.Log($"[UnityGameMediator] Registered handler for {typeof(TRequest).Name}");
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _isDisposed = true;
            
            // Dispose all subscriptions
            foreach (var subscriptionList in _subscriptions.Values)
            {
                foreach (var subscription in subscriptionList)
                {
                    try
                    {
                        subscription.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[UnityGameMediator] Error disposing subscription: {ex}");
                    }
                }
            }
            
            // Dispose request handlers if they implement IDisposable
            foreach (var handler in _requestHandlers.Values.OfType<IDisposable>())
            {
                try
                {
                    handler.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UnityGameMediator] Error disposing request handler: {ex}");
                }
            }
            
            _handlers.Clear();
            _subscriptions.Clear();
            _requestHandlers.Clear();
            
            if (_enableDebugLogging)
            {
                Debug.Log("[UnityGameMediator] Disposed successfully");
            }
        }
        
        private class MediatorSubscription : IDisposable
        {
            private readonly Action _unsubscribeAction;
            private bool _isDisposed = false;
            
            public MediatorSubscription(Action unsubscribeAction)
            {
                _unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));
            }
            
            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _unsubscribeAction();
                    _isDisposed = true;
                }
            }
        }
    }
    
    // Example notification classes
    public class ProgressNotification : INotification
    {
        public string EventType => "Progress";
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public object Payload { get; }
        
        public string Step { get; }
        public float Progress { get; }
        
        public ProgressNotification(string step, float progress)
        {
            Step = step ?? throw new ArgumentNullException(nameof(step));
            Progress = Mathf.Clamp01(progress);
            Payload = new { Step, Progress };
        }
    }
    
    public class StepCompleteNotification : INotification
    {
        public string EventType => "StepComplete";
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public object Payload { get; }
        
        public string Step { get; }
        public object Data { get; }
        
        public StepCompleteNotification(string step, object data = null)
        {
            Step = step ?? throw new ArgumentNullException(nameof(step));
            Data = data;
            Payload = new { Step, Data };
        }
    }
}

// EXAMPLE 5: Unity MonoBehaviour Integration Pattern
namespace CardWar.Managers.Examples
{
    public class ModernGameManager : MonoBehaviour, IGameStateService
    {
        [Header("Dependencies")]
        [SerializeField] private ServiceInstaller _serviceInstaller;
        [SerializeField] private GameSettings _gameSettings;
        
        // Orchestrators (injected via DI)
        private LoadGameOrchestrator _loadGameOrchestrator;
        private IGameMediator _mediator;
        private IServiceContainer _serviceContainer;
        
        // State management
        private GameStateMachine _stateMachine;
        private GameContext _currentContext;
        
        // Events for backward compatibility
        public event Action<GameState> GameStateChanged;
        public event Action<float, string> OnLoadingProgress;
        
        public GameState CurrentState => _stateMachine?.CurrentStateType ?? GameState.FirstLoad;
        public GameStatus MatchStatus { get; private set; } = GameStatus.NotStarted;
        
        #region Unity Lifecycle
        
        private async void Awake()
        {
            // Ensure singleton pattern
            if (FindObjectsOfType<ModernGameManager>().Length > 1)
            {
                Destroy(gameObject);
                return;
            }
            
            DontDestroyOnLoad(gameObject);
            
            await InitializeAsync();
        }
        
        private void OnDestroy()
        {
            Cleanup();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && CurrentState == GameState.Playing)
            {
                // Use pause orchestrator instead of direct state change
                HandleApplicationPause().Forget();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private async UniTask InitializeAsync()
        {
            try
            {
                Debug.Log("[ModernGameManager] Starting initialization");
                
                // Step 1: Setup dependency injection
                _serviceContainer = new ServiceContainer();
                _serviceInstaller.InstallServices(_serviceContainer);
                
                // Step 2: Resolve dependencies
                _mediator = _serviceContainer.Resolve<IGameMediator>();
                _loadGameOrchestrator = _serviceContainer.Resolve<LoadGameOrchestrator>();
                
                // Step 3: Setup state machine
                SetupStateMachine();
                
                // Step 4: Subscribe to mediator notifications
                SubscribeToNotifications();
                
                // Step 5: Create initial game context
                _currentContext = GameContext.Create(_gameSettings, new Player[0], null);
                
                Debug.Log("[ModernGameManager] Initialization complete");
                ChangeState(GameState.MainMenu);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModernGameManager] Initialization failed: {ex}");
                ChangeState(GameState.Error);
            }
        }
        
        private void SetupStateMachine()
        {
            _stateMachine = new GameStateMachine();
            
            // Register states with enter/exit actions
            _stateMachine.RegisterState(GameState.FirstLoad);
            _stateMachine.RegisterState(GameState.MainMenu, OnEnterMainMenu, OnExitMainMenu);
            _stateMachine.RegisterState(GameState.LoadingGame, OnEnterLoadingGame);
            _stateMachine.RegisterState(GameState.Playing, OnEnterPlaying);
            _stateMachine.RegisterState(GameState.Paused, OnEnterPaused);
            _stateMachine.RegisterState(GameState.GameEnded, OnEnterGameEnded);
            _stateMachine.RegisterState(GameState.Error, OnEnterError);
        }
        
        private void SubscribeToNotifications()
        {
            _mediator.Subscribe<ProgressNotification>(HandleProgressNotification);
            _mediator.Subscribe<StepCompleteNotification>(HandleStepCompleteNotification);
            _mediator.Subscribe<GameEndNotification>(HandleGameEndNotification);
        }
        
        #endregion
        
        #region State Management
        
        public void ChangeState(GameState newState)
        {
            if (_stateMachine == null)
            {
                Debug.LogError("[ModernGameManager] State machine not initialized");
                return;
            }
            
            var oldState = CurrentState;
            _stateMachine.ChangeState(newState);
            
            // Update context
            _currentContext = _currentContext.WithState(newState);
            
            // Fire events for backward compatibility
            GameStateChanged?.Invoke(newState);
            
            Debug.Log($"[ModernGameManager] State transition: {oldState} -> {newState}");
        }
        
        // State enter/exit handlers
        private void OnEnterMainMenu() => Debug.Log("[ModernGameManager] Entered Main Menu");
        private void OnExitMainMenu() => Debug.Log("[ModernGameManager] Exited Main Menu");
        
        private void OnEnterLoadingGame()
        {
            Debug.Log("[ModernGameManager] Entered Loading Game");
            LoadGameAsync().Forget();
        }
        
        private void OnEnterPlaying() => Debug.Log("[ModernGameManager] Entered Playing");
        private void OnEnterPaused() => Debug.Log("[ModernGameManager] Entered Paused");
        private void OnEnterGameEnded() => Debug.Log("[ModernGameManager] Entered Game Ended");
        private void OnEnterError() => Debug.LogError("[ModernGameManager] Entered Error State");
        
        #endregion
        
        #region Game Operations
        
        private async UniTask LoadGameAsync()
        {
            try
            {
                Debug.Log("[ModernGameManager] Starting game load via orchestrator");
                
                var result = await _loadGameOrchestrator.ExecuteAsync(_currentContext);
                
                if (result.IsSuccess)
                {
                    Debug.Log($"[ModernGameManager] Game loaded successfully in {result.Duration.TotalMilliseconds}ms");
                    ChangeState(GameState.Playing);
                }
                else
                {
                    Debug.LogError($"[ModernGameManager] Game load failed: {result.Message}");
                    ChangeState(GameState.MainMenu);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModernGameManager] Game load exception: {ex}");
                ChangeState(GameState.MainMenu);
            }
        }
        
        private async UniTaskVoid HandleApplicationPause()
        {
            try
            {
                var pauseOrchestrator = _serviceContainer.Resolve<PauseOrchestrator>();
                var result = await pauseOrchestrator.ExecuteAsync(_currentContext);
                
                if (result.IsSuccess)
                {
                    ChangeState(GameState.Paused);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModernGameManager] Pause handling failed: {ex}");
            }
        }
        
        #endregion
        
        #region Notification Handlers
        
        private async UniTask HandleProgressNotification(ProgressNotification notification)
        {
            OnLoadingProgress?.Invoke(notification.Progress, notification.Step);
            Debug.Log($"[ModernGameManager] Progress: {notification.Step} ({notification.Progress:P})");
        }
        
        private async UniTask HandleStepCompleteNotification(StepCompleteNotification notification)
        {
            Debug.Log($"[ModernGameManager] Step complete: {notification.Step}");
            
            switch (notification.Step)
            {
                case "GameLoaded":
                    // Game loading completed
                    break;
                    
                case "RoundComplete":
                    // Round completed
                    break;
                    
                case "GameEnded":
                    var gameEndData = notification.Data as dynamic;
                    MatchStatus = gameEndData?.Winner != null ? GameStatus.PlayerWon : GameStatus.OpponentWon;
                    ChangeState(GameState.GameEnded);
                    break;
            }
        }
        
        private async UniTask HandleGameEndNotification(GameEndNotification notification)
        {
            MatchStatus = notification.Winner.Name == "Player" ? GameStatus.PlayerWon : GameStatus.OpponentWon;
            ChangeState(GameState.GameEnded);
        }
        
        #endregion
        
        #region Cleanup
        
        private void Cleanup()
        {
            try
            {
                _stateMachine?.Clear();
                _serviceContainer?.Dispose();
                
                Debug.Log("[ModernGameManager] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModernGameManager] Cleanup error: {ex}");
            }
        }
        
        #endregion
        
        #region Public API (Backward Compatibility)
        
        public void StartNewGame()
        {
            if (CurrentState == GameState.MainMenu)
            {
                ChangeState(GameState.LoadingGame);
            }
        }
        
        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                HandleApplicationPause().Forget();
            }
        }
        
        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
            }
        }
        
        public void ReturnToMenu()
        {
            ChangeState(GameState.MainMenu);
        }
        
        #endregion
    }
    
    // Supporting notification class
    public class GameEndNotification : INotification
    {
        public string EventType => "GameEnd";
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public object Payload { get; }
        
        public Player Winner { get; }
        public int TotalRounds { get; }
        
        public GameEndNotification(Player winner, int totalRounds)
        {
            Winner = winner ?? throw new ArgumentNullException(nameof(winner));
            TotalRounds = totalRounds;
            Payload = new { Winner = winner.Name, TotalRounds };
        }
    }
}