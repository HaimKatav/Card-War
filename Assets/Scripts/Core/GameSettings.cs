using UnityEngine;
using System.Collections.Generic;

namespace CardWar.Configuration
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "CardWar/Game Settings", order = 1)]
    public class GameSettings : ScriptableObject
    {
        [Header("Asset Paths Configuration")]
        [Space(10)]
        
        [Header("Card Assets")]
        public string cardSpritesPath = "GameplaySprites/Cards";
        public string cardBackSpriteName = "card_back";
        public string cardSpriteFormat = "{rank}_{suit}"; // e.g., "ace_spades"
        
        [Header("UI Assets")]
        public string uiSpritesPath = "GameplaySprites/UI";
        public string backgroundSpritesPath = "GameplaySprites/Backgrounds";
        public string buttonSpritesPath = "GameplaySprites/UI/Buttons";
        
        [Header("Audio Assets")]
        public string sfxPath = "Audio/SFX";
        public string musicPath = "Audio/Music";
        
        [Header("Prefab Paths")]
        public string prefabsPath = "Prefabs";
        public string cardPrefabPath = "Prefabs/Cards";
        public string uiPrefabPath = "Prefabs/UI";
        
        [Header("Scene Setup")]
        public string sceneName = "GameScene";
        public Vector2 canvasReferenceResolution = new Vector2(1080, 1920);
        public float canvasMatchWidthOrHeight = 0.5f;
        
        [Header("Gameplay Configuration")]
        public float dealDelay = 0.1f;
        public float cardAnimationDuration = 0.5f;
        public float warAnimationDuration = 2f;
        public int cardPoolInitialSize = 20;
        
        [Header("Card Positions")]
        public Vector3 playerCardPosition = new Vector3(0, -400, 0);
        public Vector3 opponentCardPosition = new Vector3(0, 400, 0);
        public Vector3 deckPosition = new Vector3(-400, 0, 0);
        public Vector3 warPilePosition = new Vector3(0, 0, 0);
        
        [Header("UI Colors & Styling")]
        public Color playerColor = new Color(0.2f, 0.6f, 1f, 1f);
        public Color opponentColor = new Color(1f, 0.3f, 0.2f, 1f);
        public Color warColor = new Color(1f, 0.8f, 0.1f, 1f);
        public Color backgroundColor = new Color(0.1f, 0.5f, 0.3f, 1f);
        
        [Header("Development")]
        public bool enableDebugLogs = true;
        public bool enableAssetValidation = true;
        public bool autoCreateMissingFolders = true;
        
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
                        Debug.LogError("[GameSettings] No GameSettings asset found in Resources folder. Please create one using: Assets -> Create -> CardWar -> Game Settings");
                    }
                }
                return _instance;
            }
        }
        
        public string GetCardSpriteName(string rank, string suit)
        {
            return cardSpriteFormat.Replace("{rank}", rank.ToLower()).Replace("{suit}", suit.ToLower());
        }
        
        public string GetFullAssetPath(string relativePath)
        {
            return $"Assets/Resources/{relativePath}";
        }
        
        [ContextMenu("Validate Asset Paths")]
        public bool ValidateAssetPaths()
        {
            var issues = new List<string>();
            
            // Check if critical folders exist
            if (!System.IO.Directory.Exists(GetFullAssetPath(cardSpritesPath)))
                issues.Add($"Missing card sprites folder: {GetFullAssetPath(cardSpritesPath)}");
                
            if (!System.IO.Directory.Exists(GetFullAssetPath(prefabsPath)))
                issues.Add($"Missing prefabs folder: {GetFullAssetPath(prefabsPath)}");
            
            if (issues.Count > 0)
            {
                Debug.LogWarning($"[GameSettings] Asset path validation found {issues.Count} issues:\n{string.Join("\n", issues)}");
                return false;
            }
            
            return true;
        }
        
        [ContextMenu("Validate Asset Paths")]
        public void ValidateInEditor()
        {
            ValidateAssetPaths();
        }
        
        [ContextMenu("Create Missing Folders")]
        public void CreateMissingFolders()
        {
#if UNITY_EDITOR
            var foldersToCreate = new[]
            {
                cardSpritesPath,
                uiSpritesPath,
                backgroundSpritesPath,
                buttonSpritesPath,
                sfxPath,
                musicPath,
                prefabsPath,
                cardPrefabPath,
                uiPrefabPath
            };
            
            foreach (var folder in foldersToCreate)
            {
                var fullPath = GetFullAssetPath(folder);
                if (!System.IO.Directory.Exists(fullPath))
                {
                    System.IO.Directory.CreateDirectory(fullPath);
                    Debug.Log($"[GameSettings] Created folder: {fullPath}");
                }
            }
            
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}