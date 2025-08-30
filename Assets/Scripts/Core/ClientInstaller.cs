using UnityEngine;
using Zenject;
using CardWar.Services;
using CardWar.Managers;
using CardWar.Core;

namespace CardWar.Core
{
    public class ClientInstaller : MonoInstaller
    {
        [SerializeField] private GameSettings _gameSettings;
        [SerializeField] private NetworkSettingsData _networkSettings;
        
        public override void InstallBindings()
        {
            Debug.Log($"[ClientInstaller] Starting bindings installation");
            
            BindSettings();
            BindServices();
            BindManagers();
            
            Debug.Log($"[ClientInstaller] Bindings installation complete");
        }
        
        private void BindSettings()
        {
            if (_gameSettings != null)
            {
                Container.BindInstance(_gameSettings).AsSingle();
                Debug.Log($"[ClientInstaller] GameSettings bound");
            }
            
            if (_networkSettings != null)
            {
                Container.BindInstance(_networkSettings).AsSingle();
                Debug.Log($"[ClientInstaller] NetworkSettings bound");
            }
        }
        
        private void BindServices()
        {
            Container.Bind<IDIService>().To<SimpleDIService>().AsSingle();
            Debug.Log($"[ClientInstaller] IDIService bound");
        }
        
        private void BindManagers()
        {
            Container.Bind<IAssetService>().To<AssetManager>().FromNewComponentOnNewGameObject().AsSingle();
            Debug.Log($"[ClientInstaller] AssetManager bound");
            
            Container.Bind<IAudioService>().To<AudioManager>().FromNewComponentOnNewGameObject().AsSingle();
            Debug.Log($"[ClientInstaller] AudioManager bound");
            
            Container.Bind<IGameStateService>().To<GameManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            Debug.Log($"[ClientInstaller] GameManager bound - this will trigger initialization");
        }
    }
}