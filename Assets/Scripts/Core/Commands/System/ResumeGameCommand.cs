using CardWar.Core.Commands.State;
using CardWar.Common.States;

namespace CardWar.Core.Commands.System
{
    public class ResumeGameCommand : StateTransitionCommand
    {
        protected override GameState TargetState => GameState.PlayerTurn;
        protected override string TransitionReason => "User resumed game";
    }
}