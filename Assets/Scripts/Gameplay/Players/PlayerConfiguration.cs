using UnityEngine;
using CardWar.Core.Enums;

namespace Assets.Scripts.Player
{
    [System.Serializable]
    public class PlayerConfiguration
    {
        public int PlayerNumber { get; set; }
        public string PlayerName { get; set; }
        public bool IsLocalPlayer { get; set; }
        public Transform DeckTransform { get; set; }
        public Transform CardSlotTransform { get; set; }
        public Transform CardCountUITransform { get; set; }
        public PlayerPosition Position { get; set; }
        
        // Visual Settings
        public Color PlayerColor { get; set; } = Color.white;
        public float AnimationSpeed { get; set; } = 1f;
        
        // Optional prefab paths (if different from default)
        public string CustomDeckPrefabPath { get; set; }
        public string CustomCardSlotPrefabPath { get; set; }
        
        public PlayerConfiguration(int playerNumber, bool isLocal)
        {
            PlayerNumber = playerNumber;
            IsLocalPlayer = isLocal;
            PlayerName = isLocal ? "Player" : "Opponent";
            Position = isLocal ? PlayerPosition.Bottom : PlayerPosition.Top;
        }
    }
    

}