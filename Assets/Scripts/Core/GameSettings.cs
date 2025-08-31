using UnityEngine;

namespace CardWar.Core
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "CardWar/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        // ASSET PATHS
        public static readonly string UI_MANAGER_ASSET_PATH = "Prefabs/GameCanvas";
        public static readonly string PLAY_AREA_ASSET_PATH = "Prefabs/PlayArea";
        public static readonly string CARD_BACK_SPRITE_ASSET_PATH = "GameplaySprites/Cards/card_back";
        public static readonly string CARD_SPRITE_ASSET_PATH = "GameplaySprites/Cards";
        public static readonly string CARD_PREFAB_ASSET_PATH = "Prefabs/CardPrefab";
        public static readonly string AUDIO_ASSETS_PATH = "Audio";
        
        [Header("Network Simulation")]
        [SerializeField] private float _fakeNetworkDelay = 0.1f;
        [SerializeField] private float _fakeNetworkErrorRate = 0.01f;
        
        public float FakeNetworkDelay => _fakeNetworkDelay;
        public float FakeNetworkErrorRate => _fakeNetworkErrorRate;
    }
}