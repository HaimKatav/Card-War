using UnityEngine;
using UnityEditor;
using CardWar.Gameplay.Controllers;

namespace CardWar.Editor
{
    public class CardPositionAutoConnector : EditorWindow
    {
        [MenuItem("CardWar/🎯 Fix Card Animation References")]
        public static void ShowWindow()
        {
            GetWindow<CardPositionAutoConnector>("Card Position Connector");
        }
        
        [MenuItem("CardWar/⚡ Connect Card Positions Now")]
        public static void QuickConnect()
        {
            ConnectCardAnimationReferences();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Card Animation Reference Connector", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("This will connect CardAnimationController to scene positions:");
            GUILayout.Label("• PlayerCardPosition → _playerCardPosition");
            GUILayout.Label("• OpponentCardPosition → _opponentCardPosition");
            GUILayout.Label("• DeckPosition → _deckPosition");
            GUILayout.Label("• WarPilePosition → _warPilePosition");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Connect Card Position References", GUILayout.Height(30)))
            {
                ConnectCardAnimationReferences();
            }
            
            GUILayout.Space(10);
            ShowCurrentStatus();
        }
        
        private void ShowCurrentStatus()
        {
            GUILayout.Label("Current References Status:", EditorStyles.boldLabel);
            
            var cardAnimController = FindObjectOfType<CardAnimationController>();
            if (cardAnimController != null)
            {
                var serializedObject = new SerializedObject(cardAnimController);
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle("CardAnimationController Found", true);
                EditorGUILayout.Toggle("_playerCardPosition", serializedObject.FindProperty("_playerCardPosition").objectReferenceValue != null);
                EditorGUILayout.Toggle("_opponentCardPosition", serializedObject.FindProperty("_opponentCardPosition").objectReferenceValue != null);
                EditorGUILayout.Toggle("_deckPosition", serializedObject.FindProperty("_deckPosition").objectReferenceValue != null);
                EditorGUILayout.Toggle("_warPilePosition", serializedObject.FindProperty("_warPilePosition").objectReferenceValue != null);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.HelpBox("CardAnimationController not found in scene!", MessageType.Warning);
            }
        }
        
        private static void ConnectCardAnimationReferences()
        {
            Debug.Log("[CardPositionConnector] Connecting card animation references...");
            
            var cardAnimController = FindObjectOfType<CardAnimationController>();
            if (cardAnimController == null)
            {
                EditorUtility.DisplayDialog("Error", "CardAnimationController not found in scene!", "OK");
                return;
            }
            
            var serializedObject = new SerializedObject(cardAnimController);
            
            ConnectTransformReference(serializedObject, "_playerCardPosition", "PlayerCardPosition");
            ConnectTransformReference(serializedObject, "_opponentCardPosition", "OpponentCardPosition");
            ConnectTransformReference(serializedObject, "_deckPosition", "DeckPosition");
            ConnectTransformReference(serializedObject, "_warPilePosition", "WarPilePosition");
            
            serializedObject.ApplyModifiedProperties();
            
            EditorApplication.delayCall += () =>
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            };
            
            Debug.Log("[CardPositionConnector] Card position references connected successfully!");
            EditorUtility.DisplayDialog("References Connected!", 
                "All card position references have been connected successfully!\n\n" +
                "CardAnimationController is now ready to animate cards properly.\n\n" +
                "Press Play to test card animations!", 
                "Excellent!");
        }
        
        private static void ConnectTransformReference(SerializedObject serializedObject, string propertyName, string gameObjectName)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                var targetTransform = GameObject.Find(gameObjectName)?.transform;
                if (targetTransform != null)
                {
                    property.objectReferenceValue = targetTransform;
                    Debug.Log($"[CardPositionConnector] Connected {propertyName} to {gameObjectName}");
                }
                else
                {
                    Debug.LogWarning($"[CardPositionConnector] Could not find GameObject: {gameObjectName}");
                }
            }
        }
    }
}