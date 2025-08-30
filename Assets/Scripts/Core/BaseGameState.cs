using CardWar.Common;
using CardWar.Services;

namespace CardWar.Core
{
    public abstract class BaseGameState : IGameState
    {
        protected readonly IUIService _uiService;
        protected readonly IGameControllerService _gameController;
        protected readonly IAudioService _audioService;
        
        public abstract GameState StateType { get; }

        protected BaseGameState(IUIService uiService, IGameControllerService gameController, IAudioService audioService)
        {
            _uiService = uiService;
            _gameController = gameController;
            _audioService = audioService;
        }

        public abstract void Enter();
        public abstract void Exit();
        public virtual void Update() { }
    }
}