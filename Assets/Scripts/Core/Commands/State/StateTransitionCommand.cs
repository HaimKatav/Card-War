using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using CardWar.Core.Commands.Base;
using CardWar.Core.Context;
using CardWar.Core.StateManagement;
using CardWar.Common;
using CardWar.Services;

namespace CardWar.Core.Commands.State
{
    public abstract class StateTransitionCommand : BaseGameCommand
    {
        protected abstract GameState TargetState { get; }
        protected abstract string TransitionReason { get; }
        
        protected override async UniTask<CommandResult> ExecuteAsyncCore(GameContext context)
        {
            var stateManager = await ServiceLocator.Get<ICommandStateManager>();
            
            if (!stateManager.CanTransitionTo(TargetState))
            {
                return CommandResult.Failure(
                    context, 
                    $"Cannot transition from {stateManager.CurrentState} to {TargetState}"
                );
            }
            
            var validationResult = await ValidateTransition(context, stateManager);
            if (!validationResult.IsValid)
            {
                return CommandResult.Failure(context, validationResult.Reason);
            }
            
            var newContext = await PrepareContext(context, stateManager);
            
            stateManager.TransitionTo(TargetState, newContext);
            
            await OnTransitionComplete(newContext, stateManager);
            
            Debug.Log($"[{GetType().Name}] Transitioned to {TargetState}: {TransitionReason}");
            
            return CommandResult.Success(newContext);
        }
        
        protected virtual async UniTask<ValidationResult> ValidateTransition(
            GameContext context, 
            ICommandStateManager stateManager)
        {
            await UniTask.Yield();
            return ValidationResult.Valid();
        }
        
        protected virtual async UniTask<GameContext> PrepareContext(
            GameContext context, 
            ICommandStateManager stateManager)
        {
            await UniTask.Yield();
            return context.WithState(TargetState);
        }
        
        protected virtual async UniTask OnTransitionComplete(
            GameContext context, 
            ICommandStateManager stateManager)
        {
            await UniTask.Yield();
        }
        
        protected struct ValidationResult
        {
            public bool IsValid { get; }
            public string Reason { get; }
            
            private ValidationResult(bool isValid, string reason)
            {
                IsValid = isValid;
                Reason = reason;
            }
            
            public static ValidationResult Valid() => new ValidationResult(true, null);
            public static ValidationResult Invalid(string reason) => new ValidationResult(false, reason);
        }
    }
}