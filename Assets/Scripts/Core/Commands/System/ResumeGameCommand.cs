using CardWar.Core.Commands.State;
using CardWar.Common;

namespace CardWar.Core.Commands.System
{
    public class ResumeGameCommand : StateTransitionCommand
    {
        protected override GameState TargetState => GameState.Playing;
        protected override string TransitionReason => "User resumed game";
    }
}