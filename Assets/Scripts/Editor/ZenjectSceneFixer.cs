using UnityEngine;
using UnityEditor;
using Zenject;
using CardWar.Infrastructure.Installers;

namespace CardWar.Editor
{
    public class ZenjectSceneFixer : EditorWindow
    {
        [MenuItem("CardWar/🔧 Fix Zenject Scene Issues")]
        public static void ShowWindow()
        {
            GetWindow<ZenjectSceneFixer>("Zenject Scene Fixer");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Zenject Scene Fixer", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool fixes common Zenject issues:\n" +
                "• Multiple ProjectContext instances\n" +
                "• Missing SceneContext setup\n" +
                "• Improper installer configuration", 
                MessageType.Info);
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🔍 Analyze Current Scene", GUILayout.Height(30)))
            {
                AnalyzeScene();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("🛠️ Fix All Issues", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Fix Zenject Issues", 
                    "This will:\n• Remove duplicate ProjectContexts\n• Set up proper SceneContext\n• Fix installer bindings\n\nContinue?", 
                    "Fix Issues", "Cancel"))
                {
                    FixAllIssues();
                }
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "After fixing:\n" +
                "1. Save the scene\n" +
                "2. Press Play to test\n" +
                "3. Check console for any remaining errors", 
                MessageType.Warning);
        }
        
        private void AnalyzeScene()
        {
            Debug.Log("🔍 [Zenject Fixer] Analyzing scene...");
            
            var projectContexts = FindObjectsOfType<ProjectContext>();
            var sceneContexts = FindObjectsOfType<SceneContext>();
            
            Debug.Log($"Found {projectContexts.Length} ProjectContext(s)");
            Debug.Log($"Found {sceneContexts.Length} SceneContext(s)");
            
            if (projectContexts.Length > 1)
            {
                Debug.LogError($"❌ Multiple ProjectContexts found ({projectContexts.Length})! Only one should exist.");
                foreach (var pc in projectContexts)
                {
                    Debug.LogError($"ProjectContext found on: {pc.gameObject.name}", pc.gameObject);
                }
            }
            
            if (sceneContexts.Length == 0)
            {
                Debug.LogWarning("⚠️ No SceneContext found! Scene-specific bindings need SceneContext.");
            }
            
            if (sceneContexts.Length > 1)
            {
                Debug.LogError($"❌ Multiple SceneContexts found ({sceneContexts.Length})! Only one per scene should exist.");
            }
        }
        
        private void FixAllIssues()
        {
            Debug.Log("🛠️ [Zenject Fixer] Starting fixes...");
            
            FixMultipleProjectContexts();
            EnsureProperSceneContext();
            ValidateInstallers();
            
            EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
            Debug.Log("✅ [Zenject Fixer] All fixes completed!");
        }
        
        private void FixMultipleProjectContexts()
        {
            var projectContexts = FindObjectsOfType<ProjectContext>();
            
            if (projectContexts.Length <= 1) 
            {
                Debug.Log("✅ ProjectContext count is correct");
                return;
            }
            
            Debug.Log($"🔧 Removing {projectContexts.Length - 1} extra ProjectContext(s)...");
            
            ProjectContext keepContext = null;
            
            foreach (var pc in projectContexts)
            {
                var installer = pc.GetComponent<ProjectInstaller>();
                if (installer != null)
                {
                    keepContext = pc;
                    break;
                }
            }
            
            if (keepContext == null)
            {
                keepContext = projectContexts[0];
                keepContext.gameObject.AddComponent<ProjectInstaller>();
                Debug.Log($"Added ProjectInstaller to {keepContext.gameObject.name}");
            }
            
            foreach (var pc in projectContexts)
            {
                if (pc != keepContext)
                {
                    Debug.Log($"Destroying extra ProjectContext: {pc.gameObject.name}");
                    DestroyImmediate(pc.gameObject);
                }
            }
        }
        
        private void EnsureProperSceneContext()
        {
            var sceneContexts = FindObjectsOfType<SceneContext>();
            
            if (sceneContexts.Length == 0)
            {
                Debug.Log("🔧 Creating SceneContext...");
                
                var sceneContextObj = new GameObject("SceneContext");
                var sceneContext = sceneContextObj.AddComponent<SceneContext>();
                var gameInstaller = sceneContextObj.AddComponent<GameInstaller>();
                
                Debug.Log("✅ SceneContext created with GameInstaller");
            }
            else if (sceneContexts.Length > 1)
            {
                Debug.Log($"🔧 Removing {sceneContexts.Length - 1} extra SceneContext(s)...");
                
                SceneContext keepContext = null;
                
                foreach (var sc in sceneContexts)
                {
                    var installer = sc.GetComponent<GameInstaller>();
                    if (installer != null)
                    {
                        keepContext = sc;
                        break;
                    }
                }
                
                if (keepContext == null)
                {
                    keepContext = sceneContexts[0];
                    keepContext.gameObject.AddComponent<GameInstaller>();
                }
                
                foreach (var sc in sceneContexts)
                {
                    if (sc != keepContext)
                    {
                        Debug.Log($"Destroying extra SceneContext: {sc.gameObject.name}");
                        DestroyImmediate(sc.gameObject);
                    }
                }
            }
            else
            {
                Debug.Log("✅ SceneContext count is correct");
                
                var sceneContext = sceneContexts[0];
                var gameInstaller = sceneContext.GetComponent<GameInstaller>();
                if (gameInstaller == null)
                {
                    sceneContext.gameObject.AddComponent<GameInstaller>();
                    Debug.Log("✅ Added missing GameInstaller to SceneContext");
                }
            }
        }
        
        private void ValidateInstallers()
        {
            var projectContext = FindObjectOfType<ProjectContext>();
            if (projectContext != null)
            {
                var projectInstaller = projectContext.GetComponent<ProjectInstaller>();
                if (projectInstaller == null)
                {
                    projectContext.gameObject.AddComponent<ProjectInstaller>();
                    Debug.Log("✅ Added ProjectInstaller to ProjectContext");
                }
            }
            
            var sceneContext = FindObjectOfType<SceneContext>();
            if (sceneContext != null)
            {
                var gameInstaller = sceneContext.GetComponent<GameInstaller>();
                if (gameInstaller == null)
                {
                    sceneContext.gameObject.AddComponent<GameInstaller>();
                    Debug.Log("✅ Added GameInstaller to SceneContext");
                }
            }
        }
    }
}