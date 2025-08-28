using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Zenject;

namespace CardWar.Editor
{
    public class WarGameSceneSetup : EditorWindow
    {
        private const string MENU_PATH = "CardWar/Setup Scene";
        
        [MenuItem(MENU_PATH)]
        public static void ShowWindow()
        {
            GetWindow<WarGameSceneSetup>("War Game Scene Setup");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("War Card Game - Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool will set up your scene with all required GameObjects and components for the War Card Game.",
                MessageType.Info);
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Setup Complete Scene", GUILayout.Height(40)))
            {
                SetupCompleteScene();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Individual Setup Options:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("1. Create Zenject Contexts"))
            {
                CreateZenjectContexts();
            }
            
            if (GUILayout.Button("2. Setup Camera"))
            {
                SetupCamera();
            }
            
            if (GUILayout.Button("3. Create Game Board"))
            {
                CreateGameBoard();
            }
            
            if (GUILayout.Button("4. Create UI Canvas"))
            {
                CreateUICanvas();
            }
            
            if (GUILayout.Button("5. Create Card Prefab"))
            {
                CreateCardPrefab();
            }
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "After setup, remember to:\n" +
                "1. Add card sprites to Assets/Art/Cards/\n" +
                "2. Configure NetworkErrorConfig in ProjectInstaller\n" +
                "3. Assign card prefab to any spawners",
                MessageType.Warning);
        }
        
        private void SetupCompleteScene()
        {
            CreateZenjectContexts();
            SetupCamera();
            CreateGameBoard();
            CreateUICanvas();
            CreateCardPrefab();
            
            Debug.Log("[Scene Setup] Complete scene setup finished!");
            EditorUtility.DisplayDialog("Setup Complete", 
                "Scene has been set up successfully!\n\n" +
                "Next steps:\n" +
                "1. Add card sprites\n" +
                "2. Configure settings\n" +
                "3. Press Play to test",
                "OK");
        }
        
        private void CreateZenjectContexts()
        {
            // Create SceneContext
            GameObject sceneContext = GameObject.Find("SceneContext");
            if (sceneContext == null)
            {
                sceneContext = new GameObject("SceneContext");
                var sceneContextComponent = sceneContext.AddComponent<SceneContext>();
                
                // TODO: Add GameInstaller to the installers list
                // This needs to be done manually as it requires the script reference
                Debug.LogWarning("[Scene Setup] SceneContext created. Please manually add GameInstaller to the Installers list!");
            }
            
            // Create ProjectContext prefab
            string resourcesPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            string projectContextPath = resourcesPath + "/ProjectContext.prefab";
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(projectContextPath))
            {
                GameObject projectContext = new GameObject("ProjectContext");
                var projectContextComponent = projectContext.AddComponent<ProjectContext>();
                
                // Create prefab
                PrefabUtility.SaveAsPrefabAsset(projectContext, projectContextPath);
                DestroyImmediate(projectContext);
                
                Debug.LogWarning("[Scene Setup] ProjectContext prefab created. Please manually add ProjectInstaller to the Installers list!");
            }
            
            Debug.Log("[Scene Setup] Zenject contexts created");
        }
        
        private void SetupCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
            }
            
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 10f;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.1f, 0.3f, 0.18f); // Dark green
            
            // Position camera
            mainCamera.transform.position = new Vector3(0, 0, -10);
            
            Debug.Log("[Scene Setup] Camera configured");
        }
        
        private void CreateGameBoard()
        {
            GameObject gameBoard = GameObject.Find("GameBoard");
            if (gameBoard == null)
            {
                gameBoard = new GameObject("GameBoard");
            }
            
            // Create Background Canvas
            GameObject backgroundCanvas = CreateOrFind("Background", gameBoard.transform);
            Canvas bgCanvas = backgroundCanvas.GetComponent<Canvas>();
            if (bgCanvas == null)
            {
                bgCanvas = backgroundCanvas.AddComponent<Canvas>();
                bgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                bgCanvas.worldCamera = Camera.main;
                
                var canvasScaler = backgroundCanvas.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1080, 1920);
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 0.5f;
                
                backgroundCanvas.AddComponent<GraphicRaycaster>();
                
                // Add background image
                GameObject bg = new GameObject("BackgroundImage");
                bg.transform.SetParent(backgroundCanvas.transform, false);
                var bgImage = bg.AddComponent<Image>();
                bgImage.color = new Color(0.15f, 0.4f, 0.22f);
                
                RectTransform bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
            }
            
            // Create Player Area
            GameObject playerArea = CreateOrFind("PlayerArea", gameBoard.transform);
            playerArea.transform.position = new Vector3(0, -5, 0);
            
            CreateOrFind("PlayerDeck", playerArea.transform).transform.localPosition = new Vector3(-3, 0, 0);
            CreateOrFind("PlayerPlayArea", playerArea.transform).transform.localPosition = new Vector3(0, 0, 0);
            
            // Create Opponent Area
            GameObject opponentArea = CreateOrFind("OpponentArea", gameBoard.transform);
            opponentArea.transform.position = new Vector3(0, 5, 0);
            
            CreateOrFind("OpponentDeck", opponentArea.transform).transform.localPosition = new Vector3(3, 0, 0);
            CreateOrFind("OpponentPlayArea", opponentArea.transform).transform.localPosition = new Vector3(0, 0, 0);
            
            // Create Center Area
            GameObject centerArea = CreateOrFind("CenterArea", gameBoard.transform);
            centerArea.transform.position = Vector3.zero;
            
            CreateOrFind("WarPile", centerArea.transform);
            CreateOrFind("BattleZone", centerArea.transform);
            
            Debug.Log("[Scene Setup] Game board created");
        }
        
        private void CreateUICanvas()
        {
            GameObject uiRoot = GameObject.Find("UI");
            if (uiRoot == null)
            {
                uiRoot = new GameObject("UI");
            }
            
            // Create main UI Canvas
            GameObject canvasObj = CreateOrFind("Canvas", uiRoot.transform);
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            
            if (canvas == null)
            {
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1080, 1920);
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 0.5f;
                
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // Create HUD
            GameObject hud = CreateOrFind("GameHUD", canvas.transform);
            
            // Score Display
            GameObject scoreDisplay = CreateOrFind("ScoreDisplay", hud.transform);
            if (scoreDisplay.GetComponent<RectTransform>() == null)
            {
                RectTransform scoreRect = scoreDisplay.AddComponent<RectTransform>();
                scoreRect.anchorMin = new Vector2(0, 1);
                scoreRect.anchorMax = new Vector2(1, 1);
                scoreRect.pivot = new Vector2(0.5f, 1);
                scoreRect.anchoredPosition = new Vector2(0, -50);
                scoreRect.sizeDelta = new Vector2(0, 100);
            }
            
            // Add text components for scores
            CreateTextElement("PlayerScore", scoreDisplay.transform, "Player: 26", 
                new Vector2(0, 0.5f), new Vector2(0.3f, 0.5f), new Vector2(-200, 0));
            
            CreateTextElement("OpponentScore", scoreDisplay.transform, "Opponent: 26", 
                new Vector2(0.7f, 0.5f), new Vector2(1, 0.5f), new Vector2(200, 0));
            
            // Round Counter
            GameObject roundCounter = CreateOrFind("RoundCounter", hud.transform);
            CreateTextElement("RoundText", roundCounter.transform, "Round: 0",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150));
            
            // Game State Indicator
            GameObject stateIndicator = CreateOrFind("GameStateIndicator", hud.transform);
            CreateTextElement("StateText", stateIndicator.transform, "Tap to Draw",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            
            // Create Popups
            GameObject popups = CreateOrFind("Popups", canvas.transform);
            
            // War Indicator
            GameObject warIndicator = CreateOrFind("WarIndicator", popups.transform);
            warIndicator.SetActive(false);
            CreateTextElement("WarText", warIndicator.transform, "WAR!",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 72);
            
            // Game Over Screen
            GameObject gameOverScreen = CreateOrFind("GameOverScreen", popups.transform);
            gameOverScreen.SetActive(false);
            
            CreateTextElement("GameOverText", gameOverScreen.transform, "Game Over",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 100), 48);
            
            CreateTextElement("WinnerText", gameOverScreen.transform, "Winner: Player",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), 36);
            
            Debug.Log("[Scene Setup] UI Canvas created");
        }
        
        private void CreateCardPrefab()
        {
            string prefabPath = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            
            string cardPath = prefabPath + "/Cards";
            if (!AssetDatabase.IsValidFolder(cardPath))
            {
                AssetDatabase.CreateFolder(prefabPath, "Cards");
            }
            
            string cardPrefabPath = cardPath + "/CardPrefab.prefab";
            
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(cardPrefabPath))
            {
                // Create card GameObject
                GameObject card = new GameObject("CardPrefab");
                
                // Add RectTransform
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
                
                // Create card back
                GameObject cardBack = new GameObject("CardBack");
                cardBack.transform.SetParent(card.transform, false);
                Image backImage = cardBack.AddComponent<Image>();
                backImage.color = new Color(0.2f, 0.2f, 0.5f); // Blue back
                
                RectTransform backRect = cardBack.GetComponent<RectTransform>();
                backRect.anchorMin = Vector2.zero;
                backRect.anchorMax = Vector2.one;
                backRect.sizeDelta = Vector2.zero;
                
                // TODO: Add CardViewController component when created
                
                // Save as prefab
                PrefabUtility.SaveAsPrefabAsset(card, cardPrefabPath);
                DestroyImmediate(card);
                
                Debug.Log("[Scene Setup] Card prefab created at: " + cardPrefabPath);
            }
        }
        
        private GameObject CreateOrFind(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.gameObject;
            
            GameObject newObj = new GameObject(name);
            newObj.transform.SetParent(parent);
            newObj.transform.localPosition = Vector3.zero;
            return newObj;
        }
        
        private void CreateTextElement(string name, Transform parent, string text, 
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, int fontSize = 24)
        {
            GameObject textObj = CreateOrFind(name, parent);
            
            TextMeshProUGUI tmpText = textObj.GetComponent<TextMeshProUGUI>();
            if (tmpText == null)
            {
                tmpText = textObj.AddComponent<TextMeshProUGUI>();
            }
            
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;
            
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(300, 50);
        }
    }
}