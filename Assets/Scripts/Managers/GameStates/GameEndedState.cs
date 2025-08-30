using CardWar.Services;
using UnityEngine;

namespace CardWar.Core
{
    public class GameEndedState : BaseGameState
    {
        public override GameState StateType => GameState.GameEnded;

        public GameEndedState(IUIService uiService, IGameControllerService gameController, IAudioService audioService) 
            : base(uiService, gameController, audioService) { }

        public override void Enter()
        {
            Debug.Log($"[{GetType().Name}] Entering GameEnded state");
            var playerWon = UnityEngine.Random.value > 0.5f;
            _uiService?.ToggleGameOverScreen(true, playerWon);
        }

        public override void Exit()
        {
            Debug.Log($"[{GetType().Name}] Exiting GameEnded state");
            _uiService?.ToggleGameOverScreen(false, false);
        }
    }
}