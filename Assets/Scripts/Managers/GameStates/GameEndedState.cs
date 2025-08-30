using System;
using UnityEngine;
using CardWar.Services;
using CardWar.Common;

namespace CardWar.Core
{
    public class GameEndedState : BaseGameState
    {
        public override GameState StateType => GameState.GameEnded;

        public GameEndedState(IUIService uiService, IGameControllerService gameController, IAudioService audioService) 
            : base(uiService, gameController, audioService) { }

        public override void Enter()
        {
            Debug.Log($"[GameEndedState] Entering GameEnded state");
            _uiService?.ShowGameUI(false);
            _uiService?.ToggleGameOverScreen(true, false);
            _audioService?.StopMusic();
        }

        public override void Exit()
        {
            Debug.Log($"[GameEndedState] Exiting GameEnded state");
            _uiService?.ToggleGameOverScreen(false, false);
        }
    }
}