using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using CardWar.Core.Data;
using CardWar.Core.Enums;

namespace CardWar.Services.Assets
{
    public interface ICardAssetManager
    {
        Sprite GetCardSprite(CardData card);
        Sprite GetCardBackSprite();
        UniTask PreloadAllCardAssets();
        bool AreAssetsLoaded { get; }
    }
    
    public class CardAssetManager : ICardAssetManager, IDisposable
    {
        private const string CARDS_PATH = "Cards/";
        private const string CARD_BACK_NAME = "card_back";
        
        private readonly Dictionary<string, Sprite> _cardSprites = new Dictionary<string, Sprite>();
        private Sprite _cardBackSprite;
        private Sprite _placeholderSprite;
        private bool _assetsLoaded = false;
        
        public bool AreAssetsLoaded => _assetsLoaded;
        
        public CardAssetManager()
        {
            Debug.Log("[CardAssetManager] Initializing");
            CreatePlaceholderSprite();
        }
        
        public async UniTask PreloadAllCardAssets()
        {
            if (_assetsLoaded)
            {
                Debug.Log("[CardAssetManager] Assets already loaded");
                return;
            }
            
            Debug.Log("[CardAssetManager] Starting to preload card assets");
            
            // Load card back
            _cardBackSprite = LoadSprite(CARD_BACK_NAME);
            if (_cardBackSprite == null)
            {
                Debug.LogWarning($"[CardAssetManager] Card back sprite not found at: {CARDS_PATH}{CARD_BACK_NAME}");
                _cardBackSprite = _placeholderSprite;
            }
            
            // Load all card faces
            int loadedCount = 0;
            int missingCount = 0;
            
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardRank rank in Enum.GetValues(typeof(CardRank)))
                {
                    string cardId = GetCardId(suit, rank);
                    string spriteName = GetSpriteName(suit, rank);
                    
                    Sprite sprite = LoadSprite(spriteName);
                    if (sprite != null)
                    {
                        _cardSprites[cardId] = sprite;
                        loadedCount++;
                    }
                    else
                    {
                        // Use placeholder for missing sprites
                        _cardSprites[cardId] = CreateCardPlaceholder(suit, rank);
                        missingCount++;
                        
                        if (missingCount <= 5) // Only log first few missing
                        {
                            Debug.LogWarning($"[CardAssetManager] Card sprite not found: {CARDS_PATH}{spriteName}");
                        }
                    }
                }
            }
            
            _assetsLoaded = true;
            
            Debug.Log($"[CardAssetManager] Asset loading complete. Loaded: {loadedCount}, Missing: {missingCount}");
            
            if (missingCount > 0)
            {
                Debug.LogWarning($"[CardAssetManager] {missingCount} card sprites are missing. Using placeholders.");
                Debug.Log("[CardAssetManager] Expected sprite naming format: [rank]_[suit].png");
                Debug.Log("[CardAssetManager] Examples: 2_hearts, king_spades, ace_diamonds");
            }
            
            // Small delay to simulate async loading
            await UniTask.Delay(100);
        }
        
        public Sprite GetCardSprite(CardData card)
        {
            if (card == null)
            {
                Debug.LogWarning("[CardAssetManager] Attempted to get sprite for null card");
                return _placeholderSprite;
            }
            
            string cardId = GetCardId(card.Suit, card.Rank);
            
            if (_cardSprites.TryGetValue(cardId, out Sprite sprite))
            {
                return sprite;
            }
            
            Debug.LogWarning($"[CardAssetManager] Sprite not found for card: {cardId}");
            return CreateCardPlaceholder(card.Suit, card.Rank);
        }
        
        public Sprite GetCardBackSprite()
        {
            return _cardBackSprite ?? _placeholderSprite;
        }
        
        private Sprite LoadSprite(string spriteName)
        {
            string fullPath = CARDS_PATH + spriteName;
            Sprite sprite = Resources.Load<Sprite>(fullPath);
            return sprite;
        }
        
        private string GetCardId(CardSuit suit, CardRank rank)
        {
            return $"{rank}_{suit}".ToLower();
        }
        
        private string GetSpriteName(CardSuit suit, CardRank rank)
        {
            string rankName = GetRankName(rank);
            string suitName = suit.ToString().ToLower();
            return $"{rankName}_{suitName}";
        }
        
        private string GetRankName(CardRank rank)
        {
            return rank switch
            {
                CardRank.Jack => "jack",
                CardRank.Queen => "queen",
                CardRank.King => "king",
                CardRank.Ace => "ace",
                _ => ((int)rank).ToString()
            };
        }
        
        private void CreatePlaceholderSprite()
        {
            // Create a simple colored texture as placeholder
            Texture2D texture = new Texture2D(100, 140, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[100 * 140];
            
            // Fill with gray color
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            _placeholderSprite = Sprite.Create(
                texture,
                new Rect(0, 0, 100, 140),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }
        
        private Sprite CreateCardPlaceholder(CardSuit suit, CardRank rank)
        {
            // Create a colored placeholder based on suit
            Color suitColor = GetSuitColor(suit);
            
            Texture2D texture = new Texture2D(100, 140, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[100 * 140];
            
            // Fill with suit color
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = suitColor;
            }
            
            // Add border
            for (int x = 0; x < 100; x++)
            {
                for (int y = 0; y < 140; y++)
                {
                    if (x < 2 || x >= 98 || y < 2 || y >= 138)
                    {
                        pixels[y * 100 + x] = Color.black;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(
                texture,
                new Rect(0, 0, 100, 140),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }
        
        private Color GetSuitColor(CardSuit suit)
        {
            return suit switch
            {
                CardSuit.Hearts => new Color(0.9f, 0.2f, 0.2f, 1f),    // Red
                CardSuit.Diamonds => new Color(0.9f, 0.4f, 0.1f, 1f),  // Orange-red
                CardSuit.Clubs => new Color(0.2f, 0.2f, 0.2f, 1f),     // Black
                CardSuit.Spades => new Color(0.1f, 0.1f, 0.3f, 1f),    // Dark blue
                _ => Color.gray
            };
        }
        
        public void Dispose()
        {
            Debug.Log("[CardAssetManager] Disposing");
            
            // Clear sprite cache
            _cardSprites.Clear();
            
            // Destroy placeholder textures
            if (_placeholderSprite != null && _placeholderSprite.texture != null)
            {
                UnityEngine.Object.Destroy(_placeholderSprite.texture);
                UnityEngine.Object.Destroy(_placeholderSprite);
            }
            
            _cardBackSprite = null;
            _assetsLoaded = false;
        }
    }
}