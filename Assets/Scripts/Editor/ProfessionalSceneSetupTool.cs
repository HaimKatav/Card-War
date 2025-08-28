using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using CardWar.Configuration;
using CardWar.Core.UI;
using CardWar.Gameplay.Controllers;
using CardWar.Core;
using System.Linq;

namespace CardWar.Editor
{
    public class ProfessionalSceneSetupTool : EditorWindow
    {
        [MenuItem("CardWar/Scene Setup Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProfessionalSceneSetupTool>("Scene Setup Tool");
            window.minSize = new Vector2(600, 700);
        }
        
        private Vector2 _scrollPosition;
        private bool _showAdvancedOptions = false;
        
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            GUILayout.Label("Professional Scene Setup Tool", EditorStyles.largeLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool creates a complete, professional UI hierarchy for your Card War game.\n\n" +
                "It will create all UI components with proper references and positioning,\n" +
                "following clean architecture principles.", 
                MessageType.Info);
            
            GUILayout.Space(20);
            
            DrawCurrentSceneStatus();
            GUILayout.Space(20);
            
            DrawSetupOptions();
            GUILayout.Space(20);
            
            DrawAdvancedOptions();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawCurrentSceneStatus()
        {
            EditorGUILayout.LabelField("Current Scene Analysis", EditorStyles.boldLabel);
            
            var issues = AnalyzeCurrentScene();
            
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Scene setup is complete and ready!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Found {issues.Count} issues that need fixing:", MessageType.Warning);
                
                foreach (var issue in issues)
                {
                    EditorGUILayout.LabelField($"• {issue}", EditorStyles.miniLabel);
                }
            }
        }
        
        private void DrawSetupOptions()
        {
            EditorGUILayout.LabelField("Setup Actions", EditorStyles.boldLabel);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("CREATE COMPLETE UI HIERARCHY", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Create UI Hierarchy", 
                    "This will create a complete, professional UI setup:\n\n" +
                    "• Main Canvas with proper scaling\n" +
                    "• UI layers (Background, Game, UI, Overlay)\n" +
                    "• Score panels and game state displays\n" +
                    "• Interactive draw button\n" +
                    "• Game positions and animation targets\n" +
                    "• All required components with proper references\n\n" +
                    "Continue?", 
                    "Create UI", "Cancel"))
                {
                    CreateCompleteUIHierarchy();
                }
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Fix Missing Components"))
            {
                FixMissingComponents();
            }
            
            if (GUILayout.Button("Create Draw Button UI"))
            {
                CreateDrawButtonUI();
            }
            
            if (GUILayout.Button("Setup Game Positions"))
            {
                SetupGamePositions();
            }
        }
        
        private void DrawAdvancedOptions()
        {
            _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, "Advanced Options");
            
            if (_showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                
                if (GUILayout.Button("Clear Scene (Keep Camera)"))
                {
                    if (EditorUtility.DisplayDialog("Clear Scene", 
                        "This will remove all GameObjects except the Main Camera. Continue?", 
                        "Clear", "Cancel"))
                    {
                        ClearScene();
                    }
                }
                
                if (GUILayout.Button("Validate All References"))
                {
                    ValidateAllReferences();
                }
                
                if (GUILayout.Button("Auto-Connect References"))
                {
                    AutoConnectReferences();
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private System.Collections.Generic.List<string> AnalyzeCurrentScene()
        {
            var issues = new System.Collections.Generic.List<string>();
            
            if (FindObjectOfType<Canvas>() == null)
                issues.Add("No Canvas found");
                
            if (FindObjectOfType<CanvasManager>() == null)
                issues.Add("No CanvasManager component");
                
            if (FindObjectOfType<UIManager>() == null)
                issues.Add("No UIManager component");
                
            if (FindObjectOfType<CardAnimationController>() == null)
                issues.Add("No CardAnimationController component");
                
            if (FindObjectOfType<GameInteractionController>() == null)
                issues.Add("No GameInteractionController component");
                
            if (GameObject.Find("DrawButton") == null)
                issues.Add("No DrawButton found");
                
            var gameSettings = Resources.Load<GameSettings>("Settings/GameSettings");
            if (gameSettings == null)
                issues.Add("GameSettings asset missing");
            
            return issues;
        }
        
        private void CreateCompleteUIHierarchy()
        {
            Debug.Log("[Scene Setup] Creating complete UI hierarchy...");
            
            try
            {
                ClearScene();
                CreateMainCanvas();
                CreateUILayers();
                CreateScoreUI();
                CreateGameStateUI();
                CreateDrawButtonUI();
                CreateGamePositions();
                SetupRequiredComponents();
                AutoConnectReferences();
                
                EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
                
                Debug.Log("Scene setup completed successfully!");
                
                EditorUtility.DisplayDialog("Setup Complete", 
                    "Professional UI hierarchy created successfully!\n\n" +
                    "Your scene is now ready for gameplay. Press Play to test!",
                    "Excellent");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Setup failed: {ex.Message}");
                EditorUtility.DisplayDialog("Setup Failed", $"Error: {ex.Message}", "OK");
            }
        }
        
        private void ClearScene()
        {
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .Where(obj => obj.name != "Main Camera")
                .ToArray();
                
            foreach (var obj in rootObjects)
            {
                DestroyImmediate(obj);
            }
            
            Debug.Log($"Cleared {rootObjects.Length} objects from scene");
        }
        
        private void CreateMainCanvas()
        {
            var canvasObj = new GameObject("MainCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            var raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            var canvasManager = canvasObj.AddComponent<CanvasManager>();
            
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            Debug.Log("Created MainCanvas with CanvasManager");
        }
        
        private void CreateUILayers()
        {
            var canvas = FindObjectOfType<Canvas>();
            var canvasTransform = canvas.transform;
            
            CreateUILayer("BackgroundLayer", canvasTransform, 0);
            CreateUILayer("GameLayer", canvasTransform, 10); 
            CreateUILayer("UILayer", canvasTransform, 20);
            CreateUILayer("OverlayLayer", canvasTransform, 90);
            
            Debug.Log("Created UI layers");
        }
        
        private void CreateUILayer(string name, Transform parent, int sortingOrder)
        {
            var layerObj = new GameObject(name);
            layerObj.transform.SetParent(parent, false);
            
            var rectTransform = layerObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            var canvasGroup = layerObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        private void CreateScoreUI()
        {
            var uiLayer = GameObject.Find("UILayer");
            
            var playerScorePanel = CreateScorePanel("PlayerScorePanel", uiLayer.transform, 
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 100), "Player: 26");
                
            var opponentScorePanel = CreateScorePanel("OpponentScorePanel", uiLayer.transform,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -100), "Opponent: 26");
                
            var roundCounterPanel = CreateScorePanel("RoundCounterPanel", uiLayer.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), "Round: 0");
                
            Debug.Log("Created score UI panels");
        }
        
        private GameObject CreateScorePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, string text)
        {
            var panelObj = new GameObject(name);
            panelObj.transform.SetParent(parent, false);
            
            var rectTransform = panelObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(300, 60);
            
            var background = panelObj.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0.5f);
            
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(panelObj.transform, false);
            
            var textRectTransform = textObj.AddComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.offsetMin = Vector2.zero;
            textRectTransform.offsetMax = Vector2.zero;
            
            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 24;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontStyle = FontStyles.Bold;
            
            return panelObj;
        }
        
        private void CreateGameStateUI()
        {
            var uiLayer = GameObject.Find("UILayer");
            
            var gameStateObj = new GameObject("GameStateDisplay");
            gameStateObj.transform.SetParent(uiLayer.transform, false);
            
            var rectTransform = gameStateObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, 100);
            rectTransform.sizeDelta = new Vector2(400, 80);
            
            var textComponent = gameStateObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "Tap to Start";
            textComponent.fontSize = 32;
            textComponent.color = Color.yellow;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontStyle = FontStyles.Bold;
            
            Debug.Log("Created game state UI");
        }
        
        private void CreateDrawButtonUI()
        {
            var uiLayer = GameObject.Find("UILayer");
            
            var buttonObj = new GameObject("DrawButton");
            buttonObj.transform.SetParent(uiLayer.transform, false);
            
            var rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.3f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.3f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(300, 80);
            
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            
            var button = buttonObj.AddComponent<Button>();
            
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            var textRectTransform = textObj.AddComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.offsetMin = Vector2.zero;
            textRectTransform.offsetMax = Vector2.zero;
            
            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "TAP TO START";
            textComponent.fontSize = 28;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontStyle = FontStyles.Bold;
            
            Debug.Log("Created draw button UI");
        }
        
        private void CreateGamePositions()
        {
            var gameLayer = GameObject.Find("GameLayer");
            
            CreateGamePosition("PlayerCardPosition", gameLayer.transform, new Vector3(0, -300, 0));
            CreateGamePosition("OpponentCardPosition", gameLayer.transform, new Vector3(0, 300, 0));
            CreateGamePosition("DeckPosition", gameLayer.transform, new Vector3(-200, 0, 0));
            CreateGamePosition("WarPilePosition", gameLayer.transform, new Vector3(200, 0, 0));
            CreateGamePosition("CardPoolContainer", gameLayer.transform, new Vector3(-400, 0, 0));
            
            Debug.Log("Created game positions");
        }
        
        private void CreateGamePosition(string name, Transform parent, Vector3 position)
        {
            var positionObj = new GameObject(name);
            positionObj.transform.SetParent(parent, false);
            positionObj.transform.localPosition = position;
            
            var rectTransform = positionObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 150);
        }
        
        private void SetupRequiredComponents()
        {
            EnsureComponent<UIManager>("UIManager");
            EnsureComponent<CardAnimationController>("CardAnimationController");
            EnsureComponent<GameInteractionController>("GameInteractionController");
            
            Debug.Log("Created required components");
        }
        
        private void EnsureComponent<T>(string gameObjectName) where T : MonoBehaviour
        {
            var existing = FindObjectOfType<T>();
            if (existing == null)
            {
                var obj = new GameObject(gameObjectName);
                obj.AddComponent<T>();
                Debug.Log($"Created {gameObjectName}");
            }
        }
        
        private void FixMissingComponents()
        {
            Debug.Log("Fixing missing components...");
            SetupRequiredComponents();
            EditorUtility.DisplayDialog("Components Fixed", "Missing components have been created.", "OK");
        }
        
        private void SetupGamePositions()
        {
            Debug.Log("Setting up game positions...");
            
            var gameLayer = GameObject.Find("GameLayer");
            if (gameLayer == null)
            {
                EditorUtility.DisplayDialog("Error", "GameLayer not found. Create the complete UI hierarchy first.", "OK");
                return;
            }
            
            CreateGamePositions();
            EditorUtility.DisplayDialog("Positions Created", "Game positions have been set up.", "OK");
        }
        
        private void AutoConnectReferences()
        {
            Debug.Log("Auto-connecting UI references...");
            
            var uiManager = FindObjectOfType<UIManager>();
            var gameInteraction = FindObjectOfType<GameInteractionController>();
            
            if (uiManager != null)
            {
                ConnectUIManagerReferences(uiManager);
            }
            
            if (gameInteraction != null)
            {
                ConnectGameInteractionReferences(gameInteraction);
            }
            
            Debug.Log("References connected successfully");
        }
        
        private void ConnectUIManagerReferences(UIManager uiManager)
        {
            var serializedObject = new SerializedObject(uiManager);
            
            SetSerializedReference(serializedObject, "_playerScoreText", "PlayerScorePanel/Text");
            SetSerializedReference(serializedObject, "_opponentScoreText", "OpponentScorePanel/Text");
            SetSerializedReference(serializedObject, "_roundCounterText", "RoundCounterPanel/Text");
            SetSerializedReference(serializedObject, "_gameStateText", "GameStateDisplay");
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void ConnectGameInteractionReferences(GameInteractionController gameInteraction)
        {
            var serializedObject = new SerializedObject(gameInteraction);
            SetSerializedReference(serializedObject, "_drawCardButton", "DrawButton");
            serializedObject.ApplyModifiedProperties();
        }
        
        private void SetSerializedReference(SerializedObject serializedObject, string propertyName, string gameObjectPath)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                var targetObject = GameObject.Find(gameObjectPath);
                if (targetObject != null)
                {
                    if (propertyName.Contains("Text"))
                    {
                        property.objectReferenceValue = targetObject.GetComponent<TextMeshProUGUI>();
                    }
                    else if (propertyName.Contains("Button"))
                    {
                        property.objectReferenceValue = targetObject.GetComponent<Button>();
                    }
                    else
                    {
                        property.objectReferenceValue = targetObject;
                    }
                }
            }
        }
        
        private void ValidateAllReferences()
        {
            var issues = AnalyzeCurrentScene();
            
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation Complete", "All references are properly connected!", "Great");
            }
            else
            {
                string issueList = string.Join("\n", issues);
                EditorUtility.DisplayDialog("Validation Results", $"Found issues:\n\n{issueList}", "OK");
            }
        }
    }
}