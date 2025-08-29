using UnityEngine;
using System.Collections.Generic;

namespace CardWar.Configuration
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "CardWar/Game Settings", order = 1)]
    public class GameSettings : ScriptableObject
    {
        public static readonly string CARD_PREFAB_NAME = "CardPrefab";
        
        [Header("═══════════ ASSET PATHS ═══════════")]
        
        [Header("Card Assets")]
        [Tooltip("Path to card sprites in Resources folder")]
        public string cardSpritesPath = "GameplaySprites/Cards";
        
        [Tooltip("Name of the card back sprite")]
        public string cardBackSpriteName = "card_back";
        
        [Tooltip("Card naming format: {rank}_{suit}")]
        public string cardSpriteFormat = "{rank}_{suit}";
        
        [Header("UI Assets")]
        public string uiSpritesPath = "GameplaySprites/UI";
        public string backgroundSpritesPath = "GameplaySprites/Backgrounds";
        public string buttonSpritesPath = "GameplaySprites/UI/Buttons";
        public string iconSpritesPath = "GameplaySprites/UI/Icons";
        
        [Header("Audio Assets")]
        public string sfxPath = "Audio/SFX";
        public string musicPath = "Audio/Music";
        public string cardSoundPath = "Audio/SFX/Cards";
        
        [Header("Prefab Paths")]
        public string prefabsPath = "Prefabs";
        public string cardPrefabPath = "Prefabs/CardPrefab";
        public string uiPrefabPath = "Prefabs/UI";
        public string effectsPrefabPath = "Prefabs/Effects";
        
        [Header("═══════════ SCENE SETUP ═══════════")]
        
        [Header("Canvas Configuration")]
        public Vector2 canvasReferenceResolution = new Vector2(1080, 1920);
        [Range(0f, 1f)]
        public float canvasMatchWidthOrHeight = 0.5f;
        public RenderMode canvasRenderMode = RenderMode.ScreenSpaceOverlay;
        
        [Header("Scene Settings")]
        public string mainSceneName = "GameScene";
        public string menuSceneName = "MenuScene";
        public float sceneTransitionDuration = 0.5f;
        
        [Header("═══════════ GAMEPLAY ═══════════")]
        
        [Header("Animation Timings")]
        [Range(0.05f, 1f)]
        public float dealDelay = 0.1f;
        
        [Range(0.2f, 2f)]
        public float cardAnimationDuration = 0.5f;
        
        [Range(0.2f, 1f)]
        public float cardFlipDuration = 0.3f;
        
        [Range(1f, 5f)]
        public float warAnimationDuration = 2f;
        
        [Range(0.5f, 3f)]
        public float resultPauseDuration = 1.5f;
        
        [Header("Card Positions (Screen Space)")]
        public Vector3 playerCardOffset = new Vector3(0, -200, 0);
        public Vector3 opponentCardOffset = new Vector3(0, 200, 0);
        public Vector3 deckOffset = new Vector3(-400, 0, 0);
        public Vector3 warPileOffset = new Vector3(0, 0, 0);
        
        [Header("Game Rules")]
        public int startingCardCount = 26;
        public int warCardsPerRound = 3;
        public int maxWarRounds = 10;
        public bool autoPlayEnabled = false;
        public float autoPlayDelay = 2f;
        
        [Header("═══════════ NETWORK SIMULATION ═══════════")]
        
        [Header("Network Settings")]
        [Range(0f, 1f)]
        public float networkErrorRate = 0.1f;
        
        [Range(0f, 2f)]
        public float minNetworkDelay = 0.1f;
        
        [Range(0.5f, 5f)]
        public float maxNetworkDelay = 1f;
        
        [Range(1f, 10f)]
        public float networkTimeoutDuration = 5f;
        
        [Range(1, 10)]
        public int maxNetworkRetries = 3;
        
        [Header("═══════════ UI STYLING ═══════════")]
        
        [Header("Color Scheme")]
        [ColorUsage(true, true)]
        public Color playerColor = new Color(0.2f, 0.6f, 1f, 1f);
        
        [ColorUsage(true, true)]
        public Color opponentColor = new Color(1f, 0.3f, 0.2f, 1f);
        
        [ColorUsage(true, true)]
        public Color warColor = new Color(1f, 0.8f, 0.1f, 1f);
        
        [ColorUsage(true, true)]
        public Color backgroundColor = new Color(0.15f, 0.5f, 0.25f, 1f);
        
        [ColorUsage(true, true)]
        public Color cardHighlightColor = new Color(1f, 1f, 0.5f, 1f);
        
        [Header("UI Sizes")]
        [Range(12, 72)]
        public int scoreTextSize = 36;
        
        [Range(12, 48)]
        public int roundTextSize = 24;
        
        [Range(24, 96)]
        public int warTextSize = 72;
        
        [Range(12, 48)]
        public int buttonTextSize = 32;
        
        [Header("Card Visual Settings")]
        public Vector2 cardSize = new Vector2(120, 180);
        public float cardCornerRadius = 8f;
        public float cardShadowDistance = 4f;
        public Color cardShadowColor = new Color(0, 0, 0, 0.5f);
        
        [Header("═══════════ POOLING ═══════════")]
        
        [Header("Object Pools")]
        [Range(10, 100)]
        public int cardPoolInitialSize = 20;
        
        [Range(20, 200)]
        public int cardPoolMaxSize = 52;
        
        public bool poolGrowthEnabled = true;
        public int poolGrowthIncrement = 10;
        
        [Header("═══════════ AUDIO SETTINGS ═══════════")]
        
        [Header("Volume Defaults")]
        [Range(0f, 1f)]
        public float defaultMasterVolume = 1f;
        
        [Range(0f, 1f)]
        public float defaultSFXVolume = 0.8f;
        
        [Range(0f, 1f)]
        public float defaultMusicVolume = 0.5f;
        
        [Header("Audio Clips")]
        public string cardFlipSound = "card_flip";
        public string cardPlaceSound = "card_place";
        public string warSound = "war_horn";
        public string victorySound = "victory";
        public string defeatSound = "defeat";
        public string buttonClickSound = "button_click";
        
        [Header("═══════════ DEVELOPMENT ═══════════")]
        
        [Header("Debug Options")]
        public bool enableDebugLogs = true;
        public bool enableAssetValidation = true;
        public bool autoCreateMissingFolders = true;
        public bool showDebugUI = false;
        public bool skipIntroAnimation = false;
        
        [Header("Testing")]
        public bool forcePlayerWin = false;
        public bool forceWarScenarios = false;
        public bool instantAnimations = false;
        public bool unlimitedCards = false;
        
        [Header("═══════════ PLATFORM SPECIFIC ═══════════")]
        
        [Header("Mobile Settings")]
        public bool enableHapticFeedback = true;
        public bool enableNotchHandling = true;
        public float safeAreaPadding = 20f;
        
        [Header("Performance")]
        [Range(30, 120)]
        public int targetFrameRate = 60;
        public bool enableBatching = true;
        public bool enableOcclusionCulling = false;
        
        private static GameSettings _instance;
        public static GameSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameSettings>("GameSettings");
                    if (_instance == null)
                    {
                        _instance = Resources.Load<GameSettings>("Settings/GameSettings");
                        if (_instance == null)
                        {
                            Debug.LogError("[GameSettings] No GameSettings asset found in Resources!");
                        }
                    }
                }
                return _instance;
            }
        }
        
        public string GetCardSpriteName(string rank, string suit)
        {
            return cardSpriteFormat
                .Replace("{rank}", rank.ToLower())
                .Replace("{suit}", suit.ToLower());
        }
        
        public string GetCardPrefabResourcePath()
        {
            return cardPrefabPath;
        }
        
        public void ValidateAssetPaths()
        {
            if (!enableAssetValidation) return;
            
            var paths = new List<string> 
            { 
                cardSpritesPath, 
                uiSpritesPath, 
                sfxPath, 
                musicPath,
                prefabsPath
            };
            
            foreach (var path in paths)
            {
                if (Resources.Load<Object>(path) == null)
                {
                    Debug.LogWarning($"[GameSettings] Path not found in Resources: {path}");
                }
            }
        }
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            minNetworkDelay = Mathf.Min(minNetworkDelay, maxNetworkDelay);
            cardPoolMaxSize = Mathf.Max(cardPoolInitialSize, cardPoolMaxSize);
            targetFrameRate = Mathf.Clamp(targetFrameRate, 30, 120);
        }
        #endif
    }
}