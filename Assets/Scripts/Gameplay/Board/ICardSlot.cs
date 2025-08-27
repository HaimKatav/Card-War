using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CardWar.View.Cards;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Interface for card placement slots
    /// </summary>
    public interface ICardSlot
    {
        Transform Transform { get; }
        Vector3 Position { get; }
        CardView CurrentCard { get; }
        List<CardView> ConcealedCards { get; }
        bool HasCard { get; }
        
        // Card Placement
        UniTask PlaceCardAsync(CardView card, bool animate = true);
        UniTask RemoveCardAsync(bool animate = true);
        CardView RemoveCardImmediate();
        
        // War Card Management
        UniTask PlaceConcealedCardAsync(CardView card, int index);
        UniTask PlaceWarStackIndicator(int totalCards);
        UniTask ClearWarCards();
        List<CardView> GetAllCards();
        
        // Visual
        void SetHighlight(bool highlighted);
        void ShowCardCount(int count);
    }
}