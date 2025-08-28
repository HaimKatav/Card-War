using CardWar.Gameplay.Controllers;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Zenject;
using CardWar.UI;
using CardWar.UI.Cards;

namespace CardWar.Editor
{
    public class WarGameSceneSetup : EditorWindow
    {
        private const string MENU_PATH = "CardWar/Setup Scene";
        
        // Asset paths
        private const string RESOURCES_PATH = "Assets/Resources";
        private const string PREFABS_PATH = "Assets/Prefabs";
        private const string CARDS_PATH = "Assets/Prefabs/Cards";
        private const string ART_PATH = "Assets/Art";
        private const string CARDS_ART_PATH = "Assets/Art/Cards";
        
        [MenuItem(MENU_PATH)]
        public static void ShowWindow()
        {
            GetWindow<WarGameSceneSetup>("War Game Scene Setup");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("War Card Game - Complete Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool will set up your entire scene with all required GameObjects, components, and bindings for the War Card Game.",
                MessageType.Info);
            
            GUILayout.Space(20);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🚀 COMPLETE SETUP - ONE CLICK", GUILayout.Height(50)))
            {
                if (EditorUtility.DisplayDialog("Complete Setup", 
                    "This will create/update all necessary components for the War Card Game.\n\n" +
                    "This includes:\n" +
                    "• Zenject contexts (Project & Scene)\n" +
                    "• Complete game board hierarchy\n" +
                    "• UI Canvas with all elements\n" +
                    "• Card prefab with proper structure\n" +
                    "• Folder structure for assets\n\n" +
                    "Continue?", 
                    "Yes, Set Everything Up", "Cancel"))
                {
                    PerformCompleteSetup();
                }
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(20);
            GUILayout.Label("Individual Setup Options:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("1. Create Folder Structure"))
                CreateFolderStructure();
            
            if (GUILayout.Button("2. Create Zenject Contexts"))
                CreateZenjectContexts();
            
            if (GUILayout.Button("3. Setup Camera"))
                SetupCamera();
            
            if (GUILayout.Button("4. Create Complete Game Board"))
                CreateCompleteGameBoard();
            
            if (GUILayout.Button("5. Create UI Canvas with All Elements"))
                CreateCompleteUICanvas();
            
            if (GUILayout.Button("6. Create Card Prefab"))
                CreateCardPrefab();
            
            if (GUILayout.Button("7. Generate Card Sprite Placeholders"))
                GenerateCardSpritePlaceholders();
            
            GUILayout.Space(20);
            EditorGUILayout.HelpBox(
                "⚠️ After setup:\n" +
                "1. Add GameInstaller to SceneContext's Mono Installers\n" +
                "2. Add ProjectInstaller to ProjectContext's Mono Installers\n" +
                "3. Assign CardPrefab and CardPoolContainer in GameInstaller\n" +
                "4. Add actual card sprites to Assets/Art/Cards/",
                MessageType.Warning);
        }
        
        private void PerformCompleteSetup()
        {
            CreateFolderStructure();
            CreateZenjectContexts();
            SetupCamera();
            CreateCompleteGameBoard();
            CreateCompleteUICanvas();
            CreateCardPrefab();
            GenerateCardSpritePlaceholders();
            
            // Link components
            LinkAllComponents();
            
            EditorUtility.DisplayDialog("Setup Complete", 
                "✅ Scene setup completed successfully!\n\n" +
                "Next steps:\n" +
                "1. Check SceneContext has GameInstaller in Mono Installers\n" +
                "2. Check ProjectContext has ProjectInstaller in Mono Installers\n" +
                "3. Verify CardPrefab is assigned in GameInstaller\n" +
                "4. Add your card sprites to Assets/Art/Cards/\n" +
                "5. Press Play to test!",
                "OK");
            
            Debug.Log("✅ [Scene Setup] Complete setup finished successfully!");
        }
        
        private void CreateFolderStructure()
        {
            // Create all necessary folders
            CreateFolderIfNotExists("Assets", "Resources");
            CreateFolderIfNotExists("Assets", "Prefabs");
            CreateFolderIfNotExists(PREFABS_PATH, "Cards");
            CreateFolderIfNotExists("Assets", "Art");
            CreateFolderIfNotExists(ART_PATH, "Cards");
            
            Debug.Log("[Scene Setup] Folder structure created");
        }
        
        private void CreateFolderIfNotExists(string parent, string folderName)
        {
            string fullPath = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
        
        private void CreateZenjectContexts()
        {
            // Create SceneContext with proper setup
            GameObject sceneContext = GameObject.Find("SceneContext");
            if (sceneContext == null)
            {
                sceneContext = new GameObject("SceneContext");
            }
            
            SceneContext sceneContextComp = sceneContext.GetComponent<SceneContext>();
            if (sceneContextComp == null)
            {
                sceneContextComp = sceneContext.AddComponent<SceneContext>();
            }
            
            // Create ProjectContext prefab in Resources
            string projectContextPath = RESOURCES_PATH + "/ProjectContext.prefab";
            GameObject projectContextPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projectContextPath);
            
            if (projectContextPrefab == null)
            {
                GameObject projectContext = new GameObject("ProjectContext");
                var projectContextComp = projectContext.AddComponent<ProjectContext>();
                
                PrefabUtility.SaveAsPrefabAsset(projectContext, projectContextPath);
                DestroyImmediate(projectContext);
                
                Debug.Log("[Scene Setup] ProjectContext prefab created at: " + projectContextPath);
            }
            
            Debug.Log("[Scene Setup] Zenject contexts created");
            Debug.LogWarning("⚠️ [Scene Setup] MANUAL STEP: Add GameInstaller to SceneContext's Mono Installers");
            Debug.LogWarning("⚠️ [Scene Setup] MANUAL STEP: Add ProjectInstaller to ProjectContext's Mono Installers in Resources folder");
        }
        
        private void SetupCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.AddComponent<AudioListener>();
                cameraObj.tag = "MainCamera";
            }
            
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 10f;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.1f, 0.3f, 0.18f); // Dark green
            mainCamera.transform.position = new Vector3(0, 0, -10);
            
            Debug.Log("[Scene Setup] Camera configured");
        }
        
        private void CreateCompleteGameBoard()
        {
            GameObject gameBoard = FindOrCreate("GameBoard", null);
            
            // Create Background
            GameObject background = FindOrCreate("Background", gameBoard.transform);
            SpriteRenderer bgSprite = background.GetComponent<SpriteRenderer>();
            if (bgSprite == null)
            {
                bgSprite = background.AddComponent<SpriteRenderer>();
                bgSprite.color = new Color(0.15f, 0.4f, 0.22f); // Green table color
                bgSprite.sortingOrder = -10;
            }
            background.transform.localScale = new Vector3(20, 30, 1);
            
            // Player Area (Bottom)
            GameObject playerArea = FindOrCreate("PlayerArea", gameBoard.transform);
            playerArea.transform.position = new Vector3(0, -5, 0);
            
            GameObject playerDeck = FindOrCreate("PlayerDeck", playerArea.transform);
            playerDeck.transform.localPosition = new Vector3(-3, 0, 0);
            AddVisualIndicator(playerDeck, "Player\nDeck", Color.blue);
            
            GameObject playerPlayArea = FindOrCreate("PlayerPlayArea", playerArea.transform);
            playerPlayArea.transform.localPosition = new Vector3(0, 0, 0);
            AddVisualIndicator(playerPlayArea, "Player\nPlay", Color.cyan);
            
            // Opponent Area (Top)
            GameObject opponentArea = FindOrCreate("OpponentArea", gameBoard.transform);
            opponentArea.transform.position = new Vector3(0, 5, 0);
            
            GameObject opponentDeck = FindOrCreate("OpponentDeck", opponentArea.transform);
            opponentDeck.transform.localPosition = new Vector3(3, 0, 0);
            AddVisualIndicator(opponentDeck, "Opponent\nDeck", Color.red);
            
            GameObject opponentPlayArea = FindOrCreate("OpponentPlayArea", opponentArea.transform);
            opponentPlayArea.transform.localPosition = new Vector3(0, 0, 0);
            AddVisualIndicator(opponentPlayArea, "Opponent\nPlay", Color.magenta);
            
            // Center Area
            GameObject centerArea = FindOrCreate("CenterArea", gameBoard.transform);
            centerArea.transform.position = Vector3.zero;
            
            GameObject warPile = FindOrCreate("WarPile", centerArea.transform);
            warPile.transform.localPosition = new Vector3(-1, 0, 0);
            AddVisualIndicator(warPile, "War\nPile", Color.yellow);
            
            GameObject battleZone = FindOrCreate("BattleZone", centerArea.transform);
            battleZone.transform.localPosition = new Vector3(1, 0, 0);
            AddVisualIndicator(battleZone, "Battle\nZone", Color.green);
            
            Debug.Log("[Scene Setup] Complete game board created");
        }
        
        private void CreateCompleteUICanvas()
        {
            GameObject canvasObj = FindOrCreate("Canvas", null);
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            
            if (canvas == null)
            {
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;
            }
            
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
            
            GraphicRaycaster raycaster = canvasObj.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // Create UI structure
            GameObject gameHUD = FindOrCreate("GameHUD", canvas.transform);
            
            // Score Display
            GameObject scoreDisplay = FindOrCreate("ScoreDisplay", gameHUD.transform);
            CreateUIPanel(scoreDisplay, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50));
            
            GameObject playerScore = CreateText("PlayerScore", scoreDisplay.transform, 
                "Player: 26", new Vector2(0, 0.5f), new Vector2(200, -50), 36);
            
            GameObject opponentScore = CreateText("OpponentScore", scoreDisplay.transform,
                "Opponent: 26", new Vector2(1, 0.5f), new Vector2(-200, -50), 36);
            
            // Round Counter
            GameObject roundText = CreateText("RoundText", gameHUD.transform,
                "Round: 0", new Vector2(0.5f, 1), new Vector2(0, -150), 28);
            
            // Game State Text
            GameObject stateText = CreateText("StateText", gameHUD.transform,
                "Tap to Draw", new Vector2(0.5f, 0.5f), Vector2.zero, 48);
            
            // Popups
            GameObject popups = FindOrCreate("Popups", canvas.transform);
            
            // War Indicator
            GameObject warIndicator = FindOrCreate("WarIndicator", popups.transform);
            warIndicator.SetActive(false);
            CreateUIPanel(warIndicator, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 400, 200);
            CreateText("WarText", warIndicator.transform, "WAR!", new Vector2(0.5f, 0.5f), Vector2.zero, 72);
            
            // Game Over Screen
            GameObject gameOverScreen = FindOrCreate("GameOverScreen", popups.transform);
            gameOverScreen.SetActive(false);
            CreateUIPanel(gameOverScreen, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 600, 400);
            CreateText("GameOverText", gameOverScreen.transform, "Game Over", new Vector2(0.5f, 0.5f), new Vector2(0, 100), 48);
            CreateText("WinnerText", gameOverScreen.transform, "Winner: Player", new Vector2(0.5f, 0.5f), new Vector2(0, 0), 36);
            
            // Add UIManager component to Canvas
            UIManager uiManager = canvasObj.GetComponent<UIManager>();
            if (uiManager == null)
            {
                uiManager = canvasObj.AddComponent<UIManager>();
            }
            
            // Add CardAnimationController to GameBoard
            GameObject gameBoard = GameObject.Find("GameBoard");
            if (gameBoard != null)
            {
                CardAnimationController animController = gameBoard.GetComponent<CardAnimationController>();
                if (animController == null)
                {
                    animController = gameBoard.AddComponent<CardAnimationController>();
                }
            }
            
            Debug.Log("[Scene Setup] Complete UI Canvas created");
        }
        
        private void CreateCardPrefab()
        {
            string cardPrefabPath = CARDS_PATH + "/CardPrefab.prefab";
            
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cardPrefabPath);
            if (existingPrefab == null)
            {
                GameObject card = new GameObject("CardPrefab");
                
                // Add RectTransform for UI
                RectTransform rectTransform = card.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(200, 300);
                
                // Add CanvasGroup for fading
                card.AddComponent<CanvasGroup>();
                
                // Create card front
                GameObject cardFront = new GameObject("CardFront");
                cardFront.transform.SetParent(card.transform, false);
                Image frontImage = cardFront.AddComponent<Image>();
                frontImage.color = Color.white;
                
                RectTransform frontRect = cardFront.GetComponent<RectTransform>();
                frontRect.anchorMin = Vector2.zero;
                frontRect.anchorMax = Vector2.one;
                frontRect.sizeDelta = Vector2.zero;
                
                // Add rank and suit text
                GameObject rankText = new GameObject("RankText");
                rankText.transform.SetParent(cardFront.transform, false);
                TextMeshProUGUI rankTMP = rankText.AddComponent<TextMeshProUGUI>();
                rankTMP.text = "A";
                rankTMP.fontSize = 48;
                rankTMP.alignment = TextAlignmentOptions.Center;
                rankTMP.color = Color.black;
                
                RectTransform rankRect = rankText.GetComponent<RectTransform>();
                rankRect.anchorMin = new Vector2(0.5f, 0.5f);
                rankRect.anchorMax = new Vector2(0.5f, 0.5f);
                rankRect.sizeDelta = new Vector2(100, 100);
                
                // Create card back
                GameObject cardBack = new GameObject("CardBack");
                cardBack.transform.SetParent(card.transform, false);
                Image backImage = cardBack.AddComponent<Image>();
                backImage.color = new Color(0.2f, 0.2f, 0.5f); // Blue back
                
                RectTransform backRect = cardBack.GetComponent<RectTransform>();
                backRect.anchorMin = Vector2.zero;
                backRect.anchorMax = Vector2.one;
                backRect.sizeDelta = Vector2.zero;
                
                // Add CardViewController
                card.AddComponent<CardViewController>();
                
                // Save as prefab
                PrefabUtility.SaveAsPrefabAsset(card, cardPrefabPath);
                DestroyImmediate(card);
                
                Debug.Log("[Scene Setup] Card prefab created at: " + cardPrefabPath);
            }
        }
        
        private void GenerateCardSpritePlaceholders()
        {
            string[] suits = { "hearts", "diamonds", "clubs", "spades" };
            string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "jack", "queen", "king", "ace" };
            
            Debug.Log("[Scene Setup] Card sprite naming convention:");
            Debug.Log("Place card sprites in: Assets/Art/Cards/");
            Debug.Log("Naming format: rank_suit.png");
            Debug.Log("Examples:");
            
            foreach (string suit in suits)
            {
                foreach (string rank in ranks)
                {
                    string fileName = $"{rank}_{suit}.png";
                    if (rank == "2" || rank == "ace")
                    {
                        Debug.Log($"  • {fileName}");
                    }
                }
            }
            
            Debug.Log("Also add: card_back.png");
        }
        
        private void LinkAllComponents()
        {
            // Link UIManager references
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                UIManager uiManager = canvas.GetComponent<UIManager>();
                if (uiManager != null)
                {
                    // Use serialized property to set private fields
                    SerializedObject serializedUI = new SerializedObject(uiManager);
                    
                    LinkUIElement(serializedUI, "_playerScoreText", "PlayerScore");
                    LinkUIElement(serializedUI, "_opponentScoreText", "OpponentScore");
                    LinkUIElement(serializedUI, "_roundText", "RoundText");
                    LinkUIElement(serializedUI, "_gameStateText", "StateText");
                    LinkUIElement(serializedUI, "_warIndicator", "WarIndicator");
                    LinkUIElement(serializedUI, "_warText", "WarIndicator/WarText");
                    LinkUIElement(serializedUI, "_gameOverScreen", "GameOverScreen");
                    LinkUIElement(serializedUI, "_gameOverText", "GameOverScreen/GameOverText");
                    LinkUIElement(serializedUI, "_winnerText", "GameOverScreen/WinnerText");
                    
                    serializedUI.ApplyModifiedProperties();
                }
            }
            
            // Link CardAnimationController references
            GameObject gameBoard = GameObject.Find("GameBoard");
            if (gameBoard != null)
            {
                CardAnimationController animController = gameBoard.GetComponent<CardAnimationController>();
                if (animController != null)
                {
                    SerializedObject serializedAnim = new SerializedObject(animController);
                    
                    LinkTransform(serializedAnim, "_playerDeck", "PlayerDeck");
                    LinkTransform(serializedAnim, "_opponentDeck", "OpponentDeck");
                    LinkTransform(serializedAnim, "_playerPlayArea", "PlayerPlayArea");
                    LinkTransform(serializedAnim, "_opponentPlayArea", "OpponentPlayArea");
                    LinkTransform(serializedAnim, "_warPile", "WarPile");
                    
                    serializedAnim.ApplyModifiedProperties();
                }
            }
            
            // Create card pool container
            GameObject cardPoolContainer = GameObject.Find("CardPoolContainer");
            if (cardPoolContainer == null)
            {
                cardPoolContainer = new GameObject("CardPoolContainer");
            }
            
            Debug.Log("[Scene Setup] Components linked");
        }
        
        private void LinkUIElement(SerializedObject serializedObject, string propertyName, string gameObjectName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                GameObject go = GameObject.Find(gameObjectName);
                if (go != null)
                {
                    if (propertyName.Contains("Text"))
                    {
                        property.objectReferenceValue = go.GetComponent<TextMeshProUGUI>();
                    }
                    else
                    {
                        property.objectReferenceValue = go;
                    }
                }
            }
        }
        
        private void LinkTransform(SerializedObject serializedObject, string propertyName, string gameObjectName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                GameObject go = GameObject.Find(gameObjectName);
                if (go != null)
                {
                    property.objectReferenceValue = go.transform;
                }
            }
        }
        
        private GameObject FindOrCreate(string name, Transform parent)
        {
            Transform existing = parent != null ? parent.Find(name) : GameObject.Find(name)?.transform;
            
            if (existing != null)
                return existing.gameObject;
            
            GameObject newObj = new GameObject(name);
            if (parent != null)
                newObj.transform.SetParent(parent);
            
            return newObj;
        }
        
        private void AddVisualIndicator(GameObject go, string label, Color color)
        {
            // Add a simple sprite renderer for visualization
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = go.AddComponent<SpriteRenderer>();
            }
            
            sr.color = new Color(color.r, color.g, color.b, 0.3f);
            go.transform.localScale = new Vector3(2, 3, 1);
        }
        
        private void CreateUIPanel(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, 
            float width = -1, float height = -1)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null)
                rect = go.AddComponent<RectTransform>();
            
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            
            if (width > 0 && height > 0)
                rect.sizeDelta = new Vector2(width, height);
            
            Image img = go.GetComponent<Image>();
            if (img == null)
            {
                img = go.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0.8f);
            }
        }
        
        private GameObject CreateText(string name, Transform parent, string text,
            Vector2 anchor, Vector2 position, int fontSize)
        {
            GameObject textObj = FindOrCreate(name, parent);
            
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                tmp = textObj.AddComponent<TextMeshProUGUI>();
            
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300, 100);
            
            return textObj;
        }
    }
}