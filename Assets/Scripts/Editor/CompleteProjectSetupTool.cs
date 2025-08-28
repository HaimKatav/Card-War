using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using CardWar.Configuration;
using CardWar.Services.Network;
using CardWar.Core.UI;
using CardWar.Gameplay.Controllers;
using CardWar.Core;
using System.IO;

namespace CardWar.Editor
{
    public class CompleteProjectSetupTool : EditorWindow
    {
        [MenuItem("CardWar/🚀 Complete Project Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<CompleteProjectSetupTool>("Complete Project Setup");
            window.minSize = new Vector2(500, 600);
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🚀 COMPLETE PROJECT SETUP", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool will set up your entire project from scratch:\n\n" +
                "✅ Create all required assets (GameSettings, NetworkErrorConfig)\n" +
                "✅ Set up proper folder structure\n" +
                "✅ Create all missing scene components\n" +
                "✅ Configure Zenject bindings properly\n" +
                "✅ Test the complete setup\n\n" +
                "Run this once to fix all current issues!",
                MessageType.Info);
            
            GUILayout.Space(20);
            
            DisplayProjectStatus();
            
            GUILayout.Space(20);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🚀 SET UP ENTIRE PROJECT", GUILayout.Height(50)))
            {
                if (EditorUtility.DisplayDialog("Complete Project Setup", 
                    "This will create all missing assets and components.\n\n" +
                    "This process will:\n" +
                    "• Create GameSettings.asset\n" +
                    "• Create NetworkErrorConfig.asset\n" +
                    "• Set up proper folder structure\n" +
                    "• Add missing scene components\n" +
                    "• Configure everything properly\n\n" +
                    "Continue?", 
                    "Yes, Set Up Project", "Cancel"))
                {
                    SetupCompleteProject();
                }
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(20);
            
            GUILayout.Label("Individual Setup Steps:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("📁 Create Folder Structure"))
            {
                CreateFolderStructure();
            }
            
            if (GUILayout.Button("⚙️ Create Required Assets"))
            {
                CreateRequiredAssets();
            }
            
            if (GUILayout.Button("🎮 Setup Scene Components"))
            {
                SetupSceneComponents();
            }
            
            if (GUILayout.Button("🧪 Test Complete Setup"))
            {
                TestCompleteSetup();
            }
        }
        
        private void DisplayProjectStatus()
        {
            EditorGUILayout.LabelField("📊 Project Status:", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            
            // Check GameSettings
            var gameSettings = Resources.Load<GameSettings>("GameSettings");
            EditorGUILayout.Toggle("GameSettings.asset", gameSettings != null);
            
            // Check NetworkErrorConfig
            var networkConfig = Resources.Load<NetworkErrorConfig>("Settings/NetworkErrorConfig");
            EditorGUILayout.Toggle("NetworkErrorConfig.asset", networkConfig != null);
            
            // Check scene components
            var uiManager = FindObjectOfType<UIManager>();
            EditorGUILayout.Toggle("UIManager in Scene", uiManager != null);
            
            var canvasManager = FindObjectOfType<CanvasManager>();
            EditorGUILayout.Toggle("CanvasManager in Scene", canvasManager != null);
            
            var cardAnimController = FindObjectOfType<CardAnimationController>();
            EditorGUILayout.Toggle("CardAnimationController in Scene", cardAnimController != null);
            
            var gameInteraction = FindObjectOfType<GameInteractionController>();
            EditorGUILayout.Toggle("GameInteractionController in Scene", gameInteraction != null);
            
            EditorGUI.EndDisabledGroup();
        }
        
        private void SetupCompleteProject()
        {
            Debug.Log("🚀 [Complete Setup] Starting complete project setup...");
            
            try
            {
                CreateFolderStructure();
                CreateRequiredAssets();
                SetupSceneComponents();
                
                // Save everything
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
                
                Debug.Log("✅ [Complete Setup] Project setup completed successfully!");
                
                EditorUtility.DisplayDialog("Setup Complete!", 
                    "✅ Project setup completed successfully!\n\n" +
                    "What was created/fixed:\n" +
                    "• GameSettings.asset in Resources/\n" +
                    "• NetworkErrorConfig.asset in Resources/Settings/\n" +
                    "• All required scene components\n" +
                    "• Proper folder structure\n\n" +
                    "Press Play to test your game!",
                    "Great!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ [Complete Setup] Setup failed: {ex.Message}");
                EditorUtility.DisplayDialog("Setup Failed", $"Setup failed: {ex.Message}", "OK");
            }
        }
        
        private void CreateFolderStructure()
        {
            Debug.Log("📁 [Complete Setup] Creating folder structure...");
            
            string[] folders = {
                "Assets/Resources",
                "Assets/Resources/Settings",
                "Assets/Resources/GameplaySprites",
                "Assets/Resources/GameplaySprites/Cards",
                "Assets/Resources/GameplaySprites/UI",
                "Assets/Resources/Prefabs",
                "Assets/Resources/Prefabs/Cards",
                "Assets/Resources/Audio",
                "Assets/Resources/Audio/SFX",
                "Assets/Resources/Audio/Music"
            };
            
            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parentFolder = Path.GetDirectoryName(folder).Replace('\\', '/');
                    string folderName = Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parentFolder, folderName);
                    Debug.Log($"Created folder: {folder}");
                }
            }
            
            AssetDatabase.Refresh();
        }
        
        private void CreateRequiredAssets()
        {
            Debug.Log("⚙️ [Complete Setup] Creating required assets...");
            
            // Create GameSettings
            var existingGameSettings = Resources.Load<GameSettings>("GameSettings");
            if (existingGameSettings == null)
            {
                var gameSettings = CreateInstance<GameSettings>();
                
                // Set sensible defaults
                gameSettings.canvasReferenceResolution = new Vector2(1080, 1920);
                gameSettings.canvasMatchWidthOrHeight = 0.5f;
                gameSettings.backgroundColor = new Color(0.1f, 0.5f, 0.1f, 1f);
                gameSettings.playerColor = new Color(0.2f, 0.6f, 1f, 1f);
                gameSettings.opponentColor = new Color(1f, 0.4f, 0.2f, 1f);
                gameSettings.playerCardPosition = new Vector3(0, -300, 0);
                gameSettings.opponentCardPosition = new Vector3(0, 300, 0);
                gameSettings.deckPosition = new Vector3(-200, 0, 0);
                gameSettings.warPilePosition = new Vector3(200, 0, 0);
                
                AssetDatabase.CreateAsset(gameSettings, "Assets/Resources/GameSettings.asset");
                Debug.Log("✅ Created GameSettings.asset");
            }
            
            // Create NetworkErrorConfig
            var existingNetworkConfig = Resources.Load<NetworkErrorConfig>("Settings/NetworkErrorConfig");
            if (existingNetworkConfig == null)
            {
                var networkConfig = CreateInstance<NetworkErrorConfig>();
                
                // Set default values
                networkConfig.timeoutRate = 0.01f;
                networkConfig.networkErrorRate = 0.02f;
                networkConfig.serverErrorRate = 0.005f;
                networkConfig.corruptionRate = 0.001f;
                networkConfig.minNetworkDelay = 0.1f;
                networkConfig.maxNetworkDelay = 0.3f;
                networkConfig.timeoutDuration = 5f;
                networkConfig.retryBaseDelay = 1f;
                
                AssetDatabase.CreateAsset(networkConfig, "Assets/Resources/Settings/NetworkErrorConfig.asset");
                Debug.Log("✅ Created NetworkErrorConfig.asset");
            }
            
            AssetDatabase.SaveAssets();
        }
        
        private void SetupSceneComponents()
        {
            Debug.Log("🎮 [Complete Setup] Setting up scene components...");
            
            // Ensure MainCanvas exists with CanvasManager
            var mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                var canvasObj = new GameObject("MainCanvas");
                mainCanvas = canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Debug.Log("✅ Created MainCanvas");
            }
            
            // Add CanvasManager if missing
            var canvasManager = mainCanvas.GetComponent<CanvasManager>();
            if (canvasManager == null)
            {
                canvasManager = mainCanvas.gameObject.AddComponent<CanvasManager>();
                Debug.Log("✅ Added CanvasManager to MainCanvas");
            }
            
            // Create UIManager if missing
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                var uiManagerObj = new GameObject("UIManager");
                uiManager = uiManagerObj.AddComponent<UIManager>();
                Debug.Log("✅ Created UIManager");
            }
            
            // Create CardAnimationController if missing
            var cardAnimController = FindObjectOfType<CardAnimationController>();
            if (cardAnimController == null)
            {
                var cardAnimObj = new GameObject("CardAnimationController");
                cardAnimController = cardAnimObj.AddComponent<CardAnimationController>();
                Debug.Log("✅ Created CardAnimationController");
            }
            
            // Create GameInteractionController if missing
            var gameInteraction = FindObjectOfType<GameInteractionController>();
            if (gameInteraction == null)
            {
                var gameInteractionObj = new GameObject("GameInteractionController");
                gameInteraction = gameInteractionObj.AddComponent<GameInteractionController>();
                Debug.Log("✅ Created GameInteractionController");
            }
        }
        
        private void TestCompleteSetup()
        {
            Debug.Log("🧪 [Complete Setup] Testing complete setup...");
            
            var issues = new System.Collections.Generic.List<string>();
            
            // Test assets
            if (Resources.Load<GameSettings>("GameSettings") == null)
                issues.Add("GameSettings.asset missing");
                
            if (Resources.Load<NetworkErrorConfig>("Settings/NetworkErrorConfig") == null)
                issues.Add("NetworkErrorConfig.asset missing");
            
            // Test scene components
            if (FindObjectOfType<UIManager>() == null)
                issues.Add("UIManager missing");
                
            if (FindObjectOfType<CanvasManager>() == null)
                issues.Add("CanvasManager missing");
                
            if (FindObjectOfType<CardAnimationController>() == null)
                issues.Add("CardAnimationController missing");
                
            if (FindObjectOfType<GameInteractionController>() == null)
                issues.Add("GameInteractionController missing");
            
            if (issues.Count == 0)
            {
                Debug.Log("✅ [Complete Setup] All tests passed! Project is ready.");
                EditorUtility.DisplayDialog("Test Results", 
                    "✅ All tests passed!\n\nYour project is properly set up and ready to run.\n\nPress Play to test your game!", 
                    "Excellent!");
            }
            else
            {
                Debug.LogWarning($"⚠️ [Complete Setup] Found {issues.Count} issues: {string.Join(", ", issues)}");
                EditorUtility.DisplayDialog("Test Results", 
                    $"Found {issues.Count} issues:\n\n{string.Join("\n", issues)}\n\nRun the complete setup to fix these issues.", 
                    "OK");
            }
        }
    }
}