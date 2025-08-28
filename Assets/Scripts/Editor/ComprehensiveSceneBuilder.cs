using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using Zenject;
using CardWar.Core.UI;
using CardWar.Gameplay.Controllers;
using CardWar.Configuration;
using CardWar.Infrastructure.Installers;
using CardWar.Services.Assets;
using CardWar.Editor.Builders;

namespace CardWar.Editor
{
    public class ComprehensiveSceneBuilder : EditorWindow
    {
        private GameSettings _gameSettings;
        private bool _useArtAssets = true;
        private bool _backupExistingAssets = true;
        private string _backupSuffix = "_OLD";
        
        [MenuItem("CardWar/🎨 Fixed Scene Builder")]
        public static void ShowWindow()
        {
            var window = GetWindow<ComprehensiveSceneBuilder>("War Card Game - Fixed Scene Builder");
            window.minSize = new Vector2(450, 700);
        }
        
        private void OnEnable()
        {
            LoadGameSettings();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🎨 WAR CARD GAME - FIXED SCENE BUILDER", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This FIXED version will:\n" +
                "• Create proper Canvas with screen adaptation\n" +
                "• Build UI with actual art assets\n" +
                "• Set up working prefabs with correct references\n" +
                "• Position elements correctly for all screens\n" +
                "• Fix all Zenject integration issues\n" +
                "• Validate the complete setup",
                MessageType.Info);
            
            GUILayout.Space(10);
            
            // Settings Section
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            _gameSettings = (GameSettings)EditorGUILayout.ObjectField("Game Settings", _gameSettings, typeof(GameSettings), false);
            _useArtAssets = EditorGUILayout.Toggle("Use Art Assets", _useArtAssets);
            _backupExistingAssets = EditorGUILayout.Toggle("Backup Existing Assets", _backupExistingAssets);
            
            if (_backupExistingAssets)
            {
                EditorGUI.indentLevel++;
                _backupSuffix = EditorGUILayout.TextField("Backup Suffix", _backupSuffix);
                EditorGUI.indentLevel--;
            }
            
            if (_gameSettings == null)
            {
                EditorGUILayout.HelpBox("Game Settings required!", MessageType.Error);
                if (GUILayout.Button("Create Game Settings"))
                {
                    CreateGameSettings();
                }
                return;
            }
            
            GUILayout.Space(20);
            
            // Main Actions
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🚀 BUILD COMPLETE FIXED SCENE", GUILayout.Height(60)))
            {
                if (EditorUtility.DisplayDialog("Build Fixed Scene", 
                    $"This will create a completely new scene setup.\n\n" +
                    "Process includes:\n" +
                    "• Clear existing scene\n" +
                    "• Create proper Canvas hierarchy\n" +
                    "• Set up art-integrated UI\n" +
                    "• Build working prefabs\n" +
                    "• Configure Zenject properly\n" +
                    "• Validate complete setup\n\n" +
                    "Continue?", 
                    "Yes, Build Scene", "Cancel"))
                {
                    BuildFixedScene();
                }
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(10);
            
            // Individual Actions
            EditorGUILayout.LabelField("Individual Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("🧪 Test Art Asset Loading"))
            {
                TestArtAssets();
            }
            
            if (GUILayout.Button("🔧 Create Canvas System Only"))
            {
                CreateCanvasSystemOnly();
            }
            
            if (GUILayout.Button("🎨 Create Art-Integrated Prefabs"))
            {
                CreateArtIntegratedPrefabs();
            }
            
            if (GUILayout.Button("🔗 Fix Existing References"))
            {
                FixExistingReferences();
            }
            
            if (GUILayout.Button("🧹 Clean Scene"))
            {
                if (EditorUtility.DisplayDialog("Clean Scene", "This will remove all GameObjects except Main Camera. Continue?", "Yes", "Cancel"))
                {
                    ClearScene();
                }
            }
            
            GUILayout.Space(20);
            
            // Status Section
            EditorGUILayout.LabelField("Scene Status", EditorStyles.boldLabel);
            DisplaySceneStatus();
        }
        
        private void BuildFixedScene()
        {
            Debug.Log("🎨 [Fixed Scene Builder] Starting complete fixed scene build...");
            
            try
            {
                EditorUtility.DisplayProgressBar("Building Scene", "Backing up existing assets...", 0.1f);
                if (_backupExistingAssets)
                    BackupExistingAssets();
                
                EditorUtility.DisplayProgressBar("Building Scene", "Clearing scene...", 0.2f);
                ClearScene();
                
                EditorUtility.DisplayProgressBar("Building Scene", "Creating ProjectContext...", 0.3f);
                CreateProjectContext();
                
                EditorUtility.DisplayProgressBar("Building Scene", "Building Canvas system...", 0.4f);
                CreateMainCanvasWithArt();
                
                EditorUtility.DisplayProgressBar("Building Scene", "Creating art-integrated prefabs...", 0.6f);
                CreateArtIntegratedPrefabs();
                
                EditorUtility.DisplayProgressBar("Building Scene", "Setting up managers...", 0.8f);
                SetupManagersWithConnections();
                
                EditorUtility.DisplayProgressBar("Building Scene", "Validating setup...", 0.9f);
                ValidateFixedSetup();
                
                EditorUtility.ClearProgressBar();
                
                Debug.Log("✅ [Fixed Scene Builder] Complete fixed scene build finished!");
                
                EditorUtility.DisplayDialog("Success!", 
                    "✅ Fixed scene build completed!\n\n" +
                    "Your scene now has:\n" +
                    "• Proper Canvas with screen adaptation\n" +
                    "• UI using actual art assets\n" +
                    "• Working prefabs with correct references\n" +
                    "• Proper Zenject integration\n\n" +
                    "Press Play to test!",
                    "Awesome!");
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"❌ [Fixed Scene Builder] Error: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Build failed: {ex.Message}\n\nCheck console for details.", "OK");
            }
        }
        
        private void BackupExistingAssets()
        {
            Debug.Log($"🔄 [Fixed Scene Builder] Backing up assets with suffix '{_backupSuffix}'...");
            
            // Backup existing prefabs
            BackupAssetIfExists("Assets/Resources/Prefabs/Cards/CardPrefab.prefab", 
                $"Assets/Resources/Prefabs/Cards/CardPrefab{_backupSuffix}.prefab");
            BackupAssetIfExists("Assets/Resources/Prefabs/UI/UIManager.prefab", 
                $"Assets/Resources/Prefabs/UI/UIManager{_backupSuffix}.prefab");
        }
        
        private void BackupAssetIfExists(string originalPath, string backupPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(originalPath) != null)
            {
                AssetDatabase.CopyAsset(originalPath, backupPath);
                Debug.Log($"🔄 Backed up: {originalPath} → {backupPath}");
            }
        }
        
        private void ClearScene()
        {
            Debug.Log("🧹 [Fixed Scene Builder] Clearing scene...");
            
            var rootObjects = new System.Collections.Generic.List<GameObject>();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            
            for (int i = 0; i < scene.rootCount; i++)
            {
                var rootObject = scene.GetRootGameObjects()[i];
                if (rootObject.name != "Main Camera")
                {
                    rootObjects.Add(rootObject);
                }
            }
            
            foreach (var obj in rootObjects)
            {
                DestroyImmediate(obj);
            }
            
            Debug.Log($"🧹 Cleared {rootObjects.Count} objects from scene");
        }
        
        private void CreateProjectContext()
        {
            Debug.Log("🔧 [Fixed Scene Builder] Creating ProjectContext...");
            
            var contextObj = new GameObject("ProjectContext");
            var projectContext = contextObj.AddComponent<ProjectContext>();
            var installer = contextObj.AddComponent<ProjectInstaller>();
            
            Debug.Log("✅ ProjectContext created with ProjectInstaller");
        }
        
        private void CreateMainCanvasWithArt()
        {
            Debug.Log("🎨 [Fixed Scene Builder] Creating main canvas with art integration...");
            
            var canvasObj = new GameObject("MainCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            var raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            var canvasManager = canvasObj.AddComponent<CanvasManager>();
            
            // Setup canvas
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            
            // Setup scaler for multiple screen sizes
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = _gameSettings.canvasReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // Balance between width and height
            
            // Create background with art
            CreateBackgroundWithArt(canvasObj.transform);
            
            // Create layers
            var backgroundLayer = CreateCanvasLayer(canvasObj.transform, "BackgroundLayer", 0);
            var gameLayer = CreateCanvasLayer(canvasObj.transform, "GameLayer", 10);
            var uiLayer = CreateCanvasLayer(canvasObj.transform, "UILayer", 20);
            var overlayLayer = CreateCanvasLayer(canvasObj.transform, "OverlayLayer", 90);
            
            // Set references on CanvasManager using reflection
            SetCanvasManagerReferences(canvasManager, canvas, scaler, backgroundLayer, gameLayer, uiLayer, overlayLayer);
            
            // Create game positions under game layer
            CreateGamePositions(gameLayer);
            
            // Create UI elements under UI layer
            CreateUIElements(uiLayer);
            
            Debug.Log("✅ Main canvas created with proper art integration");
        }
        
        private void CreateCanvasSystemOnly()
        {
            Debug.Log("🔧 [Fixed Scene Builder] Creating canvas system only...");
            
            // Remove existing canvas if present
            var existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                if (EditorUtility.DisplayDialog("Replace Canvas", "Replace existing canvas?", "Yes", "Cancel"))
                {
                    DestroyImmediate(existingCanvas.gameObject);
                }
                else
                {
                    return;
                }
            }
            
            CreateMainCanvasWithArt();
        }
        
        private void CreateBackgroundWithArt(Transform parent)
        {
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(parent, false);
            
            var rectTransform = bgObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            var image = bgObj.AddComponent<Image>();
            
            if (_useArtAssets)
            {
                var bgSprite = Resources.Load<Sprite>("GameplaySprites/Backgrounds/TableBackground");
                if (bgSprite != null)
                {
                    image.sprite = bgSprite;
                    Debug.Log("✅ Background art loaded");
                }
                else
                {
                    image.color = _gameSettings.backgroundColor;
                    Debug.Log("⚠️ Background art not found, using color");
                }
            }
            else
            {
                image.color = _gameSettings.backgroundColor;
            }
        }
        
        private RectTransform CreateCanvasLayer(Transform parent, string name, int sortOrder)
        {
            var layerObj = new GameObject(name);
            layerObj.transform.SetParent(parent, false);
            
            var rectTransform = layerObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            var canvasGroup = layerObj.AddComponent<CanvasGroup>();
            
            return rectTransform;
        }
        
        private void CreateGamePositions(Transform gameLayer)
        {
            var positionsObj = new GameObject("GamePositions");
            positionsObj.transform.SetParent(gameLayer, false);
            
            CreatePosition(positionsObj.transform, "PlayerCardPosition", _gameSettings.playerCardPosition);
            CreatePosition(positionsObj.transform, "OpponentCardPosition", _gameSettings.opponentCardPosition);
            CreatePosition(positionsObj.transform, "DeckPosition", _gameSettings.deckPosition);
            CreatePosition(positionsObj.transform, "WarPilePosition", _gameSettings.warPilePosition);
            
            // Create card pool container
            var poolContainer = new GameObject("CardPoolContainer");
            poolContainer.transform.SetParent(gameLayer, false);
            poolContainer.AddComponent<RectTransform>();
        }
        
        private void CreatePosition(Transform parent, string name, Vector3 position)
        {
            var posObj = new GameObject(name);
            posObj.transform.SetParent(parent, false);
            posObj.transform.localPosition = position;
            
            // Add visual indicator for editor
            #if UNITY_EDITOR
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.transform.SetParent(posObj.transform, false);
            indicator.transform.localScale = Vector3.one * 0.1f;
            indicator.name = $"{name}_Indicator";
            
            var renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.yellow;
            }
            #endif
        }
        
        private void CreateUIElements(Transform uiLayer)
        {
            CreateScorePanel(uiLayer, "PlayerScorePanel", true);
            CreateScorePanel(uiLayer, "OpponentScorePanel", false);
            CreateCenterPanel(uiLayer);
            CreateGameStatePanel(uiLayer);
            CreateWarIndicator(uiLayer);
            CreateGameOverScreen(uiLayer);
        }
        
        private void CreateScorePanel(Transform parent, string name, bool isPlayer)
        {
            var panelObj = new GameObject(name);
            panelObj.transform.SetParent(parent, false);
            
            var rectTransform = panelObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 100);
            
            // Position at top or bottom
            if (isPlayer)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rectTransform.anchorMax = new Vector2(0.5f, 0f);
                rectTransform.anchoredPosition = new Vector2(0, 100);
            }
            else
            {
                rectTransform.anchorMin = new Vector2(0.5f, 1f);
                rectTransform.anchorMax = new Vector2(0.5f, 1f);
                rectTransform.anchoredPosition = new Vector2(0, -100);
            }
            
            // Add background image
            var bgImage = panelObj.AddComponent<Image>();
            if (_useArtAssets)
            {
                var panelSprite = Resources.Load<Sprite>("GameplaySprites/UI/ScorePanelDecor");
                if (panelSprite != null)
                {
                    bgImage.sprite = panelSprite;
                }
            }
            bgImage.color = isPlayer ? _gameSettings.playerColor : _gameSettings.opponentColor;
            
            // Create text
            var textObj = new GameObject($"{name}Text");
            textObj.transform.SetParent(panelObj.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = isPlayer ? "Player: 26" : "Opponent: 26";
            textComponent.fontSize = 24;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontStyle = FontStyles.Bold;
        }
        
        private void CreateCenterPanel(Transform parent)
        {
            var panelObj = new GameObject("CenterGamePanel");
            panelObj.transform.SetParent(parent, false);
            
            var rectTransform = panelObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(300, 80);
            
            // Add background
            var bgImage = panelObj.AddComponent<Image>();
            if (_useArtAssets)
            {
                var panelSprite = Resources.Load<Sprite>("GameplaySprites/UI/RoundPanel");
                if (panelSprite != null)
                {
                    bgImage.sprite = panelSprite;
                }
            }
            
            // Create round text
            var textObj = new GameObject("RoundText");
            textObj.transform.SetParent(panelObj.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "Round: 1";
            textComponent.fontSize = 20;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontStyle = FontStyles.Bold;
        }
        
        private void CreateGameStatePanel(Transform parent)
        {
            var panelObj = new GameObject("GameStatePanel");
            panelObj.transform.SetParent(parent, false);
            
            var rectTransform = panelObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.3f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.3f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(200, 50);
            
            var textComponent = panelObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "Tap to Draw";
            textComponent.fontSize = 18;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
        }
        
        private void CreateWarIndicator(Transform parent)
        {
            var warObj = new GameObject("WarIndicator");
            warObj.transform.SetParent(parent, false);
            
            var rectTransform = warObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(200, 200);
            
            var image = warObj.AddComponent<Image>();
            if (_useArtAssets)
            {
                var warSprite = Resources.Load<Sprite>("GameplaySprites/Backgrounds/WarIndicator");
                if (warSprite != null)
                {
                    image.sprite = warSprite;
                }
            }
            
            warObj.SetActive(false);
        }
        
        private void CreateGameOverScreen(Transform parent)
        {
            var gameOverObj = new GameObject("GameOverScreen");
            gameOverObj.transform.SetParent(parent, false);
            
            var rectTransform = gameOverObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Semi-transparent background
            var bgImage = gameOverObj.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);
            
            // Winner text
            var winnerTextObj = new GameObject("WinnerText");
            winnerTextObj.transform.SetParent(gameOverObj.transform, false);
            
            var winnerRect = winnerTextObj.AddComponent<RectTransform>();
            winnerRect.anchorMin = new Vector2(0.5f, 0.5f);
            winnerRect.anchorMax = new Vector2(0.5f, 0.5f);
            winnerRect.anchoredPosition = Vector2.zero;
            winnerRect.sizeDelta = new Vector2(400, 100);
            
            var winnerText = winnerTextObj.AddComponent<TextMeshProUGUI>();
            winnerText.text = "GAME OVER";
            winnerText.fontSize = 36;
            winnerText.color = Color.white;
            winnerText.alignment = TextAlignmentOptions.Center;
            winnerText.fontStyle = FontStyles.Bold;
            
            gameOverObj.SetActive(false);
        }
        
        private void CreateArtIntegratedPrefabs()
        {
            Debug.Log("🎨 [Fixed Scene Builder] Creating art-integrated prefabs...");
            
            try
            {
                // Create asset service instance for prefab creation
                var assetService = new AssetService();
                assetService.Initialize();
                
                // Create card prefab with art
                CreateCardPrefabWithArt(assetService);
                
                // Create UI manager prefab
                CreateUIManagerPrefab();
                
                Debug.Log("✅ Art-integrated prefabs created successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Error creating prefabs: {ex.Message}");
            }
        }
        
        private void CreateCardPrefabWithArt(IAssetService assetService)
        {
            var cardPrefab = CardPrefabBuilder.CreateCardPrefab(assetService, _gameSettings);
            
            var prefabPath = "Assets/Resources/Prefabs/Cards/CardPrefab.prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            
            PrefabUtility.SaveAsPrefabAsset(cardPrefab, prefabPath);
            DestroyImmediate(cardPrefab);
            
            // Validate the created prefab
            var createdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (CardPrefabBuilder.ValidateCardPrefab(createdPrefab))
            {
                Debug.Log($"✅ Card prefab with art created at: {prefabPath}");
            }
            else
            {
                Debug.LogError($"❌ Card prefab validation failed for: {prefabPath}");
            }
        }
        
        private void CreateUIManagerPrefab()
        {
            var uiManagerObj = new GameObject("UIManager");
            var uiManager = uiManagerObj.AddComponent<UIManager>();
            
            // Connect UI elements from scene to prefab references
            ConnectUIManagerReferences(uiManager);
            
            var prefabPath = "Assets/Resources/Prefabs/UI/UIManager.prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            
            PrefabUtility.SaveAsPrefabAsset(uiManagerObj, prefabPath);
            DestroyImmediate(uiManagerObj);
            
            Debug.Log($"✅ UIManager prefab created at: {prefabPath}");
        }
        
        private void SetCanvasManagerReferences(CanvasManager canvasManager, Canvas canvas, CanvasScaler scaler, 
            RectTransform background, RectTransform game, RectTransform ui, RectTransform overlay)
        {
            SetPrivateField(canvasManager, "_mainCanvas", canvas);
            SetPrivateField(canvasManager, "_canvasScaler", scaler);
            SetPrivateField(canvasManager, "_backgroundLayer", background);
            SetPrivateField(canvasManager, "_gameLayer", game);
            SetPrivateField(canvasManager, "_uiLayer", ui);
            SetPrivateField(canvasManager, "_overlayLayer", overlay);
        }
        
        private void ConnectUIManagerReferences(UIManager uiManager)
        {
            // Find UI elements in scene
            var playerScorePanel = GameObject.Find("PlayerScorePanel");
            var opponentScorePanel = GameObject.Find("OpponentScorePanel");
            var centerPanel = GameObject.Find("CenterGamePanel");
            var gameStatePanel = GameObject.Find("GameStatePanel");
            
            var playerScoreText = playerScorePanel?.GetComponentInChildren<TextMeshProUGUI>();
            var opponentScoreText = opponentScorePanel?.GetComponentInChildren<TextMeshProUGUI>();
            var roundText = centerPanel?.GetComponentInChildren<TextMeshProUGUI>();
            var gameStateText = gameStatePanel?.GetComponent<TextMeshProUGUI>();
            
            // Set all UI references using reflection
            SetPrivateField(uiManager, "_playerScorePanel", playerScorePanel);
            SetPrivateField(uiManager, "_opponentScorePanel", opponentScorePanel);
            SetPrivateField(uiManager, "_centerGamePanel", centerPanel);
            SetPrivateField(uiManager, "_gameStatePanel", gameStatePanel);
            
            SetPrivateField(uiManager, "_playerScoreText", playerScoreText);
            SetPrivateField(uiManager, "_opponentScoreText", opponentScoreText);
            SetPrivateField(uiManager, "_roundText", roundText);
            SetPrivateField(uiManager, "_gameStateText", gameStateText);
            
            SetPrivateField(uiManager, "_warIndicator", GameObject.Find("WarIndicator"));
            SetPrivateField(uiManager, "_gameOverScreen", GameObject.Find("GameOverScreen"));
            SetPrivateField(uiManager, "_winnerText", GameObject.Find("GameOverScreen")?.GetComponentInChildren<TextMeshProUGUI>());
            
            // Set background references
            SetPrivateField(uiManager, "_backgroundImage", GameObject.Find("Background")?.GetComponent<Image>());
            SetPrivateField(uiManager, "_playerScoreBackground", playerScorePanel?.GetComponent<Image>());
            SetPrivateField(uiManager, "_opponentScoreBackground", opponentScorePanel?.GetComponent<Image>());
            SetPrivateField(uiManager, "_centerPanelBackground", centerPanel?.GetComponent<Image>());
        }
        
        private void SetupManagersWithConnections()
        {
            Debug.Log("🔧 [Fixed Scene Builder] Setting up managers with connections...");
            
            // Create managers container
            var managersObj = new GameObject("Managers");
            
            // Add UIManager instance
            var uiManager = managersObj.AddComponent<UIManager>();
            ConnectUIManagerReferences(uiManager);
            
            // Add CardAnimationController
            var animationController = managersObj.AddComponent<CardAnimationController>();
            ConnectAnimationControllerReferences(animationController);
            
            // Create SceneContext
            var sceneContext = new GameObject("SceneContext");
            var context = sceneContext.AddComponent<SceneContext>();
            var gameInstaller = sceneContext.AddComponent<GameInstaller>();
            
            // Connect GameInstaller references
            ConnectGameInstallerReferences(gameInstaller);
        }
        
        private void ConnectAnimationControllerReferences(CardAnimationController controller)
        {
            var positions = GameObject.Find("GamePositions");
            if (positions != null)
            {
                var playerPos = positions.transform.Find("PlayerCardPosition");
                var opponentPos = positions.transform.Find("OpponentCardPosition");
                var deckPos = positions.transform.Find("DeckPosition");
                var warPos = positions.transform.Find("WarPilePosition");
                
                SetPrivateField(controller, "_playerCardPosition", playerPos);
                SetPrivateField(controller, "_opponentCardPosition", opponentPos);
                SetPrivateField(controller, "_deckPosition", deckPos);
                SetPrivateField(controller, "_warPilePosition", warPos);
            }
        }
        
        private void ConnectGameInstallerReferences(GameInstaller installer)
        {
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Cards/CardPrefab.prefab");
            var poolContainer = GameObject.Find("CardPoolContainer");
            
            SetPrivateField(installer, "_cardPrefab", cardPrefab);
            SetPrivateField(installer, "_cardPoolContainer", poolContainer?.transform);
        }
        
        private void FixExistingReferences()
        {
            Debug.Log("🔗 [Fixed Scene Builder] Fixing existing references...");
            
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                ConnectUIManagerReferences(uiManager);
                Debug.Log("✅ UIManager references fixed");
            }
            
            var animationController = FindObjectOfType<CardAnimationController>();
            if (animationController != null)
            {
                ConnectAnimationControllerReferences(animationController);
                Debug.Log("✅ CardAnimationController references fixed");
            }
            
            var gameInstaller = FindObjectOfType<GameInstaller>();
            if (gameInstaller != null)
            {
                ConnectGameInstallerReferences(gameInstaller);
                Debug.Log("✅ GameInstaller references fixed");
            }
        }
        
        private void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
        
        private void ValidateFixedSetup()
        {
            Debug.Log("🧪 [Fixed Scene Builder] Validating fixed setup...");
            
            var issues = new System.Collections.Generic.List<string>();
            
            // Validate essential GameObjects
            if (GameObject.Find("ProjectContext") == null) issues.Add("ProjectContext missing");
            if (GameObject.Find("MainCanvas") == null) issues.Add("MainCanvas missing");
            if (GameObject.Find("PlayerScorePanel") == null) issues.Add("PlayerScorePanel missing");
            if (GameObject.Find("OpponentScorePanel") == null) issues.Add("OpponentScorePanel missing");
            if (GameObject.Find("GamePositions") == null) issues.Add("GamePositions missing");
            if (GameObject.Find("CardPoolContainer") == null) issues.Add("CardPoolContainer missing");
            
            // Validate components
            if (FindObjectOfType<CanvasManager>() == null) issues.Add("CanvasManager missing");
            if (FindObjectOfType<UIManager>() == null) issues.Add("UIManager missing");
            if (FindObjectOfType<CardAnimationController>() == null) issues.Add("CardAnimationController missing");
            
            // Validate prefabs
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Cards/CardPrefab.prefab");
            if (cardPrefab == null) issues.Add("CardPrefab missing");
            
            if (issues.Count > 0)
            {
                Debug.LogWarning($"🧪 Found {issues.Count} issues: {string.Join(", ", issues)}");
            }
            else
            {
                Debug.Log("✅ All validation checks passed!");
            }
        }
        
        private void TestArtAssets()
        {
            Debug.Log("🧪 [Fixed Scene Builder] Testing art asset loading...");
            
            var testAssets = new[]
            {
                "GameplaySprites/Backgrounds/TableBackground",
                "GameplaySprites/UI/ScorePanelDecor",
                "GameplaySprites/UI/RoundPanel",
                "GameplaySprites/Backgrounds/WarIndicator",
                "GameplaySprites/Cards/card_back",
                "GameplaySprites/Cards/ace_spades"
            };
            
            foreach (var assetPath in testAssets)
            {
                var sprite = Resources.Load<Sprite>(assetPath);
                Debug.Log($"🎨 Asset {assetPath}: {(sprite != null ? "✅ Found" : "❌ Missing")}");
            }
        }
        
        private void DisplaySceneStatus()
        {
            EditorGUI.BeginDisabledGroup(true);
            
            var canvas = FindObjectOfType<Canvas>();
            EditorGUILayout.Toggle("Main Canvas Present", canvas != null);
            
            var canvasManager = FindObjectOfType<CanvasManager>();
            EditorGUILayout.Toggle("Canvas Manager Present", canvasManager != null);
            
            var uiManager = FindObjectOfType<UIManager>();
            EditorGUILayout.Toggle("UI Manager Present", uiManager != null);
            
            var projectContext = FindObjectOfType<ProjectContext>();
            EditorGUILayout.Toggle("Project Context Present", projectContext != null);
            
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Cards/CardPrefab.prefab");
            EditorGUILayout.Toggle("Card Prefab Exists", cardPrefab != null);
            
            EditorGUI.EndDisabledGroup();
        }
        
        private void LoadGameSettings()
        {
            _gameSettings = Resources.Load<GameSettings>("GameSettings");
            if (_gameSettings == null)
            {
                var guids = AssetDatabase.FindAssets("t:GameSettings");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _gameSettings = AssetDatabase.LoadAssetAtPath<GameSettings>(path);
                }
            }
        }
        
        private void CreateGameSettings()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            var settings = CreateInstance<GameSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/Resources/GameSettings.asset");
            AssetDatabase.SaveAssets();
            
            _gameSettings = settings;
            Selection.activeObject = settings;
            
            Debug.Log("✅ GameSettings created at: Assets/Resources/GameSettings.asset");
        }
    }
}