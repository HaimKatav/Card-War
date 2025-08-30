using System;
using CardWar.Common;
using CardWar.Services;
using UnityEngine;

namespace CardWar.Core
{
    public class LoadingGameState : BaseGameState
    {
        private readonly Action<float> _onProgress;
        
        public override GameState StateType => GameState.LoadingGame;

        public LoadingGameState(IUIService uiService, IGameControllerService gameController, 
            IAudioService audioService, Action<float> onProgress) 
            : base(uiService, gameController, audioService)
        {
            _onProgress = onProgress;
        }

        public override void Enter()
        {
            Debug.Log($"[{GetType().Name}] Entering LoadingGame state");
            _uiService?.ToggleLoadingScreen(true);
            _gameController?.StartNewGame();
            SimulateLoading();
        }

        public override void Exit()
        {
            Debug.Log($"[{GetType().Name}] Exiting LoadingGame state");
            _uiService?.ToggleLoadingScreen(false);
        }

        private async void SimulateLoading()
        {
            for (var i = 0; i <= 10; i++)
            {
                _onProgress?.Invoke(i / 10f);
                await Cysharp.Threading.Tasks.UniTask.Delay(200);
            }
        }
    }
}