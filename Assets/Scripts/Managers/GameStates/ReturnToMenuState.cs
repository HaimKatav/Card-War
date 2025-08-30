using System;
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
            Debug.Log($"[{GetType().Name}] Entering ReturnToMenu state");
            _gameController?.ReturnToMenu();
            _uiService?.ShowGameUI(false);
            _onComplete?.Invoke();
        }

        public override void Exit()
        {
            Debug.Log($"[{GetType().Name}] Exiting ReturnToMenu state");
        }
    }

}