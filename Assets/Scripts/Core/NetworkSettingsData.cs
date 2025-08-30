using UnityEngine;

namespace CardWar.Core
{
    [CreateAssetMenu(fileName = "NetworkSettings", menuName = "CardWar/Network Settings")]
    public class NetworkSettingsData : ScriptableObject
    {
        [Header("Network Simulation")]
        [SerializeField] private bool _simulateNetwork = true;
        [SerializeField] private float _minDelay = 0.05f;
        [SerializeField] private float _maxDelay = 0.3f;
        [SerializeField] private float _packetLossChance = 0.01f;
        [SerializeField] private float _timeoutDuration = 5f;
        
        [Header("Debug Settings")]
        [SerializeField] private bool _logNetworkEvents = false;
        
        public bool SimulateNetwork => _simulateNetwork;
        public float MinDelay => _minDelay;
        public float MaxDelay => _maxDelay;
        public float PacketLossChance => _packetLossChance;
        public float TimeoutDuration => _timeoutDuration;
        public bool LogNetworkEvents => _logNetworkEvents;
        
        public float GetRandomDelay()
        {
            return Random.Range(_minDelay, _maxDelay);
        }
        
        public bool ShouldDropPacket()
        {
            return Random.value < _packetLossChance;
        }
    }
}