using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Services;

namespace CardWar.Core
{
    public interface IGameState
    {
        GameState StateType { get; }
        void Enter();
        void Exit();
        void Update();
    }

    public class GameStateMachine
    {
        private readonly Dictionary<GameState, IGameState> _states;
        private IGameState _currentState;
        private GameState _previousStateType;
        
        public GameState CurrentStateType => _currentState?.StateType ?? GameState.FirstLoad;
        public GameState PreviousStateType => _previousStateType;
        
        public event Action<GameState, GameState> OnStateChanged;

        public GameStateMachine()
        {
            _states = new Dictionary<GameState, IGameState>();
        }

        #region Public Methods

        public void RegisterState(IGameState state)
        {
            if (state == null) return;
            
            _states[state.StateType] = state;
            Debug.Log($"[GameStateMachine] State registered: {state.StateType}");
        }

        public void ChangeState(GameState newStateType)
        {
            if (_currentState != null && _currentState.StateType == newStateType)
            {
                Debug.LogWarning($"[GameStateMachine] Already in state: {newStateType}");
                return;
            }

            if (!_states.TryGetValue(newStateType, out IGameState newState))
            {
                Debug.LogError($"[GameStateMachine] State not registered: {newStateType}");
                return;
            }

            _previousStateType = _currentState?.StateType ?? GameState.FirstLoad;
            
            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
            
            Debug.Log($"[GameStateMachine] State transition: {_previousStateType} -> {newStateType}");
            OnStateChanged?.Invoke(newStateType, _previousStateType);
        }

        public void Update()
        {
            _currentState?.Update();
        }

        #endregion
    }

    #region Concrete States

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

    public class MainMenuState : BaseGameState
    {
        public override GameState StateType => GameState.MainMenu;

        public MainMenuState(IUIService uiService, IGameControllerService gameController, IAudioService audioService) 
            : base(uiService, gameController, audioService) { }

        public override void Enter()
        {
            Debug.Log($"[{GetType().Name}] Entering MainMenu state");
            _uiService?.ShowMainMenu(true);
            _uiService?.ShowLoadingScreen(false);
            _uiService?.ShowGameUI(false);
            _uiService?.ShowGameOverScreen(false, false);
        }

        public override void Exit()
        {
            Debug.Log($"[{GetType().Name}] Exiting MainMenu state");
            _uiService?.ShowMainMenu(false);
        }
    }

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
            _uiService?.ShowLoadingScreen(true);
            _gameController?.StartNewGame();
            SimulateLoading();
        }

        public override void Exit()
        {
            Debug.Log($"[{GetType().Name}] Exiting LoadingGame state");
            _uiService?.ShowLoadingScreen(false);
        }

        private async void SimulateLoading()
        {
            for (int i = 0; i <= 10; i++)
            {
                _onProgress?.Invoke(i / 10f);
                await Cysharp.Threading.Tasks.UniTask.Delay(200);
            }
        }
    }

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

    public class GameEndedState : BaseGameState
    {
        public override GameState StateType => GameState.GameEnded;

        public GameEndedState(IUIService uiService, IGameControllerService gameController, IAudioService audioService) 
            : base(uiService, gameController, audioService) { }

        public override void Enter()
        {
            Debug.Log($"[{GetType().Name}] Entering GameEnded state");
            bool playerWon = UnityEngine.Random.value > 0.5f;
            _uiService?.ShowGameOverScreen(true, playerWon);
        }

        public override void Exit()
        {
            Debug.Log($"[{GetType().Name}] Exiting GameEnded state");
            _uiService?.ShowGameOverScreen(false, false);
        }
    }

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

    #endregion
}