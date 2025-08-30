using System;
using CardWar.Common;
using CardWar.Services;
using UnityEngine;

namespace CardWar.Core
{
    public class ReturnToMenuState : BaseGameState
    {
        private readonly Action _onComplete;
        
        public override GameState StateType => GameState.ReturnToMenu;

        public ReturnToMenuState(IUIService uiService, IGameControllerService gameController, 
            IAudioService audioService, Action onComplete) 
            : base(uiService, gameController, audioService)
        {
            _onComplete = onComplete;
        }

        public override void Enter()
        {
            Debug.Log($"[ReturnToMenuState] Entering ReturnToMenu state");
            _uiService?.ShowGameUI(false);
            _uiService?.ToggleGameOverScreen(false, false);
            _gameController?.ResetGame();
            _onComplete?.Invoke();
        }

        public override void Exit()
        {
            Debug.Log($"[ReturnToMenuState] Exiting ReturnToMenu state");
        }
    }
}