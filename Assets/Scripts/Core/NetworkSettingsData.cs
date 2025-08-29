using UnityEngine;

namespace CardWar.Core
{
    [CreateAssetMenu(fileName = "NetworkSettings", menuName = "CardWar/NetworkSettings")]
    public class NetworkSettingsData : ScriptableObject
    {
        [Header("Network Simulation")]
        public float MinDelay = 0.1f;
        public float MaxDelay = 2.0f;
        
        [Header("Error Simulation")]
        [Range(0f, 1f)]
        public float TimeoutChance = 0.05f;
        [Range(0f, 1f)]
        public float ErrorChance = 0.02f;
        public float TimeoutDuration = 5.0f;
        
        [Header("Debug")]
        public bool EnableNetworkSimulation = true;
        public bool LogNetworkEvents = false;
        
        public float GetRandomDelay()
        {
            if (!EnableNetworkSimulation) return 0f;
            return Random.Range(MinDelay, MaxDelay);
        }
        
        public bool ShouldSimulateTimeout()
        {
            if (!EnableNetworkSimulation) return false;
            return Random.value < TimeoutChance;
        }
        
        public bool ShouldSimulateError()
        {
            if (!EnableNetworkSimulation) return false;
            return Random.value < ErrorChance;
        }
    }
}