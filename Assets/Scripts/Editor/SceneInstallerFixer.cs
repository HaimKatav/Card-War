using CardWar.Core;
using CardWar.Core.UI;
using CardWar.Gameplay.Controllers;
using UnityEngine;
using UnityEditor;
using Zenject;
using CardWar.Infrastructure.Installers;

namespace CardWar.Editor
{
    public class SceneInstallerFixer : EditorWindow
    {
        [MenuItem("CardWar/Fix Scene Installer")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneInstallerFixer>("Scene Installer Fixer");
            window.minSize = new Vector2(450, 350);
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Scene Installer Fixer", EditorStyles.largeLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool fixes the missing SceneContext and GameInstaller that are required for Zenject dependency injection to work.\n\n" +
                "Without these, your scene components won't be bound and Initialize() methods won't be called.",
                MessageType.Warning);
            
            GUILayout.Space(20);
            
            DrawCurrentStatus();
            GUILayout.Space(20);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("CREATE SCENE INSTALLER SETUP", GUILayout.Height(40)))
            {
                CreateSceneInstallerSetup();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Validate Setup"))
            {
                ValidateSetup();
            }
        }
        
        private void DrawCurrentStatus()
        {
            EditorGUILayout.LabelField("Current Status:", EditorStyles.boldLabel);
            
            var sceneContext = FindObjectOfType<SceneContext>();
            var gameInstaller = FindObjectOfType<GameInstaller>();
            var camera = FindObjectOfType<Camera>();
            var canvas = FindObjectOfType<Canvas>();
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle("SceneContext Exists", sceneContext != null);
            EditorGUILayout.Toggle("GameInstaller Exists", gameInstaller != null);
            EditorGUILayout.Toggle("Main Camera Found", camera != null);
            EditorGUILayout.Toggle("Canvas Found", canvas != null);
            
            if (sceneContext != null && gameInstaller != null)
            {
                EditorGUILayout.Toggle("Installer Attached to Context", sceneContext.GetComponent<GameInstaller>() != null);
            }
            EditorGUI.EndDisabledGroup();
        }
        
        private void CreateSceneInstallerSetup()
        {
            Debug.Log("[Scene Installer Fixer] Creating SceneContext and GameInstaller setup...");
            
            try
            {
                var sceneContext = EnsureSceneContext();
                var gameInstaller = EnsureGameInstaller(sceneContext);
                ConfigureGameInstaller(gameInstaller);
                
                EditorUtility.SetDirty(sceneContext.gameObject);
                EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
                
                Debug.Log("[Scene Installer Fixer] Setup completed successfully!");
                
                EditorUtility.DisplayDialog("Setup Complete", 
                    "SceneContext and GameInstaller have been created and configured successfully!\n\n" +
                    "What was created:\n" +
                    "• SceneContext GameObject\n" +
                    "• GameInstaller component attached to SceneContext\n" +
                    "• All scene references automatically configured\n\n" +
                    "Press Play to test - Initialize() should now be called!",
                    "Perfect");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Scene Installer Fixer] Setup failed: {ex.Message}");
                EditorUtility.DisplayDialog("Setup Failed", $"Error: {ex.Message}", "OK");
            }
        }
        
        private SceneContext EnsureSceneContext()
        {
            var existingContext = FindObjectOfType<SceneContext>();
            if (existingContext != null)
            {
                Debug.Log("[Scene Installer Fixer] SceneContext already exists, using existing one");
                return existingContext;
            }
            
            var contextObj = new GameObject("SceneContext");
            var sceneContext = contextObj.AddComponent<SceneContext>();
            
            
            Debug.Log("[Scene Installer Fixer] Created new SceneContext");
            return sceneContext;
        }
        
        private GameInstaller EnsureGameInstaller(SceneContext sceneContext)
        {
            var existingInstaller = sceneContext.GetComponent<GameInstaller>();
            if (existingInstaller != null)
            {
                Debug.Log("[Scene Installer Fixer] GameInstaller already exists on SceneContext");
                return existingInstaller;
            }
            
            var gameInstaller = sceneContext.gameObject.AddComponent<GameInstaller>();
            Debug.Log("[Scene Installer Fixer] Added GameInstaller to SceneContext");
            return gameInstaller;
        }
        
        private void ConfigureGameInstaller(GameInstaller gameInstaller)
        {
            Debug.Log("[Scene Installer Fixer] Configuring GameInstaller references...");
            
            var serializedObject = new SerializedObject(gameInstaller);
            
            // Find and assign Camera
            var camera = FindObjectOfType<Camera>();
            if (camera != null)
            {
                var cameraProperty = serializedObject.FindProperty("_gameCamera");
                if (cameraProperty != null)
                {
                    cameraProperty.objectReferenceValue = camera;
                    Debug.Log($"[Scene Installer Fixer] Assigned Camera: {camera.name}");
                }
            }
            
            // Find and assign Canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                var canvasProperty = serializedObject.FindProperty("_gameCanvas");
                if (canvasProperty != null)
                {
                    canvasProperty.objectReferenceValue = canvas;
                    Debug.Log($"[Scene Installer Fixer] Assigned Canvas: {canvas.name}");
                }
            }
            
            // Find and assign CardPrefab (if it exists)
            var cardPrefab = Resources.Load<GameObject>("Prefabs/Cards/CardPrefab");
            if (cardPrefab != null)
            {
                var prefabProperty = serializedObject.FindProperty("_cardPrefab");
                if (prefabProperty != null)
                {
                    prefabProperty.objectReferenceValue = cardPrefab;
                    Debug.Log("[Scene Installer Fixer] Assigned CardPrefab from Resources");
                }
            }
            else
            {
                Debug.LogWarning("[Scene Installer Fixer] CardPrefab not found at Resources/Prefabs/Cards/CardPrefab");
            }
            
            // Find and assign CardPoolContainer
            var poolContainer = GameObject.Find("CardPoolContainer");
            if (poolContainer != null)
            {
                var poolProperty = serializedObject.FindProperty("_cardPoolContainer");
                if (poolProperty != null)
                {
                    poolProperty.objectReferenceValue = poolContainer.transform;
                    Debug.Log($"[Scene Installer Fixer] Assigned CardPoolContainer: {poolContainer.name}");
                }
            }
            else
            {
                Debug.LogWarning("[Scene Installer Fixer] CardPoolContainer not found in scene");
            }
            
            serializedObject.ApplyModifiedProperties();
            Debug.Log("[Scene Installer Fixer] GameInstaller configuration completed");
        }
        
        private void ValidateSetup()
        {
            Debug.Log("[Scene Installer Fixer] Validating setup...");
            
            var issues = new System.Collections.Generic.List<string>();
            
            // Check SceneContext
            var sceneContext = FindObjectOfType<SceneContext>();
            if (sceneContext == null)
            {
                issues.Add("SceneContext missing");
            }
            else
            {
                // Check GameInstaller on SceneContext
                var gameInstaller = sceneContext.GetComponent<GameInstaller>();
                if (gameInstaller == null)
                {
                    issues.Add("GameInstaller not attached to SceneContext");
                }
                else
                {
                    // Check GameInstaller references
                    var serializedObject = new SerializedObject(gameInstaller);
                    
                    if (serializedObject.FindProperty("_gameCamera")?.objectReferenceValue == null)
                        issues.Add("GameInstaller: Camera reference not assigned");
                        
                    if (serializedObject.FindProperty("_gameCanvas")?.objectReferenceValue == null)
                        issues.Add("GameInstaller: Canvas reference not assigned");
                }
            }
            
            // Check essential scene components
            if (FindObjectOfType<UIManager>() == null)
                issues.Add("UIManager component missing from scene");
                
            if (FindObjectOfType<CardAnimationController>() == null)
                issues.Add("CardAnimationController component missing from scene");
                
            if (FindObjectOfType<GameInteractionController>() == null)
                issues.Add("GameInteractionController component missing from scene");
            
            // Show results
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation Complete", 
                    "Setup validation passed!\n\n" +
                    "Your scene is properly configured for Zenject dependency injection.\n" +
                    "Press Play to test the game.", 
                    "Excellent");
            }
            else
            {
                string issueList = string.Join("\n• ", issues);
                EditorUtility.DisplayDialog("Validation Results", 
                    $"Found {issues.Count} issues:\n\n• {issueList}\n\n" +
                    "Run 'Create Scene Installer Setup' to fix these issues.", 
                    "OK");
            }
        }
    }
}