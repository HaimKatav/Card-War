using UnityEngine;

namespace CardWar.Core
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "CardWar/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Game Configuration")]
        public int TotalCardsInDeck = 52;
        public int CardsPerPlayer = 26;
        public int WarCardsToPlace = 3;
        
        [Header("Animation Timings")]
        public float CardFlipDelay = 0.5f;
        public float CardMoveSpeed = 0.8f;
        public float WarRevealDelay = 2.0f;
        public float RoundEndDelay = 1.0f;
        public float CollectAnimationDuration = 0.6f;
        
        [Header("UI Settings")]
        public float LoadingScreenMinDuration = 2.0f;
        public float LoadingProgressSmoothSpeed = 2.0f;
        
        [Header("Gameplay")]
        public bool AutoPlayEnabled = false;
        public float AutoPlayDelay = 1.5f;
        public bool InstantWarResolution = false;
    }
}