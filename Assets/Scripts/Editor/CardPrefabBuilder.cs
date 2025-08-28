using UnityEngine;
using UnityEngine.UI;
using CardWar.UI.Cards;
using CardWar.Services.Assets;
using CardWar.Configuration;
using System.Reflection;

namespace CardWar.Editor.Builders
{
    public static class CardPrefabBuilder
    {
        /// <summary>
        /// Creates a card prefab with proper art integration and component setup
        /// </summary>
        public static GameObject CreateCardPrefab(IAssetService assetService, GameSettings settings)
        {
            var cardObj = new GameObject("CardPrefab");
            var rectTransform = cardObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 300);
            
            var canvasGroup = cardObj.AddComponent<CanvasGroup>();
            
            // Create card front (hidden by default)
            var frontObj = CreateCardFace(cardObj.transform, "CardFront", true);
            var frontImage = frontObj.GetComponent<Image>();
            
            // Create card back (visible by default)
            var backObj = CreateCardFace(cardObj.transform, "CardBack", false);
            var backImage = backObj.GetComponent<Image>();
            
            // Set card back sprite from asset service
            if (assetService != null)
            {
                var cardBackSprite = assetService.GetCardBackSprite();
                if (cardBackSprite != null)
                {
                    backImage.sprite = cardBackSprite;
                    Debug.Log("[CardPrefabBuilder] Card back sprite loaded successfully");
                }
                else
                {
                    Debug.LogWarning("[CardPrefabBuilder] Card back sprite not found, using placeholder");
                    SetPlaceholderSprite(backImage);
                }
            }
            
            // Add CardViewController and set references
            var cardController = cardObj.AddComponent<CardViewController>();
            
            // Use reflection to set private fields properly
            SetCardControllerReferences(cardController, frontImage, backImage, canvasGroup, rectTransform);
            
            return cardObj;
        }
        
        /// <summary>
        /// Creates card prefab with placeholder sprites for testing
        /// </summary>
        public static GameObject CreateCardPrefabWithPlaceholders()
        {
            var cardObj = new GameObject("CardPrefab");
            var rectTransform = cardObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 300);
            
            var canvasGroup = cardObj.AddComponent<CanvasGroup>();
            
            // Create card faces
            var frontObj = CreateCardFace(cardObj.transform, "CardFront", true);
            var frontImage = frontObj.GetComponent<Image>();
            
            var backObj = CreateCardFace(cardObj.transform, "CardBack", false);
            var backImage = backObj.GetComponent<Image>();
            
            // Set placeholder sprites
            SetPlaceholderSprite(frontImage);
            SetPlaceholderSprite(backImage);
            
            // Add CardViewController
            var cardController = cardObj.AddComponent<CardViewController>();
            SetCardControllerReferences(cardController, frontImage, backImage, canvasGroup, rectTransform);
            
            return cardObj;
        }
        
        /// <summary>
        /// Updates existing card prefab with art assets
        /// </summary>
        public static void UpdateCardPrefabWithArt(GameObject cardPrefab, IAssetService assetService)
        {
            if (cardPrefab == null || assetService == null) return;
            
            var cardBack = cardPrefab.transform.Find("CardBack");
            if (cardBack != null)
            {
                var backImage = cardBack.GetComponent<Image>();
                if (backImage != null)
                {
                    var cardBackSprite = assetService.GetCardBackSprite();
                    if (cardBackSprite != null)
                    {
                        backImage.sprite = cardBackSprite;
                        Debug.Log("[CardPrefabBuilder] Updated card prefab with art");
                    }
                }
            }
        }
        
        private static GameObject CreateCardFace(Transform parent, string name, bool isFront)
        {
            var faceObj = new GameObject(name);
            faceObj.transform.SetParent(parent, false);
            
            var rectTransform = faceObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            var image = faceObj.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false; // Cards don't need to receive clicks on individual faces
            
            // Set initial visibility - back is visible by default, front is hidden
            faceObj.SetActive(!isFront); 
            
            return faceObj;
        }
        
        private static void SetCardControllerReferences(CardViewController cardController, Image frontImage, Image backImage, CanvasGroup canvasGroup, RectTransform rectTransform)
        {
            if (cardController == null) return;
            
            var cardViewControllerType = typeof(CardViewController);
            
            // Get private fields using reflection
            var cardFrontField = cardViewControllerType.GetField("_cardFront", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var cardBackField = cardViewControllerType.GetField("_cardBack", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var canvasGroupField = cardViewControllerType.GetField("_canvasGroup", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            var rectTransformField = cardViewControllerType.GetField("_rectTransform", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Set the references
            cardFrontField?.SetValue(cardController, frontImage);
            cardBackField?.SetValue(cardController, backImage);
            canvasGroupField?.SetValue(cardController, canvasGroup);
            rectTransformField?.SetValue(cardController, rectTransform);
            
            Debug.Log("[CardPrefabBuilder] Card controller references set via reflection");
        }
        
        private static void SetPlaceholderSprite(Image image)
        {
            if (image == null) return;
            
            // Create a simple colored sprite as placeholder
            var texture = new Texture2D(100, 140, TextureFormat.RGBA32, false);
            var pixels = new Color[100 * 140];
            
            // Fill with a card-like pattern
            for (int x = 0; x < 100; x++)
            {
                for (int y = 0; y < 140; y++)
                {
                    int index = y * 100 + x;
                    
                    // Create border
                    if (x < 5 || x >= 95 || y < 5 || y >= 135)
                    {
                        pixels[index] = Color.black;
                    }
                    // Create inner area
                    else if (x < 15 || x >= 85 || y < 15 || y >= 125)
                    {
                        pixels[index] = Color.white;
                    }
                    // Create center
                    else
                    {
                        pixels[index] = new Color(0.9f, 0.9f, 0.9f, 1f);
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 100, 140),
                new Vector2(0.5f, 0.5f),
                100f
            );
            
            image.sprite = sprite;
        }
        
        /// <summary>
        /// Validates that a card prefab has all required components
        /// </summary>
        public static bool ValidateCardPrefab(GameObject cardPrefab)
        {
            if (cardPrefab == null)
            {
                Debug.LogError("[CardPrefabBuilder] Card prefab is null");
                return false;
            }
            
            var issues = new System.Collections.Generic.List<string>();
            
            // Check for required components
            var cardController = cardPrefab.GetComponent<CardViewController>();
            if (cardController == null)
                issues.Add("CardViewController component missing");
            
            var rectTransform = cardPrefab.GetComponent<RectTransform>();
            if (rectTransform == null)
                issues.Add("RectTransform component missing");
            
            var canvasGroup = cardPrefab.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                issues.Add("CanvasGroup component missing");
            
            // Check for required child objects
            var frontChild = cardPrefab.transform.Find("CardFront");
            if (frontChild == null)
                issues.Add("CardFront child object missing");
            else if (frontChild.GetComponent<Image>() == null)
                issues.Add("CardFront Image component missing");
            
            var backChild = cardPrefab.transform.Find("CardBack");
            if (backChild == null)
                issues.Add("CardBack child object missing");
            else if (backChild.GetComponent<Image>() == null)
                issues.Add("CardBack Image component missing");
            
            if (issues.Count > 0)
            {
                Debug.LogError($"[CardPrefabBuilder] Card prefab validation failed: {string.Join(", ", issues)}");
                return false;
            }
            
            Debug.Log("[CardPrefabBuilder] Card prefab validation passed");
            return true;
        }
        
        /// <summary>
        /// Creates multiple card prefab variants for testing
        /// </summary>
        public static GameObject[] CreateCardPrefabVariants(IAssetService assetService, GameSettings settings)
        {
            var variants = new GameObject[3];
            
            // Standard card
            variants[0] = CreateCardPrefab(assetService, settings);
            variants[0].name = "CardPrefab_Standard";
            
            // Small card for mobile
            variants[1] = CreateCardPrefab(assetService, settings);
            variants[1].name = "CardPrefab_Small";
            variants[1].GetComponent<RectTransform>().sizeDelta = new Vector2(150, 225);
            
            // Large card for desktop
            variants[2] = CreateCardPrefab(assetService, settings);
            variants[2].name = "CardPrefab_Large";
            variants[2].GetComponent<RectTransform>().sizeDelta = new Vector2(250, 375);
            
            return variants;
        }
    }
}