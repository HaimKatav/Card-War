#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace CardWar.Editor
{
    public class SceneSetupEditor : EditorWindow
    {
        private Canvas mainCanvas;
        private CanvasScaler canvasScaler;
        
        [MenuItem("Card War/Setup Complete Scene")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupEditor>("Scene Setup");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("War Card Game - Complete Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("1. Setup Canvas and UI", GUILayout.Height(30)))
            {
                SetupCanvas();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("2. Setup Game Board Layout", GUILayout.Height(30)))
            {
                SetupGameBoard();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("3. Setup Card Positions", GUILayout.Height(30)))
            {
                SetupCardPositions();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("4. Setup UI Elements", GUILayout.Height(30)))
            {
                SetupUIElements();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("SETUP EVERYTHING", GUILayout.Height(40)))
            {
                SetupCanvas();
                SetupGameBoard();
                SetupCardPositions();
                SetupUIElements();
                Debug.Log("[Scene Setup] Complete setup finished!");
            }
        }
        
        private void SetupCanvas()
        {
            // Find or create main canvas
            mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                GameObject canvasGO = new GameObject("MainCanvas");
                mainCanvas = canvasGO.AddComponent<Canvas>();
            }
            
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // Setup Canvas Scaler for mobile
            canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
            if (canvasScaler == null)
            {
                canvasScaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
            }
            
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920); // Portrait mode
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f; // Balance between width and height
            
            // Add Graphic Raycaster
            if (!mainCanvas.GetComponent<GraphicRaycaster>())
            {
                mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // Setup EventSystem
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            Debug.Log("[Scene Setup] Canvas configured for mobile portrait mode");
        }
        
        private void SetupGameBoard()
        {
            if (mainCanvas == null)
            {
                Debug.LogError("Please setup canvas first!");
                return;
            }
            
            // Create main game board container
            GameObject gameBoard = CreateOrFind("GameBoard", mainCanvas.transform);
            RectTransform boardRect = gameBoard.GetComponent<RectTransform>();
            
            // Full screen setup
            boardRect.anchorMin = Vector2.zero;
            boardRect.anchorMax = Vector2.one;
            boardRect.sizeDelta = Vector2.zero;
            boardRect.anchoredPosition = Vector2.zero;
            
            // Add green background
            Image boardImage = gameBoard.GetComponent<Image>();
            if (boardImage == null)
            {
                boardImage = gameBoard.AddComponent<Image>();
            }
            boardImage.color = new Color(0.15f, 0.5f, 0.25f, 1f); // Green felt color
            
            // Create three main sections
            CreateScoreSection("OpponentSection", gameBoard.transform, true);
            CreatePlayArea("PlayArea", gameBoard.transform);
            CreateScoreSection("PlayerSection", gameBoard.transform, false);
            
            Debug.Log("[Scene Setup] Game board layout created");
        }
        
        private void CreateScoreSection(string name, Transform parent, bool isOpponent)
        {
            GameObject section = CreateOrFind(name, parent);
            RectTransform rect = section.GetComponent<RectTransform>();
            
            // Position at top or bottom
            if (isOpponent)
            {
                rect.anchorMin = new Vector2(0, 0.85f);
                rect.anchorMax = new Vector2(1, 1);
            }
            else
            {
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(1, 0.15f);
            }
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            
            // Add semi-transparent background
            Image bg = section.GetComponent<Image>();
            if (bg == null)
            {
                bg = section.AddComponent<Image>();
            }
            bg.color = new Color(0, 0, 0, 0.3f);
            
            // Create score display
            GameObject scoreDisplay = CreateOrFind("ScoreDisplay", section.transform);
            RectTransform scoreRect = scoreDisplay.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.3f, 0.2f);
            scoreRect.anchorMax = new Vector2(0.7f, 0.8f);
            scoreRect.sizeDelta = Vector2.zero;
            scoreRect.anchoredPosition = Vector2.zero;
            
            // Add text
            TextMeshProUGUI scoreText = scoreDisplay.GetComponent<TextMeshProUGUI>();
            if (scoreText == null)
            {
                scoreText = scoreDisplay.AddComponent<TextMeshProUGUI>();
            }
            
            scoreText.text = isOpponent ? "Opponent: 26" : "Player: 26";
            scoreText.fontSize = 36;
            scoreText.color = Color.white;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.fontStyle = FontStyles.Bold;
            
            // Add round counter for opponent section
            if (isOpponent)
            {
                GameObject roundDisplay = CreateOrFind("RoundDisplay", section.transform);
                RectTransform roundRect = roundDisplay.GetComponent<RectTransform>();
                roundRect.anchorMin = new Vector2(0.05f, 0.2f);
                roundRect.anchorMax = new Vector2(0.25f, 0.8f);
                roundRect.sizeDelta = Vector2.zero;
                roundRect.anchoredPosition = Vector2.zero;
                
                TextMeshProUGUI roundText = roundDisplay.GetComponent<TextMeshProUGUI>();
                if (roundText == null)
                {
                    roundText = roundDisplay.AddComponent<TextMeshProUGUI>();
                }
                
                roundText.text = "Round: 0";
                roundText.fontSize = 24;
                roundText.color = Color.white;
                roundText.alignment = TextAlignmentOptions.Center;
            }
        }
        
        private void CreatePlayArea(string name, Transform parent)
        {
            GameObject playArea = CreateOrFind(name, parent);
            RectTransform rect = playArea.GetComponent<RectTransform>();
            
            // Center area between score sections
            rect.anchorMin = new Vector2(0, 0.15f);
            rect.anchorMax = new Vector2(1, 0.85f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            
            // Create TAP TO START button
            GameObject tapButton = CreateOrFind("TapToStartButton", playArea.transform);
            RectTransform buttonRect = tapButton.GetComponent<RectTransform>();
            
            // Center the button
            buttonRect.anchorMin = new Vector2(0.25f, 0.45f);
            buttonRect.anchorMax = new Vector2(0.75f, 0.55f);
            buttonRect.sizeDelta = Vector2.zero;
            buttonRect.anchoredPosition = Vector2.zero;
            
            Button button = tapButton.GetComponent<Button>();
            if (button == null)
            {
                button = tapButton.AddComponent<Button>();
            }
            
            Image buttonImage = tapButton.GetComponent<Image>();
            if (buttonImage == null)
            {
                buttonImage = tapButton.AddComponent<Image>();
            }
            buttonImage.color = new Color(0.2f, 0.5f, 1f, 1f);
            
            // Button text
            GameObject buttonTextGO = CreateOrFind("Text", tapButton.transform);
            RectTransform textRect = buttonTextGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI buttonText = buttonTextGO.GetComponent<TextMeshProUGUI>();
            if (buttonText == null)
            {
                buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
            }
            buttonText.text = "TAP TO START";
            buttonText.fontSize = 32;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontStyle = FontStyles.Bold;
            
            // Create game state text
            GameObject stateText = CreateOrFind("GameStateText", playArea.transform);
            RectTransform stateRect = stateText.GetComponent<RectTransform>();
            stateRect.anchorMin = new Vector2(0.2f, 0.6f);
            stateRect.anchorMax = new Vector2(0.8f, 0.65f);
            stateRect.sizeDelta = Vector2.zero;
            stateRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI stateDisplay = stateText.GetComponent<TextMeshProUGUI>();
            if (stateDisplay == null)
            {
                stateDisplay = stateText.AddComponent<TextMeshProUGUI>();
            }
            stateDisplay.text = "Tap to Draw";
            stateDisplay.fontSize = 28;
            stateDisplay.color = new Color(1f, 1f, 0.5f, 1f);
            stateDisplay.alignment = TextAlignmentOptions.Center;
        }
        
        private void SetupCardPositions()
        {
            GameObject playArea = GameObject.Find("PlayArea");
            if (playArea == null)
            {
                Debug.LogError("Please setup game board first!");
                return;
            }
            
            // Create card positions container
            GameObject cardPositions = CreateOrFind("CardPositions", playArea.transform);
            RectTransform containerRect = cardPositions.GetComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;
            containerRect.anchoredPosition = Vector2.zero;
            
            // Opponent card position (top center)
            CreateCardSlot("OpponentCardPosition", cardPositions.transform, 
                new Vector2(0.35f, 0.65f), new Vector2(0.65f, 0.8f));
            
            // Player card position (bottom center)
            CreateCardSlot("PlayerCardPosition", cardPositions.transform, 
                new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.35f));
            
            // War pile position (center)
            CreateCardSlot("WarPilePosition", cardPositions.transform, 
                new Vector2(0.4f, 0.425f), new Vector2(0.6f, 0.575f));
            
            // Deck position (off-screen left)
            GameObject deckPos = CreateOrFind("DeckPosition", cardPositions.transform);
            RectTransform deckRect = deckPos.GetComponent<RectTransform>();
            deckRect.anchorMin = new Vector2(-0.2f, 0.45f);
            deckRect.anchorMax = new Vector2(-0.05f, 0.55f);
            deckRect.sizeDelta = Vector2.zero;
            deckRect.anchoredPosition = Vector2.zero;
            
            Debug.Log("[Scene Setup] Card positions created");
        }
        
        private void CreateCardSlot(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject slot = CreateOrFind(name, parent);
            RectTransform rect = slot.GetComponent<RectTransform>();
            
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            
            // Add visual indicator (semi-transparent white)
            Image slotImage = slot.GetComponent<Image>();
            if (slotImage == null)
            {
                slotImage = slot.AddComponent<Image>();
            }
            slotImage.color = new Color(1, 1, 1, 0.1f);
        }
        
        private void SetupUIElements()
        {
            GameObject gameBoard = GameObject.Find("GameBoard");
            if (gameBoard == null)
            {
                Debug.LogError("Please setup game board first!");
                return;
            }
            
            // Create UI overlay container
            GameObject uiOverlay = CreateOrFind("UIOverlay", gameBoard.transform);
            RectTransform overlayRect = uiOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            overlayRect.anchoredPosition = Vector2.zero;
            
            // War indicator (hidden by default)
            GameObject warIndicator = CreateOrFind("WarIndicator", uiOverlay.transform);
            RectTransform warRect = warIndicator.GetComponent<RectTransform>();
            warRect.anchorMin = new Vector2(0.3f, 0.45f);
            warRect.anchorMax = new Vector2(0.7f, 0.55f);
            warRect.sizeDelta = Vector2.zero;
            warRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI warText = warIndicator.GetComponent<TextMeshProUGUI>();
            if (warText == null)
            {
                warText = warIndicator.AddComponent<TextMeshProUGUI>();
            }
            warText.text = "WAR!";
            warText.fontSize = 72;
            warText.color = new Color(1f, 0.2f, 0.2f, 1f);
            warText.alignment = TextAlignmentOptions.Center;
            warText.fontStyle = FontStyles.Bold;
            
            warIndicator.SetActive(false);
            
            // Game Over Panel (hidden by default)
            CreateGameOverPanel(uiOverlay.transform);
            
            // Card Pool Container
            GameObject cardPool = CreateOrFind("CardPoolContainer", gameBoard.transform);
            cardPool.SetActive(true);
            
            Debug.Log("[Scene Setup] UI elements configured");
        }
        
        private void CreateGameOverPanel(Transform parent)
        {
            GameObject panel = CreateOrFind("GameOverPanel", parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            
            rect.anchorMin = new Vector2(0.1f, 0.3f);
            rect.anchorMax = new Vector2(0.9f, 0.7f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            
            Image panelBg = panel.GetComponent<Image>();
            if (panelBg == null)
            {
                panelBg = panel.AddComponent<Image>();
            }
            panelBg.color = new Color(0, 0, 0, 0.9f);
            
            // Title
            GameObject titleGO = CreateOrFind("Title", panel.transform);
            RectTransform titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.6f);
            titleRect.anchorMax = new Vector2(0.9f, 0.85f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI titleText = titleGO.GetComponent<TextMeshProUGUI>();
            if (titleText == null)
            {
                titleText = titleGO.AddComponent<TextMeshProUGUI>();
            }
            titleText.text = "GAME OVER";
            titleText.fontSize = 48;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
            
            // Winner text
            GameObject winnerGO = CreateOrFind("WinnerText", panel.transform);
            RectTransform winnerRect = winnerGO.GetComponent<RectTransform>();
            winnerRect.anchorMin = new Vector2(0.1f, 0.35f);
            winnerRect.anchorMax = new Vector2(0.9f, 0.55f);
            winnerRect.sizeDelta = Vector2.zero;
            winnerRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI winnerText = winnerGO.GetComponent<TextMeshProUGUI>();
            if (winnerText == null)
            {
                winnerText = winnerGO.AddComponent<TextMeshProUGUI>();
            }
            winnerText.text = "You Win!";
            winnerText.fontSize = 36;
            winnerText.color = new Color(0.2f, 1f, 0.2f, 1f);
            winnerText.alignment = TextAlignmentOptions.Center;
            
            // Restart button
            GameObject restartButton = CreateOrFind("RestartButton", panel.transform);
            RectTransform buttonRect = restartButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.25f, 0.1f);
            buttonRect.anchorMax = new Vector2(0.75f, 0.25f);
            buttonRect.sizeDelta = Vector2.zero;
            buttonRect.anchoredPosition = Vector2.zero;
            
            Button button = restartButton.GetComponent<Button>();
            if (button == null)
            {
                button = restartButton.AddComponent<Button>();
            }
            
            Image buttonImage = restartButton.GetComponent<Image>();
            if (buttonImage == null)
            {
                buttonImage = restartButton.AddComponent<Image>();
            }
            buttonImage.color = new Color(0.2f, 0.5f, 1f, 1f);
            
            GameObject buttonTextGO = CreateOrFind("Text", restartButton.transform);
            TextMeshProUGUI buttonText = buttonTextGO.GetComponent<TextMeshProUGUI>();
            if (buttonText == null)
            {
                buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
            }
            buttonText.text = "PLAY AGAIN";
            buttonText.fontSize = 24;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            panel.SetActive(false);
        }
        
        private GameObject CreateOrFind(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }
            
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = go.AddComponent<RectTransform>();
            }
            
            return go;
        }
    }
}
#endif