using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace CardWar.Editor
{
    public class CardAssetRenamer : EditorWindow
    {
        private string _sourceFolder = "Assets/Art/Cards";
        private bool _createBackup = true;
        private bool _previewMode = true;
        private Vector2 _scrollPosition;
        private Dictionary<string, string> _renamingMap = new Dictionary<string, string>();
        
        [MenuItem("CardWar/Tools/Card Asset Renamer")]
        public static void ShowWindow()
        {
            GetWindow<CardAssetRenamer>("Card Asset Renamer");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Card Asset Renaming Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool will rename card assets from abbreviated format (2H, AS) to full format (2_hearts, ace_spades)",
                MessageType.Info);
            
            GUILayout.Space(10);
            
            _sourceFolder = EditorGUILayout.TextField("Cards Folder:", _sourceFolder);
            _createBackup = EditorGUILayout.Toggle("Create Backup", _createBackup);
            _previewMode = EditorGUILayout.Toggle("Preview Mode", _previewMode);
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Analyze Assets", GUILayout.Height(30)))
            {
                AnalyzeAssets();
            }
            
            if (_renamingMap.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label($"Found {_renamingMap.Count} assets to rename:", EditorStyles.boldLabel);
                
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(300));
                foreach (var kvp in _renamingMap)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(150));
                    EditorGUILayout.LabelField("→", GUILayout.Width(20));
                    EditorGUILayout.LabelField(kvp.Value);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
                
                GUILayout.Space(20);
                
                GUI.backgroundColor = _previewMode ? Color.yellow : Color.green;
                string buttonText = _previewMode ? "⚠️ Preview Changes (Safe)" : "✅ Apply Renaming (Permanent)";
                
                if (GUILayout.Button(buttonText, GUILayout.Height(40)))
                {
                    if (_previewMode)
                    {
                        PreviewRenaming();
                    }
                    else
                    {
                        if (EditorUtility.DisplayDialog("Confirm Renaming",
                            "This will rename all card assets. Are you sure?\n\n" +
                            (_createBackup ? "A backup will be created." : "NO BACKUP will be created!"),
                            "Yes, Rename", "Cancel"))
                        {
                            ApplyRenaming();
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
            }
        }
        
        private void AnalyzeAssets()
        {
            _renamingMap.Clear();
            
            if (!AssetDatabase.IsValidFolder(_sourceFolder))
            {
                EditorUtility.DisplayDialog("Error", "Invalid folder path: " + _sourceFolder, "OK");
                return;
            }
            
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { _sourceFolder });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                
                string newName = ConvertCardName(fileName);
                if (newName != fileName && !string.IsNullOrEmpty(newName))
                {
                    _renamingMap[fileName] = newName;
                }
            }
            
            if (_renamingMap.Count == 0)
            {
                EditorUtility.DisplayDialog("Info", "No assets need renaming. They may already be in the correct format.", "OK");
            }
            else
            {
                Debug.Log($"[CardAssetRenamer] Found {_renamingMap.Count} assets to rename");
            }
        }
        
        private string ConvertCardName(string oldName)
        {
            // Handle special cases first
            if (oldName.ToLower() == "card_back" || oldName.ToLower() == "cardback")
                return "card_back";
                
            // Remove any spaces or special characters
            oldName = oldName.Replace(" ", "").Replace("-", "").Replace("_", "");
            
            // Check if it's already in correct format
            if (oldName.Contains("_"))
            {
                string[] parts = oldName.Split('_');
                if (parts.Length == 2 && IsValidSuitName(parts[1]))
                    return oldName; // Already correct
            }
            
            // Parse abbreviated format (2H, AS, KD, etc.)
            if (oldName.Length >= 2)
            {
                string rankPart = "";
                string suitPart = "";
                
                // Check last character for suit
                char lastChar = oldName[oldName.Length - 1];
                string suit = GetFullSuitName(lastChar);
                
                if (!string.IsNullOrEmpty(suit))
                {
                    rankPart = oldName.Substring(0, oldName.Length - 1);
                    suitPart = suit;
                }
                else
                {
                    // Maybe it's in format like "Hearts2" or "2Hearts"
                    foreach (string suitName in new[] { "hearts", "diamonds", "clubs", "spades" })
                    {
                        if (oldName.ToLower().Contains(suitName))
                        {
                            suitPart = suitName;
                            rankPart = oldName.ToLower().Replace(suitName, "");
                            break;
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(rankPart) && !string.IsNullOrEmpty(suitPart))
                {
                    string rank = GetFullRankName(rankPart);
                    if (!string.IsNullOrEmpty(rank))
                    {
                        return $"{rank}_{suitPart}";
                    }
                }
            }
            
            Debug.LogWarning($"[CardAssetRenamer] Could not parse card name: {oldName}");
            return "";
        }
        
        private string GetFullSuitName(char suitChar)
        {
            switch (char.ToUpper(suitChar))
            {
                case 'H': return "hearts";
                case 'D': return "diamonds";
                case 'C': return "clubs";
                case 'S': return "spades";
                default: return "";
            }
        }
        
        private string GetFullRankName(string rankStr)
        {
            rankStr = rankStr.ToUpper().Trim();
            
            // Handle face cards
            switch (rankStr)
            {
                case "J": case "JACK": return "jack";
                case "Q": case "QUEEN": return "queen";
                case "K": case "KING": return "king";
                case "A": case "ACE": case "1": return "ace";
            }
            
            // Handle number cards
            if (int.TryParse(rankStr, out int rankNum))
            {
                if (rankNum >= 2 && rankNum <= 10)
                    return rankNum.ToString();
            }
            
            // Handle written numbers
            switch (rankStr.ToLower())
            {
                case "two": return "2";
                case "three": return "3";
                case "four": return "4";
                case "five": return "5";
                case "six": return "6";
                case "seven": return "7";
                case "eight": return "8";
                case "nine": return "9";
                case "ten": return "10";
            }
            
            return "";
        }
        
        private bool IsValidSuitName(string suit)
        {
            suit = suit.ToLower();
            return suit == "hearts" || suit == "diamonds" || suit == "clubs" || suit == "spades";
        }
        
        private void PreviewRenaming()
        {
            Debug.Log("[CardAssetRenamer] === PREVIEW MODE - No actual changes will be made ===");
            
            foreach (var kvp in _renamingMap)
            {
                Debug.Log($"Would rename: {kvp.Key} → {kvp.Value}");
            }
            
            Debug.Log($"[CardAssetRenamer] Total files to rename: {_renamingMap.Count}");
        }
        
        private void ApplyRenaming()
        {
            if (_createBackup)
            {
                CreateBackup();
            }
            
            int successCount = 0;
            int errorCount = 0;
            List<string> errors = new List<string>();
            
            foreach (var kvp in _renamingMap)
            {
                string[] guids = AssetDatabase.FindAssets(kvp.Key + " t:Sprite", new[] { _sourceFolder });
                
                foreach (string guid in guids)
                {
                    string oldPath = AssetDatabase.GUIDToAssetPath(guid);
                    string directory = Path.GetDirectoryName(oldPath);
                    string extension = Path.GetExtension(oldPath);
                    string newPath = Path.Combine(directory, kvp.Value + extension);
                    
                    string result = AssetDatabase.RenameAsset(oldPath, kvp.Value);
                    
                    if (string.IsNullOrEmpty(result))
                    {
                        successCount++;
                        Debug.Log($"[CardAssetRenamer] Renamed: {kvp.Key} → {kvp.Value}");
                    }
                    else
                    {
                        errorCount++;
                        errors.Add($"{kvp.Key}: {result}");
                        Debug.LogError($"[CardAssetRenamer] Failed to rename {kvp.Key}: {result}");
                    }
                }
            }
            
            AssetDatabase.Refresh();
            
            string message = $"Renaming complete!\n\nSuccess: {successCount}\nErrors: {errorCount}";
            if (errors.Count > 0)
            {
                message += "\n\nErrors:\n" + string.Join("\n", errors);
            }
            
            EditorUtility.DisplayDialog("Renaming Complete", message, "OK");
            
            // Clear the map after applying
            _renamingMap.Clear();
        }
        
        private void CreateBackup()
        {
            string backupFolder = _sourceFolder + "_Backup_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(backupFolder)))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(_sourceFolder), Path.GetFileName(backupFolder));
            }
            
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { _sourceFolder });
            
            foreach (string guid in guids)
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(sourcePath);
                string destPath = Path.Combine(backupFolder, fileName);
                
                AssetDatabase.CopyAsset(sourcePath, destPath);
            }
            
            Debug.Log($"[CardAssetRenamer] Backup created at: {backupFolder}");
        }
    }
}