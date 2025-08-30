using CardWar.Services;
using UnityEngine;

namespace CardWar.Core
{
    public class MainMenuState : BaseGameState
    {
        public override GameState StateType => GameState.MainMenu;

        public MainMenuState(IUIService uiService, IGameControllerService gameController, IAudioService audioService) 
            : base(uiService, gameController, audioService) { }

        public override void Enter()
        {
            Debug.Log($"[{GetType().Name}] Entering MainMenu state");
            _uiService?.ShowMainMenu(true);
            _uiService?.ToggleLoadingScreen(false);
            _uiService?.ShowGameUI(false);
            _uiService?.ToggleGameOverScreen(false, false);
        }

        public override void Exit()
        {
            Debug.Log($"[{GetType().Name}] Exiting MainMenu state");
            _uiService?.ShowMainMenu(false);
        }
    }

}