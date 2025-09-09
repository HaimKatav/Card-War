using UnityEngine;
using Cysharp.Threading.Tasks;
using CardWar.Core.Commands.Base;
using CardWar.Core.Context;
using CardWar.Common.States;
using CardWar.Services;
using CardWar.Services.State;

namespace CardWar.Core.Commands.State
{
    public abstract class StateTransitionCommand : BaseGameCommand
    {
        protected abstract GameState TargetState { get; }
        protected abstract string TransitionReason { get; }

        protected override async UniTask<CommandResult> ExecuteAsyncCore(GameContext context)
        {
            var stateManager = ServiceLocator.Get<IGameStateManager>();
            var currentState = stateManager.GetCurrentGameState();
            if (!stateManager.CanTransition(currentState, TargetState, context))
            {
                var reason = stateManager.GetInvalidTransitionReason(currentState, TargetState, context);
                Debug.LogWarning($"[{GetType().Name}] {reason}");
                return CommandResult.Failure(context, reason);
            }
            var newContext = context.WithGameState(TargetState);
            stateManager.SetCurrentGameState(TargetState, newContext);
            Debug.Log($"[{GetType().Name}] {TransitionReason}");
            return CommandResult.Success(newContext);
        }
    }
}
