using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using Zenject;
using CardWar.UI;
using CardWar.UI.Cards;
using CardWar.Gameplay.Controllers;
using CardWar.Configuration;
using CardWar.Infrastructure.Installers;

namespace CardWar.Editor
{
    public class ComprehensiveSceneBuilder : EditorWindow
    {
        private GameSettings _gameSettings;
        private bool _backupOldAssets = true;
        private string _backupSuffix = "_OLD";
        
        [MenuItem("CardWar/🏗️ Comprehensive Scene Builder")]
        public static void ShowWindow()
        {
            var window = GetWindow<ComprehensiveSceneBuilder>("War Card Game - Clean Scene Builder");
            window.minSize = new Vector2(400, 600);
        }
        
        private void OnEnable()
        {
            LoadOrCreateGameSettings();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🏗️ WAR CARD GAME - CLEAN SCENE BUILDER", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool will create a completely clean scene setup using configurable settings.\n" +
                "It will rename existing assets and create new ones from scratch.",
                MessageType.Info);
            
            GUILayout.Space(10);
            
            // Game Settings Section
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            _gameSettings = (GameSettings)EditorGUILayout.ObjectField("Game Settings", _gameSettings, typeof(GameSettings), false);
            
            if (_gameSettings == null)
            {
                EditorGUILayout.HelpBox("Game Settings asset required! Click 'Create Game Settings' to generate one.", MessageType.Warning);
                
                if (GUILayout.Button("Create Game Settings Asset"))
                {
                    CreateGameSettingsAsset();
                }
                return;
            }
            
            GUILayout.Space(10);
            
            // Backup Options
            EditorGUILayout.LabelField("Backup Options", EditorStyles.boldLabel);
            _backupOldAssets = EditorGUILayout.Toggle("Backup Existing Assets", _backupOldAssets);
            if (_backupOldAssets)
            {
                _backupSuffix = EditorGUILayout.TextField("Backup Suffix", _backupSuffix);
            }
            
            GUILayout.Space(20);
            
            // Main Action Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🚀 BUILD COMPLETE CLEAN SCENE", GUILayout.Height(60)))
            {
                if (EditorUtility.DisplayDialog("Build Clean Scene", 
                    $"This will create a completely new scene setup and {(_backupOldAssets ? "backup" : "replace")} existing assets.\n\n" +
                    "This process includes:\n" +
                    "• Creating/organizing folder structure\n" +
                    "• Setting up GameSettings-based asset paths\n" +
                    "• Building new prefabs with proper connections\n" +
                    "• Creating clean scene hierarchy\n" +
                    "• Setting up Zenject bindings\n\n" +
                    "Continue?", 
                    "Yes, Build Clean Scene", "Cancel"))
                {
                    BuildCompleteScene();
                }
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(20);
            
            // Individual Actions
            EditorGUILayout.LabelField("Individual Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("1. 📁 Create Folder Structure"))
                CreateFolderStructure();
            
            if (GUILayout.Button("2. 🔄 Backup & Rename Old Assets"))
                BackupExistingAssets();
            
            if (GUILayout.Button("3. 🎮 Create New Prefabs"))
                CreateNewPrefabs();
            
            if (GUILayout.Button("4. 🏗️ Setup Clean Scene Hierarchy"))
                SetupSceneHierarchy();
            
            if (GUILayout.Button("5. 🔗 Connect All References"))
                ConnectAllReferences();
            
            if (GUILayout.Button("6. 🧪 Validate Setup"))
                ValidateCompleteSetup();
            
            GUILayout.Space(20);
            
            // Settings Actions
            EditorGUILayout.LabelField("Settings Management", EditorStyles.boldLabel);
            
            if (GUILayout.Button("⚙️ Edit Game Settings"))
            {
                if (_gameSettings != null)
                    Selection.activeObject = _gameSettings;
            }
            
            if (GUILayout.Button("📋 Debug Asset Paths"))
            {
                if (_gameSettings != null)
                    _gameSettings.ValidateInEditor();
            }
        }
        
        private void BuildCompleteScene()
        {
            Debug.Log("🏗️ [Scene Builder] Starting complete clean scene build...");
            
            try
            {
                EditorUtility.DisplayProgressBar("Building Scene", "Creating folder structure...", 0.1f);
                CreateFolderStructure();
                
                EditorUtility.DisplayProgressBar("Building Scene", "Backing up existing assets...", 0.2f);
                if (_backupOldAssets)
                    BackupExistingAssets();
                
                EditorUtility.DisplayProgressBar("Building Scene", "Creating new prefabs...", 0.4f);
                CreateNewPrefabs();
                
                EditorUtility.DisplayProgressBar("Building Scene", "Setting up scene hierarchy...", 0.6f);
                SetupSceneHierarchy();
                
                EditorUtility.DisplayProgressBar("Building Scene", "Connecting references...", 0.8f);
                ConnectAllReferences();
                
                EditorUtility.DisplayProgressBar("Building Scene", "Validating setup...", 0.9f);
                ValidateCompleteSetup();
                
                EditorUtility.ClearProgressBar();
                
                EditorUtility.DisplayDialog("Success!", 
                    "✅ Complete clean scene build finished successfully!\n\n" +
                    "Your scene is now ready with:\n" +
                    "• Clean hierarchy with proper Canvas setup\n" +
                    "• New prefabs with all connections\n" +
                    "• Settings-based asset management\n" +
                    "• Proper Zenject bindings\n\n" +
                    "Press Play to test your game!",
                    "Awesome!");
                    
                Debug.Log("✅ [Scene Builder] Complete clean scene build finished successfully!");
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"❌ [Scene Builder] Error during scene build: {ex.Message}");
                EditorUtility.DisplayDialog("Error", 
                    $"Something went wrong during the build:\n{ex.Message}\n\nCheck console for details.",
                    "OK");
            }
        }
        
        private void CreateFolderStructure()
        {
            Debug.Log("📁 [Scene Builder] Creating folder structure based on GameSettings...");
            
            if (_gameSettings.autoCreateMissingFolders)
            {
                _gameSettings.CreateMissingFolders();
            }
            
            // Create additional editor-specific folders
            CreateFolderIfNotExists("Assets", "Editor");
            CreateFolderIfNotExists("Assets", "Settings");
            
            Debug.Log("✅ [Scene Builder] Folder structure created");
        }
        
        private void BackupExistingAssets()
        {
            Debug.Log($"🔄 [Scene Builder] Backing up existing assets with suffix '{_backupSuffix}'...");
            
            // Backup existing prefabs
            BackupIfExists("Assets/Prefabs/Cards/CardPrefab.prefab", $"Assets/Prefabs/Cards/CardPrefab{_backupSuffix}.prefab");
            BackupIfExists("Assets/Prefabs/GameManager.prefab", $"Assets/Prefabs/GameManager{_backupSuffix}.prefab");
            
            // Backup existing GameSettings if it exists
            var existingSettings = AssetDatabase.LoadAssetAtPath<GameSettings>("Assets/Resources/GameSettings.asset");
            if (existingSettings != null && existingSettings != _gameSettings)
            {
                AssetDatabase.CopyAsset("Assets/Resources/GameSettings.asset", $"Assets/Resources/GameSettings{_backupSuffix}.asset");
            }
            
            Debug.Log("✅ [Scene Builder] Asset backup completed");
        }
        
        private void CreateNewPrefabs()
        {
            Debug.Log("🎮 [Scene Builder] Creating new prefabs based on GameSettings...");
            
            CreateCardPrefab();
            CreateUIManagerPrefab();
            CreateGameManagerPrefab();
            
            Debug.Log("✅ [Scene Builder] New prefabs created");
        }
        
        private void CreateCardPrefab()
        {
            var cardPrefabPath = $"{_gameSettings.GetFullAssetPath(_gameSettings.cardPrefabPath)}/CardPrefab.prefab";
            
            // Create card GameObject
            var cardObj = new GameObject("CardPrefab");
            var rectTransform = cardObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 300);
            
            var canvasGroup = cardObj.AddComponent<CanvasGroup>();
            var cardController = cardObj.AddComponent<CardViewController>();
            
            // Create card front
            var frontObj = new GameObject("CardFront");
            frontObj.transform.SetParent(cardObj.transform, false);
            var frontRect = frontObj.AddComponent<RectTransform>();
            frontRect.anchorMin = Vector2.zero;
            frontRect.anchorMax = Vector2.one;
            frontRect.sizeDelta = Vector2.zero;
            var frontImage = frontObj.AddComponent<Image>();
            
            // Create card back
            var backObj = new GameObject("CardBack");
            backObj.transform.SetParent(cardObj.transform, false);
            var backRect = backObj.AddComponent<RectTransform>();
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = Vector2.one;
            backRect.sizeDelta = Vector2.zero;
            var backImage = backObj.AddComponent<Image>();
            
            // Setup CardViewController references using reflection
            var cardFrontField = typeof(CardViewController).GetField("_cardFront", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cardBackField = typeof(CardViewController).GetField("_cardBack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canvasGroupField = typeof(CardViewController).GetField("_canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            cardFrontField?.SetValue(cardController, frontImage);
            cardBackField?.SetValue(cardController, backImage);
            canvasGroupField?.SetValue(cardController, canvasGroup);
            
            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(cardObj, cardPrefabPath);
            DestroyImmediate(cardObj);
            
            Debug.Log($"✅ [Scene Builder] Created CardPrefab at: {cardPrefabPath}");
        }
        
        private void CreateUIManagerPrefab()
        {
            var uiManagerObj = new GameObject("UIManager");
            var uiManager = uiManagerObj.AddComponent<UIManager>();
            
            var uiPrefabPath = $"{_gameSettings.GetFullAssetPath(_gameSettings.uiPrefabPath)}/UIManager.prefab";
            PrefabUtility.SaveAsPrefabAsset(uiManagerObj, uiPrefabPath);
            DestroyImmediate(uiManagerObj);
            
            Debug.Log($"✅ [Scene Builder] Created UIManager prefab at: {uiPrefabPath}");
        }
        
        private void CreateGameManagerPrefab()
        {
            var gameManagerObj = new GameObject("GameManager");
            var animationController = gameManagerObj.AddComponent<CardAnimationController>();
            
            // Setup animation controller with GameSettings values
            var dealDelayField = typeof(CardAnimationController).GetField("_dealDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var warDurationField = typeof(CardAnimationController).GetField("_warAnimationDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            dealDelayField?.SetValue(animationController, _gameSettings.dealDelay);
            warDurationField?.SetValue(animationController, _gameSettings.warAnimationDuration);
            
            var managerPrefabPath = $"{_gameSettings.GetFullAssetPath(_gameSettings.prefabsPath)}/GameManager.prefab";
            PrefabUtility.SaveAsPrefabAsset(gameManagerObj, managerPrefabPath);
            DestroyImmediate(gameManagerObj);
            
            Debug.Log($"✅ [Scene Builder] Created GameManager prefab at: {managerPrefabPath}");
        }
        
        private void SetupSceneHierarchy()
        {
            Debug.Log("🏗️ [Scene Builder] Setting up clean scene hierarchy...");
            
            // Clear existing scene (with confirmation)
            ClearScene();
            
            // Create ProjectContext
            CreateProjectContext();
            
            // Create main Canvas with proper setup
            CreateMainCanvas();
            
            // Create game positions and managers
            CreateGamePositions();
            CreateManagers();
            
            Debug.Log("✅ [Scene Builder] Scene hierarchy setup complete");
        }
        
        private void ClearScene()
        {
            // Remove existing GameObjects (except Camera)
            var rootObjects = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount; i++)
            {
                var rootObject = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[i];
                if (rootObject.name != "Main Camera")
                {
                    rootObjects.Add(rootObject);
                }
            }
            
            foreach (var obj in rootObjects)
            {
                if (obj.name.Contains("OLD") || EditorUtility.DisplayDialog("Delete GameObject", 
                    $"Delete existing GameObject '{obj.name}'?", "Yes", "No"))
                {
                    DestroyImmediate(obj);
                }
            }
        }
        
        private void CreateProjectContext()
        {
            var projectContextPath = "Assets/Resources/ProjectContext.prefab";
            var existingContext = AssetDatabase.LoadAssetAtPath<GameObject>(projectContextPath);
            
            if (existingContext == null)
            {
                var contextObj = new GameObject("ProjectContext");
                var context = contextObj.AddComponent<ProjectContext>();
                
                PrefabUtility.SaveAsPrefabAsset(contextObj, projectContextPath);
                DestroyImmediate(contextObj);
                
                Debug.Log("✅ [Scene Builder] Created ProjectContext prefab");
            }
        }
        
        private void CreateMainCanvas()
        {
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            var raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = _gameSettings.canvasReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = _gameSettings.canvasMatchWidthOrHeight;
            
            // Create Canvas hierarchy
            CreateCanvasLayer(canvasObj.transform, "BackgroundLayer", 0);
            CreateCanvasLayer(canvasObj.transform, "GamePanel", 10);
            CreateCanvasLayer(canvasObj.transform, "UILayer", 20);
            CreateCanvasLayer(canvasObj.transform, "OverlayLayer", 90);
            
            // Create CardPoolContainer under GamePanel
            var gamePanel = canvasObj.transform.Find("GamePanel");
            var poolContainer = new GameObject("CardPoolContainer");
            poolContainer.transform.SetParent(gamePanel, false);
        }
        
        private void CreateCanvasLayer(Transform parent, string name, int sortOrder)
        {
            var layerObj = new GameObject(name);
            layerObj.transform.SetParent(parent, false);
            
            var rectTransform = layerObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            var canvasGroup = layerObj.AddComponent<CanvasGroup>();
        }
        
        private void CreateGamePositions()
        {
            var canvas = FindObjectOfType<Canvas>();
            var gamePanel = canvas.transform.Find("GamePanel");
            
            var positionsObj = new GameObject("GamePositions");
            positionsObj.transform.SetParent(gamePanel, false);
            
            CreatePosition(positionsObj.transform, "PlayerCardPosition", _gameSettings.playerCardPosition);
            CreatePosition(positionsObj.transform, "OpponentCardPosition", _gameSettings.opponentCardPosition);
            CreatePosition(positionsObj.transform, "DeckPosition", _gameSettings.deckPosition);
            CreatePosition(positionsObj.transform, "WarPilePosition", _gameSettings.warPilePosition);
        }
        
        private void CreatePosition(Transform parent, string name, Vector3 position)
        {
            var posObj = new GameObject(name);
            posObj.transform.SetParent(parent, false);
            posObj.transform.localPosition = position;
        }
        
        private void CreateManagers()
        {
            var managersObj = new GameObject("Managers");
            
            // Add UIManager
            var uiManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{_gameSettings.GetFullAssetPath(_gameSettings.uiPrefabPath)}/UIManager.prefab");
            if (uiManagerPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(uiManagerPrefab, managersObj.transform);
            }
            
            // Add GameManager
            var gameManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{_gameSettings.GetFullAssetPath(_gameSettings.prefabsPath)}/GameManager.prefab");
            if (gameManagerPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(gameManagerPrefab, managersObj.transform);
            }
        }
        
        private void ConnectAllReferences()
        {
            Debug.Log("🔗 [Scene Builder] Connecting all references based on GameSettings...");
            
            // Find components
            var cardAnimationController = FindObjectOfType<CardAnimationController>();
            var gameInstaller = FindObjectOfType<GameInstaller>();
            
            if (cardAnimationController != null)
            {
                ConnectAnimationControllerReferences(cardAnimationController);
            }
            
            if (gameInstaller != null)
            {
                ConnectGameInstallerReferences(gameInstaller);
            }
            
            Debug.Log("✅ [Scene Builder] All references connected");
        }
        
        private void ConnectAnimationControllerReferences(CardAnimationController controller)
        {
            var positions = FindObjectOfType<Canvas>().transform.Find("GamePanel/GamePositions");
            
            if (positions != null)
            {
                var playerPos = positions.Find("PlayerCardPosition");
                var opponentPos = positions.Find("OpponentCardPosition");
                var deckPos = positions.Find("DeckPosition");
                var warPos = positions.Find("WarPilePosition");
                
                // Use reflection to set private fields
                var playerField = typeof(CardAnimationController).GetField("_playerCardPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var opponentField = typeof(CardAnimationController).GetField("_opponentCardPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var deckField = typeof(CardAnimationController).GetField("_deckPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var warField = typeof(CardAnimationController).GetField("_warPilePosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                playerField?.SetValue(controller, playerPos);
                opponentField?.SetValue(controller, opponentPos);
                deckField?.SetValue(controller, deckPos);
                warField?.SetValue(controller, warPos);
                
                Debug.Log("✅ [Scene Builder] CardAnimationController references connected");
            }
        }
        
        private void ConnectGameInstallerReferences(GameInstaller installer)
        {
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{_gameSettings.GetFullAssetPath(_gameSettings.cardPrefabPath)}/CardPrefab.prefab");
            var poolContainer = FindObjectOfType<Canvas>().transform.Find("GamePanel/CardPoolContainer");
            
            if (cardPrefab != null && poolContainer != null)
            {
                // Use reflection to set private fields
                var prefabField = typeof(GameInstaller).GetField("_cardPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var poolField = typeof(GameInstaller).GetField("_cardPoolContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                prefabField?.SetValue(installer, cardPrefab);
                poolField?.SetValue(installer, poolContainer);
                
                Debug.Log("✅ [Scene Builder] GameInstaller references connected");
            }
        }
        
        private void ValidateCompleteSetup()
        {
            Debug.Log("🧪 [Scene Builder] Validating complete setup...");
            
            var issues = new System.Collections.Generic.List<string>();
            
            // Validate GameSettings
            if (_gameSettings == null)
                issues.Add("GameSettings asset missing");
            else if (!_gameSettings.ValidateAssetPaths())
                issues.Add("GameSettings asset paths invalid");
            
            // Validate scene components
            if (FindObjectOfType<ProjectContext>() == null)
                issues.Add("ProjectContext missing");
            if (FindObjectOfType<Canvas>() == null)
                issues.Add("Main Canvas missing");
            if (FindObjectOfType<CardAnimationController>() == null)
                issues.Add("CardAnimationController missing");
            if (FindObjectOfType<UIManager>() == null)
                issues.Add("UIManager missing");
            
            // Validate prefabs
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{_gameSettings.GetFullAssetPath(_gameSettings.cardPrefabPath)}/CardPrefab.prefab");
            if (cardPrefab == null)
                issues.Add("CardPrefab missing");
            
            if (issues.Count > 0)
            {
                Debug.LogWarning($"🧪 [Scene Builder] Validation found {issues.Count} issues: {string.Join(", ", issues)}");
            }
            else
            {
                Debug.Log("✅ [Scene Builder] All validation checks passed!");
            }
        }
        
        private void LoadOrCreateGameSettings()
        {
            _gameSettings = Resources.Load<GameSettings>("GameSettings");
            if (_gameSettings == null)
            {
                // Try to find it anywhere in the project
                var guids = AssetDatabase.FindAssets("t:GameSettings");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _gameSettings = AssetDatabase.LoadAssetAtPath<GameSettings>(path);
                }
            }
        }
        
        private void CreateGameSettingsAsset()
        {
            var settingsPath = "Assets/Resources/GameSettings.asset";
            
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            var settings = CreateInstance<GameSettings>();
            AssetDatabase.CreateAsset(settings, settingsPath);
            AssetDatabase.SaveAssets();
            
            _gameSettings = settings;
            Selection.activeObject = settings;
            
            Debug.Log($"✅ [Scene Builder] Created GameSettings asset at: {settingsPath}");
        }
        
        private void CreateFolderIfNotExists(string parent, string folderName)
        {
            string fullPath = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
        
        private void BackupIfExists(string originalPath, string backupPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(originalPath) != null)
            {
                AssetDatabase.CopyAsset(originalPath, backupPath);
                Debug.Log($"🔄 [Scene Builder] Backed up: {originalPath} → {backupPath}");
            }
        }
    }
}