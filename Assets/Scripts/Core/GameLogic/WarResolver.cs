using System;
using System.Collections.Generic;
using System.Linq;
using CardWar.Core.Data;
using UnityEngine;

namespace CardWar.Core.GameLogic
{
    /// <summary>
    /// Resolves complete war scenarios by calculating the entire war to completion
    /// and providing all necessary data for visual representation
    /// </summary>
    public class WarResolver
    {
        private const int MAX_CONCEALED_CARDS = 3;
        private const float BASE_ANIMATION_DURATION = 0.5f;
        private const float CARD_FLIP_DURATION = 0.3f;
        private const float WAR_ROUND_DURATION = 1.0f;
        
        /// <summary>
        /// Resolves a complete war scenario from start to finish
        /// </summary>
        /// <param name="initialCards">Cards that initiated the war</param>
        /// <param name="playerDeck">Player's current deck</param>
        /// <param name="opponentDeck">Opponent's current deck</param>
        /// <param name="parentWar">Parent war if this is a consecutive war</param>
        /// <returns>Complete war data with all rounds and final results</returns>
        public WarData ResolveWar(
            List<CardData> initialCards,
            Queue<CardData> playerDeck,
            Queue<CardData> opponentDeck)
        {
            var warData = new WarData
            {
                InitialWarCards = new List<CardData>(initialCards)
            };
            
            // Calculate maximum concealed cards based on available cards
            var maxConcealedPlayer = Math.Min(MAX_CONCEALED_CARDS, playerDeck.Count - 1);
            var maxConcealedOpponent = Math.Min(MAX_CONCEALED_CARDS, opponentDeck.Count - 1);
            var maxConcealedCards = Math.Min(maxConcealedPlayer, maxConcealedOpponent);
            
            // Initialize card tracking
            var allCardsInWar = new List<CardData>(initialCards);
            var currentRound = 1;
            
            // Process war rounds until resolution
            while (true)
            {
                var warRound = ProcessWarRound(
                    currentRound,
                    allCardsInWar,
                    playerDeck,
                    opponentDeck,
                    maxConcealedCards);
                
                warData.AllWarRounds.Add(warRound);
                allCardsInWar.AddRange(warRound.ConcealedCards.Values.SelectMany(cards => cards));
                allCardsInWar.AddRange(warRound.FightingCards.Values);
                
                // Check if war continues
                if (warRound.ResultedInWar)
                {
                    currentRound++;
                    
                    // Check if either player ran out of cards
                    if (playerDeck.Count == 0 || opponentDeck.Count == 0)
                    {
                        // Handle edge case: players ran out of cards
                        warData.RequiresShuffle = true;
                        warData.WinningPlayerNumber = playerDeck.Count > opponentDeck.Count ? 1 : 2;
                        warData.TotalCardsWon = allCardsInWar.Count;
                        break;
                    }
                }
                else
                {
                    // War resolved - determine winner
                    var playerFightingCard = warRound.FightingCards[1];
                    var opponentFightingCard = warRound.FightingCards[2];
                    
                    if (playerFightingCard.Value > opponentFightingCard.Value)
                    {
                        warData.WinningPlayerNumber = 1;
                        warData.TotalCardsWon = allCardsInWar.Count;
                        
                        // Add all cards to player deck
                        foreach (var card in allCardsInWar)
                        {
                            playerDeck.Enqueue(card);
                        }
                    }
                    else if (opponentFightingCard.Value > playerFightingCard.Value)
                    {
                        warData.WinningPlayerNumber = 2;
                        warData.TotalCardsWon = allCardsInWar.Count;
                        
                        // Add all cards to opponent deck
                        foreach (var card in allCardsInWar)
                        {
                            opponentDeck.Enqueue(card);
                        }
                    }
                    else
                    {
                        // Another tie - continue war
                        warRound.ResultedInWar = true;
                        currentRound++;
                        continue;
                    }
                    break;
                }
            }
            
            // Calculate final data
            warData.RequiredPoolSize = CalculateRequiredPoolSize(warData);
            warData.EstimatedAnimationDuration = CalculateAnimationDuration(warData);
            
            // Set final card distribution
            warData.FinalCardDistribution[1] = new List<CardData>();
            warData.FinalCardDistribution[2] = new List<CardData>();
            
            if (warData.WinningPlayerNumber == 1)
            {
                warData.FinalCardDistribution[1] = new List<CardData>(allCardsInWar);
            }
            else
            {
                warData.FinalCardDistribution[2] = new List<CardData>(allCardsInWar);
            }
            
            Debug.Log($"[WarResolver] War resolved: Player {warData.WinningPlayerNumber} wins {warData.TotalCardsWon} cards in {warData.AllWarRounds.Count} rounds");
            
            return warData;
        }
        
        private WarRound ProcessWarRound(
            int roundNumber,
            List<CardData> accumulatedCards,
            Queue<CardData> playerDeck,
            Queue<CardData> opponentDeck,
            int maxConcealedCards)
        {
            var warRound = new WarRound
            {
                RoundNumber = roundNumber,
                TotalCardsAccumulated = accumulatedCards.Count
            };
            
            // Place concealed cards
            var playerConcealed = new List<CardData>();
            var opponentConcealed = new List<CardData>();
            
            for (int i = 0; i < maxConcealedCards; i++)
            {
                if (playerDeck.Count > 1) // Leave 1 card for fighting
                {
                    playerConcealed.Add(playerDeck.Dequeue());
                }
                
                if (opponentDeck.Count > 1) // Leave 1 card for fighting
                {
                    opponentConcealed.Add(opponentDeck.Dequeue());
                }
            }
            
            warRound.ConcealedCards[1] = playerConcealed;
            warRound.ConcealedCards[2] = opponentConcealed;
            
            // Draw fighting cards
            if (playerDeck.Count > 0 && opponentDeck.Count > 0)
            {
                warRound.FightingCards[1] = playerDeck.Dequeue();
                warRound.FightingCards[2] = opponentDeck.Dequeue();
                
                // Check if this round results in another war
                if (warRound.FightingCards[1].Value == warRound.FightingCards[2].Value)
                {
                    warRound.ResultedInWar = true;
                }
            }
            
            return warRound;
        }
        
        private int CalculateRequiredPoolSize(WarData warData)
        {
            // Base size: initial cards + all concealed cards + all fighting cards
            var baseSize = warData.InitialWarCards.Count;
            
            foreach (var round in warData.AllWarRounds)
            {
                baseSize += round.ConcealedCards.Values.Sum(cards => cards.Count);
                baseSize += round.FightingCards.Count;
            }
            
            // Add some buffer for smooth animations
            return Math.Max(16, baseSize + 4);
        }
        
        private float CalculateAnimationDuration(WarData warData)
        {
            var duration = BASE_ANIMATION_DURATION; // Initial card reveal
            
            foreach (var round in warData.AllWarRounds)
            {
                // Concealed cards placement
                duration += round.ConcealedCards.Values.Sum(cards => cards.Count) * 0.1f;
                
                // Fighting cards reveal
                duration += CARD_FLIP_DURATION * round.FightingCards.Count;
                
                // War round pause
                duration += WAR_ROUND_DURATION;
            }
            
            // Final result animation
            duration += 1.0f;
            
            return duration;
        }
    }
}

