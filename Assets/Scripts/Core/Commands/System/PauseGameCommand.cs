using CardWar.Core.Commands.State;
using CardWar.Common.States;

namespace CardWar.Core.Commands.System
{
    public class PauseGameCommand : StateTransitionCommand
    {
        protected override GameState TargetState => GameState.Paused;
        protected override string TransitionReason => "User requested pause";
    }
}