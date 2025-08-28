using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CardWar.Editor
{
    public class CardNameFixer : EditorWindow
    {
        [MenuItem("CardWar/Tools/Quick Card Name Fixer")]
        public static void ShowWindow()
        {
            GetWindow<CardNameFixer>("Quick Card Name Fixer");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Quick Card Name Fixes", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            // EditorGUILayout.HelpBox(
            //     "This will fix the remaining card naming issues:\n" +
            //     "• Rename 'card_back' image properly\n" +
            //     "• Fix any remaining numbered cards\n" +
            //     "• Ensure all cards follow the pattern: rank_suit",
            //     MessageType.Info);
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Show Required Renames", GUILayout.Height(30)))
            {
                ShowRequiredRenames();
            }
            
            GUILayout.Space(10);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Apply Quick Fixes", GUILayout.Height(40)))
            {
                ApplyQuickFixes();
            }
            GUI.backgroundColor = Color.white;
        }
        
        private void ShowRequiredRenames()
        {
            Debug.Log("=== Required Card Renames ===");
            Debug.Log("card_back image should be named: card_back (no extension in Unity)");
            Debug.Log("All cards should follow pattern: rank_suit");
            Debug.Log("Example: 2_hearts, 10_diamonds, ace_spades, king_clubs");
            Debug.Log("");
            Debug.Log("Based on console errors, missing:");
            Debug.Log("• 2_hearts through 10_hearts");
            Debug.Log("• 2_diamonds through 10_diamonds");
            Debug.Log("• 2_clubs through 10_clubs");
            Debug.Log("• 2_spades through 10_spades");
            Debug.Log("• jack_[all suits], queen_[all suits], king_[all suits], ace_[all suits]");
        }
        
        private void ApplyQuickFixes()
        {
            // Dictionary of what we see in your project -> what it should be
            Dictionary<string, string> fixMap = new Dictionary<string, string>
            {
                // Fix numbered cards that are still showing wrong names
                {"2_clubs", "2_clubs"},
                {"3_clubs", "3_clubs"},
                {"4_clubs", "4_clubs"},
                {"5_clubs", "5_clubs"},
                {"6_clubs", "6_clubs"},
                {"7_clubs", "7_clubs"},
                {"8_clubs", "8_clubs"},
                {"9_clubs", "9_clubs"},
                {"10_clubs", "10_clubs"},
                
                {"2_diamonds", "2_diamonds"},
                {"3_diamonds", "3_diamonds"},
                {"4_diamonds", "4_diamonds"},
                {"5_diamonds", "5_diamonds"},
                {"6_diamonds", "6_diamonds"},
                {"7_diamonds", "7_diamonds"},
                {"8_diamonds", "8_diamonds"},
                {"9_diamonds", "9_diamonds"},
                {"10_diamonds", "10_diamonds"},
                
                {"2_hearts", "2_hearts"},
                {"3_hearts", "3_hearts"},
                {"4_hearts", "4_hearts"},
                {"5_hearts", "5_hearts"},
                {"6_hearts", "6_hearts"},
                {"7_hearts", "7_hearts"},
                {"8_hearts", "8_hearts"},
                {"9_hearts", "9_hearts"},
                {"10_hearts", "10_hearts"},
                
                {"2_spades", "2_spades"},
                {"3_spades", "3_spades"},
                {"4_spades", "4_spades"},
                {"5_spades", "5_spades"},
                {"6_spades", "6_spades"},
                {"7_spades", "7_spades"},
                {"8_spades", "8_spades"},
                {"9_spades", "9_spades"},
                {"10_spades", "10_spades"},
                
                // Face cards
                {"jack_clubs", "jack_clubs"},
                {"queen_clubs", "queen_clubs"},
                {"king_clubs", "king_clubs"},
                {"ace_clubs", "ace_clubs"},
                
                {"jack_diamonds", "jack_diamonds"},
                {"queen_diamonds", "queen_diamonds"},
                {"king_diamonds", "king_diamonds"},
                {"ace_diamonds", "ace_diamonds"},
                
                {"jack_hearts", "jack_hearts"},
                {"queen_hearts", "queen_hearts"},
                {"king_hearts", "king_hearts"},
                {"ace_hearts", "ace_hearts"},
                
                {"jack_spades", "jack_spades"},
                {"queen_spades", "queen_spades"},
                {"king_spades", "king_spades"},
                {"ace_spades", "ace_spades"},
            };
            
            Debug.Log("[CardNameFixer] Checking all card names...");
            
            // Just verify that files exist with correct names
            string[] allFiles = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art/Cards" });
            HashSet<string> existingNames = new HashSet<string>();
            
            foreach (string guid in allFiles)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                existingNames.Add(fileName.ToLower());
                Debug.Log($"Found card: {fileName}");
            }
            
            // Check what's missing
            List<string> missing = new List<string>();
            foreach (var expectedName in fixMap.Keys)
            {
                if (!existingNames.Contains(expectedName.ToLower()))
                {
                    missing.Add(expectedName);
                }
            }
            
            if (missing.Count > 0)
            {
                Debug.LogError($"[CardNameFixer] Missing {missing.Count} cards:");
                foreach (string card in missing)
                {
                    Debug.LogError($"  • {card}");
                }
                
                EditorUtility.DisplayDialog("Missing Cards",
                    $"Found {missing.Count} missing card names.\n" +
                    "Please check the console for details.\n\n" +
                    "The existing cards may need manual renaming.",
                    "OK");
            }
            else
            {
                Debug.Log("[CardNameFixer] All cards have correct names!");
                EditorUtility.DisplayDialog("Success", "All cards are correctly named!", "OK");
            }
            
            // Special check for card_back
            if (!existingNames.Contains("card_back"))
            {
                Debug.LogWarning("[CardNameFixer] card_back not found - please rename your card back image to 'card_back'");
            }
            
            AssetDatabase.Refresh();
        }
    }
}