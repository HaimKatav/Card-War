using System;
using UnityEngine;

namespace CardWar.Services.Network
{
    [Serializable]
    public class NetworkErrorConfig
    {
        [Header("Error Rates (0-1)")]
        [Range(0f, 1f)] public float timeoutRate = 0.02f;
        [Range(0f, 1f)] public float networkErrorRate = 0.05f;
        [Range(0f, 1f)] public float serverErrorRate = 0.01f;
        [Range(0f, 1f)] public float corruptionRate = 0.005f;
        
        [Header("Network Delays")]
        public float minNetworkDelay = 0.1f;
        public float maxNetworkDelay = 0.5f;
        public float timeoutDuration = 5.0f;
        public float retryBaseDelay = 1.0f;
        
        [Header("Error Messages")]
        public string[] networkErrorMessages = {
            "Connection timeout",
            "Network unreachable",
            "Connection lost",
            "DNS resolution failed"
        };
        
        public string[] serverErrorMessages = {
            "Internal server error",
            "Service unavailable",
            "Database connection failed",
            "Rate limit exceeded"
        };
    }
    
    public enum NetworkErrorType
    {
        None,
        Timeout,
        NetworkError,
        ServerError,
        DataCorruption,
        Retry
    }
}