using System.Collections.Generic;
using CardWar.View.Cards;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Interface for card deck visualization and management
    /// </summary>
    public interface ICardDeck
    {
        int Count { get; }
        Transform Transform { get; }
        Vector3 DrawPosition { get; }
        
        // Deck Operations
        void SetCardCount(int count);
        UniTask AnimateDrawAsync(CardView card);
        UniTask AnimateReturnAsync(CardView card);
        UniTask AnimateShuffleAsync();
        
        // Visual Updates
        void UpdateDeckVisual(int remainingCards);
        void SetHighlight(bool highlighted);
    }
}