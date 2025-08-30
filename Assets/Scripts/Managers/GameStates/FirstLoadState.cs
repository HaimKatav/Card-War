using CardWar.Common;
using CardWar.Services;
using UnityEngine;

namespace CardWar.Core
{
    public class FirstLoadState : BaseGameState
    {
        public override GameState StateType => GameState.FirstLoad;

        public FirstLoadState(IUIService uiService, IGameControllerService gameController, IAudioService audioService) 
            : base(uiService, gameController, audioService) { }

        public override void Enter()
        {
            Debug.Log($"[{GetType().Name}] Entering FirstLoad state");
        }

        public override void Exit()
        {
            Debug.Log($"[{GetType().Name}] Exiting FirstLoad state");
        }
    }
}