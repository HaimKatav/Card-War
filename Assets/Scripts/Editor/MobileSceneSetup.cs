using CardWar.Gameplay.Controllers;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using CardWar.UI;

namespace CardWar.Editor
{
    public class MobileSceneSetup : EditorWindow
    {
        private Camera _mainCamera;
        private Canvas _uiCanvas;
        private GameObject _gameBoard;
        
        [MenuItem("CardWar/Tools/Mobile Scene Setup")]
        public static void ShowWindow()
        {
            GetWindow<MobileSceneSetup>("Mobile Scene Setup");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Mobile Portrait Scene Setup (9:16)", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool will properly configure the scene for mobile portrait view (1080x1920).\n" +
                "It will fix camera, UI layout, and game board positions.",
                MessageType.Info);
            
            GUILayout.Space(10);
            
            // Find references
            _mainCamera = Camera.main;
            _uiCanvas = FindObjectOfType<Canvas>();
            _gameBoard = GameObject.Find("GameBoard");
            
            // Show current status
            EditorGUILayout.LabelField("Current Status:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Main Camera:", _mainCamera != null ? "✓ Found" : "✗ Not Found");
            EditorGUILayout.LabelField("UI Canvas:", _uiCanvas != null ? "✓ Found" : "✗ Not Found");
            EditorGUILayout.LabelField("Game Board:", _gameBoard != null ? "✓ Found" : "✗ Not Found");
            
            GUILayout.Space(20);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🎯 FIX COMPLETE SCENE LAYOUT", GUILayout.Height(50)))
            {
                if (EditorUtility.DisplayDialog("Fix Scene Layout",
                    "This will:\n" +
                    "• Configure camera for portrait view\n" +
                    "• Fix UI canvas and all UI positions\n" +
                    "• Properly position game board elements\n" +
                    "• Assign missing UI references\n\n" +
                    "Continue?",
                    "Yes, Fix Layout", "Cancel"))
                {
                    FixCompleteSceneLayout();
                }
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(20);
            GUILayout.Label("Individual Fixes:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("1. Fix Camera for Mobile"))
                FixCameraForMobile();
                
            if (GUILayout.Button("2. Fix UI Canvas"))
                FixUICanvas();
                
            if (GUILayout.Button("3. Fix Game Board Layout"))
                FixGameBoardLayout();
                
            if (GUILayout.Button("4. Assign UI References"))
                AssignUIReferences();
                
            if (GUILayout.Button("5. Create Missing UI Elements"))
                CreateMissingUIElements();
        }
        
        private void FixCompleteSceneLayout()
        {
            FixCameraForMobile();
            FixUICanvas();
            FixGameBoardLayout();
            CreateMissingUIElements();
            AssignUIReferences();
            
            EditorUtility.DisplayDialog("Success",
                "✅ Scene layout fixed for mobile portrait view!\n\n" +
                "The scene is now configured for 1080x1920 portrait display.\n" +
                "All UI references should be properly assigned.",
                "OK");
                
            Debug.Log("✅ [MobileSceneSetup] Complete scene layout fixed for mobile");
        }
        
        private void FixCameraForMobile()
        {
            if (_mainCamera == null)
            {
                GameObject camObj = GameObject.Find("Main Camera");
                if (camObj == null)
                {
                    camObj = new GameObject("Main Camera");
                    camObj.tag = "MainCamera";
                }
                _mainCamera = camObj.GetComponent<Camera>();
                if (_mainCamera == null)
                {
                    _mainCamera = camObj.AddComponent<Camera>();
                    camObj.AddComponent<AudioListener>();
                }
            }
            
            // Set camera for portrait mobile view
            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = 8f; // Smaller size for portrait
            _mainCamera.transform.position = new Vector3(0, 0, -10);
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = new Color(0.05f, 0.2f, 0.1f); // Dark green
            
            Debug.Log("[MobileSceneSetup] Camera configured for mobile portrait view");
        }
        
        private void FixUICanvas()
        {
            if (_uiCanvas == null)
            {
                GameObject canvasObj = GameObject.Find("Canvas");
                if (canvasObj == null)
                {
                    canvasObj = new GameObject("Canvas");
                }
                _uiCanvas = canvasObj.GetComponent<Canvas>();
                if (_uiCanvas == null)
                {
                    _uiCanvas = canvasObj.AddComponent<Canvas>();
                }
            }
            
            // Configure canvas for mobile
            _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _uiCanvas.sortingOrder = 10;
            
            // Fix Canvas Scaler for mobile
            CanvasScaler scaler = _uiCanvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = _uiCanvas.gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add Graphic Raycaster if missing
            GraphicRaycaster raycaster = _uiCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                _uiCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            
            Debug.Log("[MobileSceneSetup] UI Canvas configured for 1080x1920 mobile display");
        }
        
        private void FixGameBoardLayout()
        {
            if (_gameBoard == null)
            {
                _gameBoard = GameObject.Find("GameBoard");
                if (_gameBoard == null)
                {
                    _gameBoard = new GameObject("GameBoard");
                }
            }
            
            // Remove or hide the background sprite renderer
            Transform background = _gameBoard.transform.Find("Background");
            if (background != null)
            {
                // Keep it but make it invisible - we'll use UI background instead
                SpriteRenderer bgSprite = background.GetComponent<SpriteRenderer>();
                if (bgSprite != null)
                {
                    bgSprite.enabled = false;
                }
            }
            
            // Position game areas for portrait view
            Transform playerArea = _gameBoard.transform.Find("PlayerArea");
            if (playerArea == null)
            {
                playerArea = new GameObject("PlayerArea").transform;
                playerArea.SetParent(_gameBoard.transform);
            }
            playerArea.localPosition = new Vector3(0, -4.5f, 0);
            
            Transform opponentArea = _gameBoard.transform.Find("OpponentArea");
            if (opponentArea == null)
            {
                opponentArea = new GameObject("OpponentArea").transform;
                opponentArea.SetParent(_gameBoard.transform);
            }
            opponentArea.localPosition = new Vector3(0, 4.5f, 0);
            
            Transform centerArea = _gameBoard.transform.Find("CenterArea");
            if (centerArea == null)
            {
                centerArea = new GameObject("CenterArea").transform;
                centerArea.SetParent(_gameBoard.transform);
            }
            centerArea.localPosition = Vector3.zero;
            
            // Fix individual positions for portrait layout
            FixAreaPositions(playerArea, true);
            FixAreaPositions(opponentArea, false);
            FixCenterAreaPositions(centerArea);
            
            // Remove the visual indicators (colored sprites) - we'll use proper card graphics
            RemoveDebugVisuals(_gameBoard.transform);
            
            Debug.Log("[MobileSceneSetup] Game board layout fixed for portrait view");
        }
        
        private void FixAreaPositions(Transform area, bool isPlayer)
        {
            if (area == null) return;
            
            // Find or create deck position
            string deckName = isPlayer ? "PlayerDeck" : "OpponentDeck";
            Transform deck = area.Find(deckName);
            if (deck == null)
            {
                deck = new GameObject(deckName).transform;
                deck.SetParent(area);
            }
            deck.localPosition = new Vector3(-2.5f, 0, 0);
            
            // Find or create play area
            string playAreaName = isPlayer ? "PlayerPlayArea" : "OpponentPlayArea";
            Transform playArea = area.Find(playAreaName);
            if (playArea == null)
            {
                playArea = new GameObject(playAreaName).transform;
                playArea.SetParent(area);
            }
            playArea.localPosition = new Vector3(0.5f, 0, 0);
        }
        
        private void FixCenterAreaPositions(Transform centerArea)
        {
            if (centerArea == null) return;
            
            Transform warPile = centerArea.Find("WarPile");
            if (warPile == null)
            {
                warPile = new GameObject("WarPile").transform;
                warPile.SetParent(centerArea);
            }
            warPile.localPosition = new Vector3(0, 0, 0);
            
            Transform battleZone = centerArea.Find("BattleZone");
            if (battleZone != null)
            {
                // We don't really need battle zone, war pile is enough
                battleZone.gameObject.SetActive(false);
            }
        }
        
        private void RemoveDebugVisuals(Transform root)
        {
            // Remove all debug sprite renderers
            SpriteRenderer[] sprites = root.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sprite in sprites)
            {
                if (sprite.sprite == null) // These are our debug placeholders
                {
                    sprite.enabled = false;
                }
            }
        }
        
        private void CreateMissingUIElements()
        {
            if (_uiCanvas == null) return;
            
            // Ensure we have the proper UI structure
            Transform gameHUD = _uiCanvas.transform.Find("GameHUD");
            if (gameHUD == null)
            {
                gameHUD = new GameObject("GameHUD").transform;
                gameHUD.SetParent(_uiCanvas.transform);
                RectTransform hudRect = gameHUD.gameObject.AddComponent<RectTransform>();
                hudRect.anchorMin = Vector2.zero;
                hudRect.anchorMax = Vector2.one;
                hudRect.sizeDelta = Vector2.zero;
                hudRect.anchoredPosition = Vector2.zero;
            }
            
            // Create score display at top
            CreateScoreDisplay(gameHUD);
            
            // Create state indicator in middle
            CreateStateIndicator(gameHUD);
            
            // Create round counter
            CreateRoundCounter(gameHUD);
            
            // Create popups container
            Transform popups = _uiCanvas.transform.Find("Popups");
            if (popups == null)
            {
                popups = new GameObject("Popups").transform;
                popups.SetParent(_uiCanvas.transform);
                RectTransform popupsRect = popups.gameObject.AddComponent<RectTransform>();
                popupsRect.anchorMin = Vector2.zero;
                popupsRect.anchorMax = Vector2.one;
                popupsRect.sizeDelta = Vector2.zero;
                popupsRect.anchoredPosition = Vector2.zero;
            }
            
            CreateWarIndicator(popups);
            CreateGameOverScreen(popups);
            
            Debug.Log("[MobileSceneSetup] UI elements created/verified");
        }
        
        private void CreateScoreDisplay(Transform parent)
        {
            Transform scoreDisplay = parent.Find("ScoreDisplay");
            if (scoreDisplay == null)
            {
                scoreDisplay = new GameObject("ScoreDisplay").transform;
                scoreDisplay.SetParent(parent);
            }
            
            RectTransform scoreRect = scoreDisplay.GetComponent<RectTransform>();
            if (scoreRect == null)
            {
                scoreRect = scoreDisplay.gameObject.AddComponent<RectTransform>();
            }
            scoreRect.anchorMin = new Vector2(0, 1);
            scoreRect.anchorMax = new Vector2(1, 1);
            scoreRect.pivot = new Vector2(0.5f, 1);
            scoreRect.anchoredPosition = new Vector2(0, -50);
            scoreRect.sizeDelta = new Vector2(0, 150);
            
            // Add background
            Image bgImage = scoreDisplay.GetComponent<Image>();
            if (bgImage == null)
            {
                bgImage = scoreDisplay.gameObject.AddComponent<Image>();
                bgImage.color = new Color(0, 0, 0, 0.5f);
            }
            
            // Create player score
            Transform playerScore = scoreDisplay.Find("PlayerScore");
            if (playerScore == null)
            {
                playerScore = CreateText("PlayerScore", scoreDisplay, "Player: 26", 
                    new Vector2(0, 0.5f), new Vector2(150, 0), 42).transform;
            }
            
            // Create opponent score
            Transform opponentScore = scoreDisplay.Find("OpponentScore");
            if (opponentScore == null)
            {
                opponentScore = CreateText("OpponentScore", scoreDisplay, "Opponent: 26",
                    new Vector2(1, 0.5f), new Vector2(-150, 0), 42).transform;
            }
        }
        
        private void CreateStateIndicator(Transform parent)
        {
            Transform stateIndicator = parent.Find("GameStateIndicator");
            if (stateIndicator == null)
            {
                stateIndicator = new GameObject("GameStateIndicator").transform;
                stateIndicator.SetParent(parent);
            }
            
            RectTransform stateRect = stateIndicator.GetComponent<RectTransform>();
            if (stateRect == null)
            {
                stateRect = stateIndicator.gameObject.AddComponent<RectTransform>();
            }
            stateRect.anchorMin = new Vector2(0.5f, 0.5f);
            stateRect.anchorMax = new Vector2(0.5f, 0.5f);
            stateRect.anchoredPosition = Vector2.zero;
            stateRect.sizeDelta = new Vector2(500, 150);
            
            Transform stateText = stateIndicator.Find("StateText");
            if (stateText == null)
            {
                stateText = CreateText("StateText", stateIndicator, "Tap to Draw",
                    new Vector2(0.5f, 0.5f), Vector2.zero, 56).transform;
            }
        }
        
        private void CreateRoundCounter(Transform parent)
        {
            Transform roundCounter = parent.Find("RoundCounter");
            if (roundCounter == null)
            {
                roundCounter = new GameObject("RoundCounter").transform;
                roundCounter.SetParent(parent);
            }
            
            RectTransform roundRect = roundCounter.GetComponent<RectTransform>();
            if (roundRect == null)
            {
                roundRect = roundCounter.gameObject.AddComponent<RectTransform>();
            }
            roundRect.anchorMin = new Vector2(0.5f, 1);
            roundRect.anchorMax = new Vector2(0.5f, 1);
            roundRect.anchoredPosition = new Vector2(0, -220);
            roundRect.sizeDelta = new Vector2(300, 60);
            
            Transform roundText = roundCounter.Find("RoundText");
            if (roundText == null)
            {
                roundText = CreateText("RoundText", roundCounter, "Round: 0",
                    new Vector2(0.5f, 0.5f), Vector2.zero, 32).transform;
            }
        }
        
        private void CreateWarIndicator(Transform parent)
        {
            Transform warIndicator = parent.Find("WarIndicator");
            if (warIndicator == null)
            {
                warIndicator = new GameObject("WarIndicator").transform;
                warIndicator.SetParent(parent);
                warIndicator.gameObject.SetActive(false);
            }
            
            RectTransform warRect = warIndicator.GetComponent<RectTransform>();
            if (warRect == null)
            {
                warRect = warIndicator.gameObject.AddComponent<RectTransform>();
            }
            warRect.anchorMin = new Vector2(0.5f, 0.5f);
            warRect.anchorMax = new Vector2(0.5f, 0.5f);
            warRect.anchoredPosition = Vector2.zero;
            warRect.sizeDelta = new Vector2(600, 300);
            
            Image bgImage = warIndicator.GetComponent<Image>();
            if (bgImage == null)
            {
                bgImage = warIndicator.gameObject.AddComponent<Image>();
                bgImage.color = new Color(0.8f, 0, 0, 0.9f);
            }
            
            Transform warText = warIndicator.Find("WarText");
            if (warText == null)
            {
                warText = CreateText("WarText", warIndicator, "WAR!",
                    new Vector2(0.5f, 0.5f), Vector2.zero, 96).transform;
                warText.GetComponent<TextMeshProUGUI>().color = Color.white;
            }
        }
        
        private void CreateGameOverScreen(Transform parent)
        {
            Transform gameOverScreen = parent.Find("GameOverScreen");
            if (gameOverScreen == null)
            {
                gameOverScreen = new GameObject("GameOverScreen").transform;
                gameOverScreen.SetParent(parent);
                gameOverScreen.gameObject.SetActive(false);
            }
            
            RectTransform overRect = gameOverScreen.GetComponent<RectTransform>();
            if (overRect == null)
            {
                overRect = gameOverScreen.gameObject.AddComponent<RectTransform>();
            }
            overRect.anchorMin = Vector2.zero;
            overRect.anchorMax = Vector2.one;
            overRect.sizeDelta = Vector2.zero;
            overRect.anchoredPosition = Vector2.zero;
            
            Image bgImage = gameOverScreen.GetComponent<Image>();
            if (bgImage == null)
            {
                bgImage = gameOverScreen.gameObject.AddComponent<Image>();
                bgImage.color = new Color(0, 0, 0, 0.8f);
            }
            
            Transform gameOverText = gameOverScreen.Find("GameOverText");
            if (gameOverText == null)
            {
                gameOverText = CreateText("GameOverText", gameOverScreen, "Game Over",
                    new Vector2(0.5f, 0.6f), Vector2.zero, 72).transform;
            }
            
            Transform winnerText = gameOverScreen.Find("WinnerText");
            if (winnerText == null)
            {
                winnerText = CreateText("WinnerText", gameOverScreen, "Winner: Player",
                    new Vector2(0.5f, 0.4f), Vector2.zero, 56).transform;
            }
        }
        
        private GameObject CreateText(string name, Transform parent, string text,
            Vector2 anchor, Vector2 position, int fontSize)
        {
            Transform existing = parent.Find(name);
            GameObject textObj = existing != null ? existing.gameObject : new GameObject(name);
            
            if (existing == null)
                textObj.transform.SetParent(parent);
            
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
            rect.sizeDelta = new Vector2(400, 100);
            
            return textObj;
        }
        
        private void AssignUIReferences()
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[MobileSceneSetup] Canvas not found!");
                return;
            }
            
            // Find all UIManager components (there might be duplicates)
            UIManager[] uiManagers = canvas.GetComponents<UIManager>();
            UIManager uiManager = null;
            
            if (uiManagers.Length > 1)
            {
                Debug.LogWarning($"[MobileSceneSetup] Found {uiManagers.Length} UIManager components, using first one");
                uiManager = uiManagers[0];
            }
            else if (uiManagers.Length == 1)
            {
                uiManager = uiManagers[0];
            }
            
            if (uiManager == null)
            {
                Debug.LogError("[MobileSceneSetup] UIManager not found on Canvas!");
                return;
            }
            
            SerializedObject serializedUI = new SerializedObject(uiManager);
            
            // Assign all references
            AssignReference(serializedUI, "_playerScoreText", "GameHUD/ScoreDisplay/PlayerScore");
            AssignReference(serializedUI, "_opponentScoreText", "GameHUD/ScoreDisplay/OpponentScore");
            AssignReference(serializedUI, "_roundText", "GameHUD/RoundCounter/RoundText");
            AssignReference(serializedUI, "_gameStateText", "GameHUD/GameStateIndicator/StateText");
            AssignReference(serializedUI, "_warIndicator", "Popups/WarIndicator");
            AssignReference(serializedUI, "_warText", "Popups/WarIndicator/WarText");
            AssignReference(serializedUI, "_gameOverScreen", "Popups/GameOverScreen");
            AssignReference(serializedUI, "_gameOverText", "Popups/GameOverScreen/GameOverText");
            AssignReference(serializedUI, "_winnerText", "Popups/GameOverScreen/WinnerText");
            
            serializedUI.ApplyModifiedProperties();
            
            Debug.Log("[MobileSceneSetup] UI references assigned");
            
            // Also fix CardAnimationController references
            GameObject gameBoard = GameObject.Find("GameBoard");
            if (gameBoard != null)
            {
                CardAnimationController animController = gameBoard.GetComponent<CardAnimationController>();
                if (animController != null)
                {
                    SerializedObject serializedAnim = new SerializedObject(animController);
                    
                    AssignTransformReference(serializedAnim, "_playerDeck", "GameBoard/PlayerArea/PlayerDeck");
                    AssignTransformReference(serializedAnim, "_opponentDeck", "GameBoard/OpponentArea/OpponentDeck");
                    AssignTransformReference(serializedAnim, "_playerPlayArea", "GameBoard/PlayerArea/PlayerPlayArea");
                    AssignTransformReference(serializedAnim, "_opponentPlayArea", "GameBoard/OpponentArea/OpponentPlayArea");
                    AssignTransformReference(serializedAnim, "_warPile", "GameBoard/CenterArea/WarPile");
                    
                    serializedAnim.ApplyModifiedProperties();
                    
                    Debug.Log("[MobileSceneSetup] CardAnimationController references assigned");
                }
            }
        }
        
        private void AssignReference(SerializedObject serializedObject, string propertyName, string path)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                GameObject go = GameObject.Find(path);
                if (go != null)
                {
                    if (propertyName.Contains("Text"))
                    {
                        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            property.objectReferenceValue = tmp;
                            Debug.Log($"[MobileSceneSetup] Assigned {propertyName} to {path}");
                        }
                        else
                        {
                            Debug.LogWarning($"[MobileSceneSetup] TextMeshProUGUI not found on {path}");
                        }
                    }
                    else
                    {
                        property.objectReferenceValue = go;
                        Debug.Log($"[MobileSceneSetup] Assigned {propertyName} to {path}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[MobileSceneSetup] GameObject not found at path: {path}");
                }
            }
            else
            {
                Debug.LogWarning($"[MobileSceneSetup] Property not found: {propertyName}");
            }
        }
        
        private void AssignTransformReference(SerializedObject serializedObject, string propertyName, string path)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                GameObject go = GameObject.Find(path);
                if (go != null)
                {
                    property.objectReferenceValue = go.transform;
                    Debug.Log($"[MobileSceneSetup] Assigned {propertyName} to {path}");
                }
                else
                {
                    Debug.LogWarning($"[MobileSceneSetup] GameObject not found at path: {path}");
                }
            }
            else
            {
                Debug.LogWarning($"[MobileSceneSetup] Property not found: {propertyName}");
            }
        }
    }
}