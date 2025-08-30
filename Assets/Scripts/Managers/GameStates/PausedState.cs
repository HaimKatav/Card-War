using CardWar.Common;
using CardWar.Services;
using UnityEngine;

namespace CardWar.Core
{
    public class PausedState : BaseGameState
    {
        public override GameState StateType => GameState.Paused;

        public PausedState(IUIService uiService, IGameControllerService gameController, IAudioService audioService) 
            : base(uiService, gameController, audioService) { }

        public override void Enter()
        {
            Debug.Log($"[{GetType().Name}] Entering Paused state");
            _gameController?.PauseGame();
        }

        public override void Exit()
        {
            Debug.Log($"[{GetType().Name}] Exiting Paused state");
            _gameController?.ResumeGame();
        }
    }
}