using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;
using CardWar.Core.Events;
using CardWar.Services.Game;
using CardWar.Services.Network;
using CardWar.Services.UI;

namespace CardWar.Controllers.Game
{
    public class GameManager : MonoBehaviour, IInitializable
    {
        // Injected services (non-MonoBehaviour)
        private IGameService _gameService;
        private IAnimationService _animationService;
        private IFakeServerService _serverService;

        // Injected managers (MonoBehaviour scene objects)
        private UIManager _uiManager;

        // Communication
        private SignalBus _signalBus;

        [Inject]
        public void Construct(
            IGameService gameService,
            IAnimationService animationService,
            IFakeServerService serverService,
            UIManager uiManager,
            SignalBus signalBus)
        {
            _gameService = gameService;
            _animationService = animationService;
            _serverService = serverService;
            _uiManager = uiManager;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            Debug.Log("RefactoredGameManager: Starting game initialization...");

            // Initialize systems in order
            InitializeServices();
            InitializeManagers();
            SetupEventHandlers();
            StartGameFlow().Forget(); // Fire and forget for async operation

            Debug.Log("RefactoredGameManager: Initialization complete!");
        }

        private void InitializeServices()
        {
            Debug.Log("GameManager: Initializing services...");

            // Services are automatically created by Zenject and implement IInitializable if needed
            // GameService implements IInitializable so it will be initialized by Zenject automatically
            // We just log here for visibility

            Debug.Log("GameManager: Services initialized");
        }

        private void InitializeManagers()
        {
            Debug.Log("GameManager: Initializing scene managers...");

            // Scene managers are already created and exist in the scene
            // Their Construct methods will be called by Zenject
            // We can perform additional setup here if needed

            Debug.Log("GameManager: Scene managers initialized");
        }

        private void SetupEventHandlers()
        {
            Debug.Log("GameManager: Setting up event handlers...");

            // Subscribe to game-level events
            _signalBus.Subscribe<StartGameEvent>(OnStartGameRequested);
            _signalBus.Subscribe<GameEndedEvent>(OnGameEnded);

            Debug.Log("GameManager: Event handlers setup complete");
        }

        private async UniTask StartGameFlow()
        {
            Debug.Log("GameManager: Starting game flow...");
            
            await _uiManager.Initialize();
            
            Debug.Log("GameManager: Game flow started - showing main menu");
        }

        private void OnStartGameRequested()
        {
            Debug.Log("GameManager: Start game requested - delegating to GameService");
            
            // GameManager coordinates but delegates actual game logic to GameService
            _gameService.StartNewGame();
        }

        private void OnGameEnded(GameEndedEvent @event)
        {
            Debug.Log($"GameManager: Game ended - {@event.resultData.WinnerName} won");
            
            // Handle game end at the manager level
            // Could save statistics, show ads, etc.
        }

        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (_signalBus != null)
            {
                _signalBus.TryUnsubscribe<StartGameEvent>(OnStartGameRequested);
                _signalBus.TryUnsubscribe<GameEndedEvent>(OnGameEnded);
            }
            
            Debug.Log("GameManager: Cleanup complete");
        }
    }
}