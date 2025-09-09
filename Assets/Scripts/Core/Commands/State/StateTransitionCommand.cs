using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CardWar.Core.Commands.Base;
using CardWar.Core.Context;
using CardWar.Common.States;
using CardWar.Services.State;
using CardWar.Services;

namespace CardWar.Core.Commands.State
{
    public abstract class StateTransitionCommand : BaseGameCommand
    {
        protected abstract AppState? TargetAppState { get; }
        protected abstract GameState? TargetGameState { get; }
        protected abstract string TransitionReason { get; }
        
        protected override async UniTask<CommandResult> ExecuteAsyncCore(GameContext context)
        {
            if (TargetAppState.HasValue)
            {
                var appStateManager = await ServiceLocator.Get<IAppStateManager>();
                if (!appStateManager.CanTransition(context.AppState, TargetAppState.Value, context))
                {
                    var reason = appStateManager.GetInvalidTransitionReason(context.AppState, TargetAppState.Value, context);
                    return CommandResult.Failure(context, reason);
                }
                
                var newContext = context.WithAppState(TargetAppState.Value);
                appStateManager.SetCurrentAppState(TargetAppState.Value, newContext);
                Debug.Log($"[{GetType().Name}] App state transitioned: {context.AppState} -> {TargetAppState.Value}");
                return CommandResult.Success(newContext);
            }
            
            if (TargetGameState.HasValue)
            {
                var gameStateManager = await ServiceLocator.Get<IGameStateManager>();
                if (!gameStateManager.CanTransition(context.GameState, TargetGameState.Value, context))
                {
                    var reason = gameStateManager.GetInvalidTransitionReason(context.GameState, TargetGameState.Value, context);
                    return CommandResult.Failure(context, reason);
                }
                
                var newContext = context.WithGameState(TargetGameState.Value);
                gameStateManager.SetCurrentGameState(TargetGameState.Value, newContext);
                Debug.Log($"[{GetType().Name}] Game state transitioned: {context.GameState} -> {TargetGameState.Value}");
                return CommandResult.Success(newContext);
            }
            
            return CommandResult.Failure(context, "No target state specified");
        }
    }
}
