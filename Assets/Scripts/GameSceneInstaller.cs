using UnityEngine;
using Zenject;

public class GameSceneInstaller : MonoInstaller
{
    [Header("Settings")]
    [SerializeField] private float _defaultAnimationDuration = 0.5f;

    public override void InstallBindings()
    {
        Debug.Log("GameSceneInstaller: Starting installation...");

        SignalBusInstaller.Install(Container);
        
        DeclareSignals();

        BindSceneManagers();

        BindServices();

        BindGameManager();

        Debug.Log("GameSceneInstaller: Installation completed successfully!");
    }

    private void DeclareSignals()
    {
        Container.DeclareSignal<StartGameSignal>();
        Container.DeclareSignal<RoundStartedSignal>();
        Container.DeclareSignal<RoundCompletedSignal>();
        Container.DeclareSignal<GameEndedSignal>();
        Container.DeclareSignal<NetworkErrorSignal>();
        Container.DeclareSignal<GameUIControllerReadySignal>();
    }

    private void BindSceneManagers()
    {
        // Find existing managers in scene
        UIManager uiManager = FindObjectOfType<UIManager>();

        if (uiManager == null)
        {
            Debug.LogError("UIManager not found in scene! Please add it manually.");
            return;
        }

        Container.Bind<UIManager>().FromInstance(uiManager).AsSingle();
        Container.Bind<IUIService>().FromInstance(uiManager).AsSingle();

        Debug.Log("Scene managers bound successfully");
    }

    private void BindServices()
    {
        Container.Bind<IGameService>()
            .To<GameService>()
            .AsSingle()
            .NonLazy(); // Created immediately

        Container.Bind<IAnimationService>()
            .To<AnimationService>()
            .AsSingle()
            .WithArguments(_defaultAnimationDuration);

        Container.Bind<IAssetService>()
            .To<AssetService>()
            .AsSingle()
            .NonLazy();

        Container.Bind<IFakeServerService>()
            .To<FakeWarServer>()
            .AsSingle()
            .NonLazy();

        Debug.Log("Services bound successfully");
    }

    private void BindGameManager()
    {
        // Create GameManager and bind as both concrete type and interfaces
        Container.Bind<GameManager>()
            .FromNewComponentOnNewGameObject()
            .AsSingle()
            .NonLazy();

        // Bind GameManager interfaces
        Container.Bind<IInitializable>()
            .To<GameManager>()
            .FromResolve(); // Use the same instance that was created above

        Debug.Log("GameManager bound successfully");
    }
}