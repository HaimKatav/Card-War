using UnityEngine;
using CardWar.Services.State;

namespace CardWar.Services
{
    public class ServiceBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var appStateManager = gameObject.AddComponent<AppStateManager>();
            var gameStateManager = gameObject.AddComponent<GameStateManager>();
            var bridge = gameObject.AddComponent<StateManagementBridge>();

            Debug.Log($"[{GetType().Name}] State services registered");
        }
    }
}
