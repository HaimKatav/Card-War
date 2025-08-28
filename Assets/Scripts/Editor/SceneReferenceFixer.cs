using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using CardWar.UI;
using CardWar.UI.Core;
using CardWar.Configuration;
using CardWar.Core.UI;
using CardWar.Gameplay.Controllers;
using CardWar.Infrastructure.Installers;

namespace CardWar.Editor
{
    public class SceneReferenceFixer : EditorWindow
    {
        [MenuItem("CardWar/🔧 Fix Scene References")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneReferenceFixer>("Scene Reference Fixer");
            window.minSize = new Vector2(400, 500);
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🔧 SCENE REFERENCE FIXER", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool will fix common scene reference issues:\n" +
                "• Create missing GameSettings\n" +
                "• Connect UIManager references properly\n" +
                "• Fix positioning issues\n" +
                "• Validate all connections",
                MessageType.Info);
            
            GUILayout.Space(20);
            
            // Status display
            DisplayCurrentStatus();
            
            GUILayout.Space(20);
            
            // Fix buttons
            if (GUILayout.Button("🎯 Fix All References", GUILayout.Height(40)))
            {
                FixAllReferences();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("📝 Create GameSettings"))
            {
                CreateGameSettings();
            }
            
            if (GUILayout.Button("🔗 Fix UIManager References"))
            {
                FixUIManagerReferences();
            }
            
            if (GUILayout.Button("📐 Fix UI Positioning"))
            {
                FixUIPositioning();
            }
            
            if (GUILayout.Button("🧪 Validate Scene"))
            {
                ValidateScene();
            }
        }
        
        private void DisplayCurrentStatus()
        {
            EditorGUILayout.LabelField("Current Scene Status:", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            
            var gameSettings = Resources.Load<GameSettings>("GameSettings");
            EditorGUILayout.Toggle("GameSettings Exists", gameSettings != null);
            
            var uiManager = FindObjectOfType<UIManager>();
            EditorGUILayout.Toggle("UIManager Present", uiManager != null);
            
            var canvas = FindObjectOfType<Canvas>();
            EditorGUILayout.Toggle("Main Canvas Present", canvas != null);
            
            var playerScorePanel = GameObject.Find("PlayerScorePanel");
            EditorGUILayout.Toggle("Player Score Panel Found", playerScorePanel != null);
            
            var opponentScorePanel = GameObject.Find("OpponentScorePanel");
            EditorGUILayout.Toggle("Opponent Score Panel Found", opponentScorePanel != null);
            
            EditorGUI.EndDisabledGroup();
        }
        
        private void FixAllReferences()
        {
            Debug.Log("🔧 [Scene Reference Fixer] Starting comprehensive fix...");
            
            try
            {
                // Step 1: Create GameSettings if missing
                if (Resources.Load<GameSettings>("GameSettings") == null)
                {
                    CreateGameSettings();
                }
                
                // Step 2: Fix UI positioning
                FixUIPositioning();
                
                // Step 3: Fix UIManager references
                FixUIManagerReferences();
                
                // Step 4: Validate everything
                ValidateScene();
                
                Debug.Log("✅ All fixes applied successfully!");
                
                EditorUtility.DisplayDialog("Success!", 
                    "✅ Scene references fixed!\n\n" +
                    "Changes made:\n" +
                    "• GameSettings created/verified\n" +
                    "• UI positioning corrected\n" +
                    "• UIManager references connected\n" +
                    "• Scene validated\n\n" +
                    "Press Play to test!",
                    "Great!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Error during fix: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Fix failed: {ex.Message}", "OK");
            }
        }
        
        private void CreateGameSettings()
        {
            Debug.Log("📝 [Scene Reference Fixer] Creating GameSettings...");
            
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            var settings = CreateInstance<GameSettings>();
            
            // Set default values
            settings.canvasReferenceResolution = new Vector2(1080, 1920);
            settings.canvasMatchWidthOrHeight = 0.5f;
            settings.backgroundColor = new Color(0.1f, 0.5f, 0.1f, 1f); // Green table color
            settings.playerColor = new Color(0.2f, 0.6f, 1f, 1f); // Blue
            settings.opponentColor = new Color(1f, 0.4f, 0.2f, 1f); // Red
            
            // Set card positions (relative to screen)
            settings.playerCardPosition = new Vector3(0, -300, 0);
            settings.opponentCardPosition = new Vector3(0, 300, 0);
            settings.deckPosition = new Vector3(-200, 0, 0);
            settings.warPilePosition = new Vector3(200, 0, 0);
            
            AssetDatabase.CreateAsset(settings, "Assets/Resources/GameSettings.asset");
            AssetDatabase.SaveAssets();
            
            Selection.activeObject = settings;
            
            Debug.Log("✅ GameSettings created at: Assets/Resources/GameSettings.asset");
        }
        
        private void FixUIManagerReferences()
        {
            Debug.Log("🔗 [Scene Reference Fixer] Fixing UIManager references...");
            
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("UIManager not found in scene!");
                return;
            }
            
            // Find UI elements
            var playerScorePanel = GameObject.Find("PlayerScorePanel");
            var opponentScorePanel = GameObject.Find("OpponentScorePanel");
            var centerPanel = GameObject.Find("CenterGamePanel");
            var gameStatePanel = GameObject.Find("GameStatePanel");
            var warIndicator = GameObject.Find("WarIndicator");
            var gameOverScreen = GameObject.Find("GameOverScreen");
            var background = GameObject.Find("Background");
            
            if (playerScorePanel == null)
            {
                Debug.LogError("PlayerScorePanel not found! Make sure scene was built properly.");
                return;
            }
            
            // Get text components
            var playerScoreText = playerScorePanel?.GetComponentInChildren<TextMeshProUGUI>();
            var opponentScoreText = opponentScorePanel?.GetComponentInChildren<TextMeshProUGUI>();
            var roundText = centerPanel?.GetComponentInChildren<TextMeshProUGUI>();
            var gameStateText = gameStatePanel?.GetComponent<TextMeshProUGUI>();
            var winnerText = gameOverScreen?.GetComponentInChildren<TextMeshProUGUI>();
            
            // Get image components
            var backgroundImage = background?.GetComponent<Image>();
            var playerScoreBackground = playerScorePanel?.GetComponent<Image>();
            var opponentScoreBackground = opponentScorePanel?.GetComponent<Image>();
            var centerPanelBackground = centerPanel?.GetComponent<Image>();
            
            // Use SerializedObject for proper editor assignment
            var serializedObject = new SerializedObject(uiManager);
            
            // Set panel references
            SetSerializedProperty(serializedObject, "_playerScorePanel", playerScorePanel);
            SetSerializedProperty(serializedObject, "_opponentScorePanel", opponentScorePanel);
            SetSerializedProperty(serializedObject, "_centerGamePanel", centerPanel);
            SetSerializedProperty(serializedObject, "_gameStatePanel", gameStatePanel);
            SetSerializedProperty(serializedObject, "_warIndicator", warIndicator);
            SetSerializedProperty(serializedObject, "_gameOverScreen", gameOverScreen);
            
            // Set text references
            SetSerializedProperty(serializedObject, "_playerScoreText", playerScoreText);
            SetSerializedProperty(serializedObject, "_opponentScoreText", opponentScoreText);
            SetSerializedProperty(serializedObject, "_roundText", roundText);
            SetSerializedProperty(serializedObject, "_gameStateText", gameStateText);
            SetSerializedProperty(serializedObject, "_winnerText", winnerText);
            
            // Set image references
            SetSerializedProperty(serializedObject, "_backgroundImage", backgroundImage);
            SetSerializedProperty(serializedObject, "_playerScoreBackground", playerScoreBackground);
            SetSerializedProperty(serializedObject, "_opponentScoreBackground", opponentScoreBackground);
            SetSerializedProperty(serializedObject, "_centerPanelBackground", centerPanelBackground);
            
            serializedObject.ApplyModifiedProperties();
            
            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            
            Debug.Log("✅ UIManager references connected successfully!");
        }
        
        private void SetSerializedProperty(SerializedObject serializedObject, string propertyName, Object value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                if (value != null)
                {
                    Debug.Log($"✓ Set {propertyName} = {value.name}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Property {propertyName} not found in UIManager");
            }
        }
        
        private void FixUIPositioning()
        {
            Debug.Log("📐 [Scene Reference Fixer] Fixing UI positioning...");
            
            var playerScorePanel = GameObject.Find("PlayerScorePanel");
            var opponentScorePanel = GameObject.Find("OpponentScorePanel");
            var centerPanel = GameObject.Find("CenterGamePanel");
            var gameStatePanel = GameObject.Find("GameStatePanel");
            
            // Fix Player Score Panel (bottom of screen)
            if (playerScorePanel != null)
            {
                var rectTransform = playerScorePanel.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0.5f, 0f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0f);
                    rectTransform.anchoredPosition = new Vector2(0, 80);
                    rectTransform.sizeDelta = new Vector2(400, 80);
                    
                    Debug.Log("✓ Fixed Player Score Panel position");
                }
            }
            
            // Fix Opponent Score Panel (top of screen)
            if (opponentScorePanel != null)
            {
                var rectTransform = opponentScorePanel.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0.5f, 1f);
                    rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    rectTransform.anchoredPosition = new Vector2(0, -80);
                    rectTransform.sizeDelta = new Vector2(400, 80);
                    
                    Debug.Log("✓ Fixed Opponent Score Panel position");
                }
            }
            
            // Fix Center Panel
            if (centerPanel != null)
            {
                var rectTransform = centerPanel.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = new Vector2(0, 0);
                    rectTransform.sizeDelta = new Vector2(300, 80);
                    
                    Debug.Log("✓ Fixed Center Panel position");
                }
            }
            
            // Fix Game State Panel
            if (gameStatePanel != null)
            {
                var rectTransform = gameStatePanel.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0.5f, 0.3f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.3f);
                    rectTransform.anchoredPosition = new Vector2(0, 0);
                    rectTransform.sizeDelta = new Vector2(300, 60);
                    
                    Debug.Log("✓ Fixed Game State Panel position");
                }
            }
            
            Debug.Log("✅ UI positioning fixed!");
        }
        
        private void ValidateScene()
        {
            Debug.Log("🧪 [Scene Reference Fixer] Validating scene setup...");
            
            var issues = new System.Collections.Generic.List<string>();
            var warnings = new System.Collections.Generic.List<string>();
            
            // Check essential components
            var gameSettings = Resources.Load<GameSettings>("GameSettings");
            if (gameSettings == null) issues.Add("GameSettings missing");
            
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) issues.Add("Main Canvas missing");
            
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null) issues.Add("UIManager missing");
            
            // Check UI elements
            var playerScorePanel = GameObject.Find("PlayerScorePanel");
            if (playerScorePanel == null) issues.Add("PlayerScorePanel missing");
            
            var opponentScorePanel = GameObject.Find("OpponentScorePanel");
            if (opponentScorePanel == null) issues.Add("OpponentScorePanel missing");
            
            // Check text components
            if (playerScorePanel != null)
            {
                var playerText = playerScorePanel.GetComponentInChildren<TextMeshProUGUI>();
                if (playerText == null) warnings.Add("Player score text missing");
            }
            
            if (opponentScorePanel != null)
            {
                var opponentText = opponentScorePanel.GetComponentInChildren<TextMeshProUGUI>();
                if (opponentText == null) warnings.Add("Opponent score text missing");
            }
            
            // Check positioning
            if (playerScorePanel != null)
            {
                var rectTransform = playerScorePanel.GetComponent<RectTransform>();
                if (rectTransform != null && rectTransform.anchoredPosition.y < 0)
                {
                    warnings.Add("Player panel may be positioned incorrectly");
                }
            }
            
            // Report results
            if (issues.Count > 0)
            {
                Debug.LogError($"🧪 Found {issues.Count} critical issues: {string.Join(", ", issues)}");
            }
            
            if (warnings.Count > 0)
            {
                Debug.LogWarning($"🧪 Found {warnings.Count} warnings: {string.Join(", ", warnings)}");
            }
            
            if (issues.Count == 0 && warnings.Count == 0)
            {
                Debug.Log("✅ Scene validation passed! Everything looks good.");
            }
            
            // Display summary
            string summary = $"Validation Results:\n" +
                           $"• Critical Issues: {issues.Count}\n" +
                           $"• Warnings: {warnings.Count}";
                           
            if (issues.Count == 0)
            {
                summary += "\n\n✅ Scene is ready for testing!";
                EditorUtility.DisplayDialog("Validation Complete", summary, "Great!");
            }
            else
            {
                summary += $"\n\n❌ Issues found:\n{string.Join("\n", issues)}";
                EditorUtility.DisplayDialog("Validation Complete", summary, "OK");
            }
        }
    }
}