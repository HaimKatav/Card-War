#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using CardWar.Core.UI;
using TMPro;

namespace CardWar.Editor
{
    public class SceneCleanupEditor : EditorWindow
    {
        [MenuItem("Card War/Cleanup & Fix Scene")]
        public static void ShowWindow()
        {
            GetWindow<SceneCleanupEditor>("Scene Cleanup");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Scene Cleanup & Reference Fixer", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("Remove Duplicate UI Elements", GUILayout.Height(30)))
            {
                RemoveDuplicates();
            }
            
            if (GUILayout.Button("Fix CardAnimationController References", GUILayout.Height(30)))
            {
                FixCardAnimationReferences();
            }
            
            if (GUILayout.Button("Connect UI to Managers", GUILayout.Height(30)))
            {
                ConnectUIToManagers();
            }
            
            if (GUILayout.Button("Validate Scene Hierarchy", GUILayout.Height(30)))
            {
                ValidateSceneHierarchy();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("COMPLETE CLEANUP", GUILayout.Height(40)))
            {
                RemoveDuplicates();
                FixCardAnimationReferences();
                ConnectUIToManagers();
                ValidateSceneHierarchy();
                EditorUtility.SetDirty(FindObjectOfType<Canvas>());
                Debug.Log("[Cleanup] Complete cleanup finished!");
            }
        }
        
        private void RemoveDuplicates()
        {
            // Find all canvases
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            if (canvases.Length > 1)
            {
                Debug.Log($"[Cleanup] Found {canvases.Length} canvases, keeping only MainCanvas");
                
                Canvas mainCanvas = null;
                foreach (var canvas in canvases)
                {
                    if (canvas.name == "MainCanvas")
                    {
                        mainCanvas = canvas;
                        break;
                    }
                }
                
                if (mainCanvas == null)
                {
                    mainCanvas = canvases[0];
                    mainCanvas.name = "MainCanvas";
                }
                
                // Delete other canvases
                foreach (var canvas in canvases)
                {
                    if (canvas != mainCanvas)
                    {
                        DestroyImmediate(canvas.gameObject);
                    }
                }
            }
            
            // Remove duplicate TAP TO START buttons
            Button[] buttons = FindObjectsOfType<Button>();
            List<Button> tapButtons = new List<Button>();
            
            foreach (var button in buttons)
            {
                var text = button.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null && text.text.Contains("TAP TO START"))
                {
                    tapButtons.Add(button);
                }
            }
            
            if (tapButtons.Count > 1)
            {
                Debug.Log($"[Cleanup] Found {tapButtons.Count} TAP TO START buttons, keeping only one");
                
                // Keep the one inside PlayArea
                Button keepButton = null;
                foreach (var button in tapButtons)
                {
                    if (button.transform.parent != null && 
                        button.transform.parent.name == "PlayArea")
                    {
                        keepButton = button;
                        break;
                    }
                }
                
                if (keepButton == null) keepButton = tapButtons[0];
                
                foreach (var button in tapButtons)
                {
                    if (button != keepButton)
                    {
                        DestroyImmediate(button.gameObject);
                    }
                }
            }
            
            Debug.Log("[Cleanup] Duplicate removal complete");
        }
        
        private void FixCardAnimationReferences()
        {
            var cardAnimController = FindObjectOfType<CardWar.Gameplay.Controllers.CardAnimationController>();
            if (cardAnimController == null)
            {
                Debug.LogWarning("[Cleanup] CardAnimationController not found in scene");
                return;
            }
            
            // Use serialized object to set references
            SerializedObject serializedController = new SerializedObject(cardAnimController);
            
            // Find card position objects
            GameObject playerCard = GameObject.Find("PlayerCardPosition");
            GameObject opponentCard = GameObject.Find("OpponentCardPosition");
            GameObject warPile = GameObject.Find("WarPilePosition");
            GameObject deck = GameObject.Find("DeckPosition");
            
            if (playerCard != null)
            {
                SerializedProperty prop = serializedController.FindProperty("_playerCardPosition");
                if (prop != null)
                {
                    prop.objectReferenceValue = playerCard.transform;
                }
            }
            
            if (opponentCard != null)
            {
                SerializedProperty prop = serializedController.FindProperty("_opponentCardPosition");
                if (prop != null)
                {
                    prop.objectReferenceValue = opponentCard.transform;
                }
            }
            
            if (warPile != null)
            {
                SerializedProperty prop = serializedController.FindProperty("_warPilePosition");
                if (prop != null)
                {
                    prop.objectReferenceValue = warPile.transform;
                }
            }
            
            if (deck != null)
            {
                SerializedProperty prop = serializedController.FindProperty("_deckPosition");
                if (prop != null)
                {
                    prop.objectReferenceValue = deck.transform;
                }
            }
            
            serializedController.ApplyModifiedProperties();
            EditorUtility.SetDirty(cardAnimController);
            
            Debug.Log("[Cleanup] CardAnimationController references fixed");
        }
        
        private void ConnectUIToManagers()
        {
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("[Cleanup] UIManager not found in scene");
                return;
            }
            
            SerializedObject serializedUI = new SerializedObject(uiManager);
            
            // Connect score displays
            GameObject playerScore = GameObject.Find("PlayerSection/ScoreDisplay");
            GameObject opponentScore = GameObject.Find("OpponentSection/ScoreDisplay");
            GameObject roundDisplay = GameObject.Find("OpponentSection/RoundDisplay");
            GameObject stateText = GameObject.Find("PlayArea/GameStateText");
            GameObject warIndicator = GameObject.Find("UIOverlay/WarIndicator");
            GameObject gameOverPanel = GameObject.Find("UIOverlay/GameOverPanel");
            GameObject tapButton = GameObject.Find("PlayArea/TapToStartButton");
            
            if (playerScore != null)
            {
                var prop = serializedUI.FindProperty("_playerScoreText");
                if (prop != null)
                {
                    prop.objectReferenceValue = playerScore.GetComponent<TextMeshProUGUI>();
                }
            }
            
            if (opponentScore != null)
            {
                var prop = serializedUI.FindProperty("_opponentScoreText");
                if (prop != null)
                {
                    prop.objectReferenceValue = opponentScore.GetComponent<TextMeshProUGUI>();
                }
            }
            
            if (roundDisplay != null)
            {
                var prop = serializedUI.FindProperty("_roundText");
                if (prop != null)
                {
                    prop.objectReferenceValue = roundDisplay.GetComponent<TextMeshProUGUI>();
                }
            }
            
            if (stateText != null)
            {
                var prop = serializedUI.FindProperty("_gameStateText");
                if (prop != null)
                {
                    prop.objectReferenceValue = stateText.GetComponent<TextMeshProUGUI>();
                }
            }
            
            if (warIndicator != null)
            {
                var prop = serializedUI.FindProperty("_warIndicator");
                if (prop != null)
                {
                    prop.objectReferenceValue = warIndicator;
                }
            }
            
            if (gameOverPanel != null)
            {
                var prop = serializedUI.FindProperty("_gameOverPanel");
                if (prop != null)
                {
                    prop.objectReferenceValue = gameOverPanel;
                }
                
                var winnerText = gameOverPanel.transform.Find("WinnerText");
                if (winnerText != null)
                {
                    var winnerProp = serializedUI.FindProperty("_gameOverWinnerText");
                    if (winnerProp != null)
                    {
                        winnerProp.objectReferenceValue = winnerText.GetComponent<TextMeshProUGUI>();
                    }
                }
                
                var restartButton = gameOverPanel.transform.Find("RestartButton");
                if (restartButton != null)
                {
                    var buttonProp = serializedUI.FindProperty("_restartButton");
                    if (buttonProp != null)
                    {
                        buttonProp.objectReferenceValue = restartButton.GetComponent<Button>();
                    }
                }
            }
            
            if (tapButton != null)
            {
                var prop = serializedUI.FindProperty("_tapToStartButton");
                if (prop != null)
                {
                    prop.objectReferenceValue = tapButton.GetComponent<Button>();
                }
            }
            
            serializedUI.ApplyModifiedProperties();
            EditorUtility.SetDirty(uiManager);
            
            Debug.Log("[Cleanup] UIManager connections established");
        }
        
        private void ValidateSceneHierarchy()
        {
            List<string> issues = new List<string>();
            
            // Check Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                issues.Add("No Canvas found in scene");
            }
            else
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    issues.Add("Canvas Scaler not properly configured for mobile");
                }
            }
            
            // Check EventSystem
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                issues.Add("No EventSystem found in scene");
            }
            
            // Check required GameObjects
            string[] requiredObjects = {
                "GameBoard",
                "OpponentSection",
                "PlayerSection", 
                "PlayArea",
                "CardPositions",
                "PlayerCardPosition",
                "OpponentCardPosition",
                "WarPilePosition",
                "DeckPosition",
                "UIOverlay",
                "CardPoolContainer"
            };
            
            foreach (string objName in requiredObjects)
            {
                if (GameObject.Find(objName) == null)
                {
                    issues.Add($"Missing GameObject: {objName}");
                }
            }
            
            // Check Controllers
            if (FindObjectOfType<CardWar.Gameplay.Controllers.CardAnimationController>() == null)
            {
                issues.Add("CardAnimationController not found in scene");
            }
            
            if (FindObjectOfType<UIManager>() == null)
            {
                issues.Add("UIManager not found in scene");
            }
            
            // Report results
            if (issues.Count == 0)
            {
                Debug.Log("[Validation] ✅ Scene hierarchy is valid!");
            }
            else
            {
                Debug.LogError($"[Validation] Found {issues.Count} issues:");
                foreach (string issue in issues)
                {
                    Debug.LogError($"  - {issue}");
                }
            }
        }
    }
}

namespace CardWar.UI
{
    internal class UIManager
    {
    }
}
#endif