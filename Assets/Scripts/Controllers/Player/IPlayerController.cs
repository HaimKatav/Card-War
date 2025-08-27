using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CardWar.Core.Data;

namespace CardWar.Gameplay.Players
{
    /// <summary>
    /// Interface for all player controllers (local player and AI opponent)
    /// </summary>
    public interface IPlayerController : IDisposable
    {
        // Identity
        int PlayerNumber { get; }
        string PlayerName { get; }
        bool IsLocalPlayer { get; }
        
        // State
        bool IsMyTurn { get; }
        int CardCount { get; }
        bool HasCards { get; }
        
        // Components
        ICardDeck Deck { get; }
        ICardSlot CardSlot { get; }
        Transform DeckTransform { get; }
        Transform CardSlotTransform { get; }
        
        // Events
        event Action<IPlayerController> OnTurnStarted;
        event Action<IPlayerController> OnTurnEnded;
        event Action<CardData> OnCardPlayed;
        event Action<int> OnCardCountChanged;
        
        // Initialization
        UniTask InitializeAsync(PlayerConfiguration config);
        
        // Game Actions
        UniTask<CardData> PlayCardAsync();
        UniTask ShowCardAsync(CardData card);
        UniTask CollectCardsAsync(List<CardData> wonCards);
        UniTask ReturnCardsAsync(List<CardData> cards);
        
        // War Actions
        UniTask<List<CardData>> PlaceConcealedCardsAsync(int count);
        UniTask<CardData> PlaceFightingCardAsync();
        UniTask ShowWarResultAsync(bool won, List<CardData> cardsWon);
        
        // Turn Management
        void StartTurn();
        void EndTurn();
        
        // Visual Updates
        void UpdateCardCountDisplay(int count);
        void SetInteractable(bool interactable);
        void HighlightPlayer(bool highlight);
        
        // Cleanup
        void Reset();
    }
}