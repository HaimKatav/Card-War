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
        public static readonly string CARD_SPRITE_ASSET_PATH = "GameplaySprites/Cards/";
        public static readonly string CARD_PREFAB_ASSET_PATH = "Prefabs/CardPrefab";
        
        [Header("Asset Paths")]
        [SerializeField] private string _uiManagerAssetPath = "Assets/Resources/Prefabs/GameCanvas";
        [SerializeField] private string _cardSpritesPath = "GameplaySprites/Cards";
        [SerializeField] private string _audioClipsPath = "Audio";
        
        [Header("Game Configuration")]
        [SerializeField] private int _cardsPerPlayer = 26;
        [SerializeField] private int _warCardCount = 3;
        [SerializeField] private float _cardFlipDelay = 0.5f;
        [SerializeField] private float _cardMoveSpeed = 1f;
        
        [Header("Network Simulation")]
        [SerializeField] private float _fakeNetworkDelay = 0.1f;
        [SerializeField] private float _fakeNetworkErrorRate = 0.01f;
        
        public string UIManagerAssetPath => _uiManagerAssetPath;
        public string CardSpritesPath => _cardSpritesPath;
        public string AudioClipsPath => _audioClipsPath;
        
        public int CardsPerPlayer => _cardsPerPlayer;
        public int WarCardCount => _warCardCount;
        public float CardFlipDelay => _cardFlipDelay;
        public float CardMoveSpeed => _cardMoveSpeed;
        
        public float FakeNetworkDelay => _fakeNetworkDelay;
        public float FakeNetworkErrorRate => _fakeNetworkErrorRate;
    }
}