using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;
using Assets.Scripts.Services.AssetManagement;
using CardWar.Core.Enums;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Factory for creating player controllers
    /// </summary>
    public class PlayerControllerFactory : IDisposable
    {
        private readonly DiContainer _container;
        private readonly IAssetManager _assetManager;
        private readonly Transform _playersParent;
        
        private const string LOCAL_PLAYER_PREFAB = "Prefabs/Player/LocalPlayer";
        private const string AI_PLAYER_PREFAB = "Prefabs/Player/AIPlayer";
        
        [Inject]
        public PlayerControllerFactory(
            DiContainer container,
            IAssetManager assetManager,
            [Inject(Id = "PlayersParent")] Transform playersParent = null)
        {
            _container = container;
            _assetManager = assetManager;
            _playersParent = playersParent;
        }
        
        public async UniTask<IPlayerController> CreatePlayerAsync(
            PlayerConfiguration config,
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            Debug.Log($"[PlayerControllerFactory] Creating {(config.IsLocalPlayer ? "Local" : "AI")} player");
            
            try
            {
                string prefabPath = config.IsLocalPlayer ? LOCAL_PLAYER_PREFAB : AI_PLAYER_PREFAB;
                
                // Try to load player prefab, fallback to creating GameObject if prefab doesn't exist
                GameObject playerGO;
                
                try
                {
                    playerGO = await _assetManager.InstantiateAsync(
                        prefabPath,
                        _playersParent,
                        cancellationToken);
                }
                catch (AssetLoadException)
                {
                    // Fallback: Create player GameObject manually if prefab doesn't exist
                    Debug.LogWarning($"[PlayerControllerFactory] Prefab not found at {prefabPath}, creating manually");
                    playerGO = CreatePlayerGameObject(config);
                }
                
                // Add appropriate controller component
                PlayerController controller;
                
                if (config.IsLocalPlayer)
                {
                    controller = playerGO.GetComponent<LocalPlayerController>();
                    if (controller == null)
                    {
                        controller = playerGO.AddComponent<LocalPlayerController>();
                    }
                }
                else
                {
                    controller = playerGO.GetComponent<AIPlayerController>();
                    if (controller == null)
                    {
                        controller = playerGO.AddComponent<AIPlayerController>();
                    }
                }
                
                // Inject dependencies
                _container.Inject(controller);
                
                // Initialize with configuration
                await controller.InitializeAsync(config);
                
                Debug.Log($"[PlayerControllerFactory] Successfully created {config.PlayerName}");
                
                return controller;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerControllerFactory] Failed to create player: {ex.Message}");
                throw;
            }
        }
        
        public async UniTask<(IPlayerController localPlayer, IPlayerController aiPlayer)> 
            CreateBothPlayersAsync(
                Transform playerArea,
                Transform opponentArea,
                System.Threading.CancellationToken cancellationToken = default)
        {
            // Create configurations
            var localConfig = new PlayerConfiguration(1, true)
            {
                PlayerName = "Player",
                DeckTransform = playerArea,
                CardSlotTransform = playerArea,
                Position = PlayerPosition.Bottom
            };
            
            var aiConfig = new PlayerConfiguration(2, false)
            {
                PlayerName = "Opponent",
                DeckTransform = opponentArea,
                CardSlotTransform = opponentArea,
                Position = PlayerPosition.Top
            };
            
            // Create players in parallel
            var localPlayerTask = CreatePlayerAsync(localConfig, cancellationToken);
            var aiPlayerTask = CreatePlayerAsync(aiConfig, cancellationToken);
            
            await UniTask.WhenAll(localPlayerTask, aiPlayerTask);
            
            return (localPlayerTask.GetAwaiter().GetResult(), 
                    aiPlayerTask.GetAwaiter().GetResult());
        }
        
        private GameObject CreatePlayerGameObject(PlayerConfiguration config)
        {
            var playerGO = new GameObject($"Player_{config.PlayerNumber}_{config.PlayerName}");
            
            if (_playersParent != null)
            {
                playerGO.transform.SetParent(_playersParent);
            }
            
            playerGO.transform.localPosition = Vector3.zero;
            playerGO.transform.localRotation = Quaternion.identity;
            playerGO.transform.localScale = Vector3.one;
            
            return playerGO;
        }
        
        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}