using CardWar.Services;
using UnityEngine;

namespace CardWar.Core
{
    public class PlayingState : BaseGameState
    {
        public override GameState StateType => GameState.Playing;

        public PlayingState(IUIService uiService, IGameControllerService gameController, IAudioService audioService) 
            : base(uiService, gameController, audioService) { }

        public override void Enter()
        {
            Debug.Log($"[{GetType().Name}] Entering Playing state");
            _uiService?.ShowGameUI(true);
        }

        public override void Exit()
        {
            Debug.Log($"[{GetType().Name}] Exiting Playing state");
        }
    }
}