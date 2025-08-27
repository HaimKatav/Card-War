using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;
using Assets.Scripts.Player;
using Assets.Scripts.Services.AssetManagement;
using CardWar.Core.Data;
using CardWar.Core.Events;
using CardWar.Services.Network;
using CardWar.View.Cards;

namespace Assets.Scripts.Services
{
    /// <summary>
    /// Controls the visual representation of war scenarios based on complete WarData
    /// </summary>
    public class WarAnimationController : IDisposable
    {
        private readonly IAssetManager _assetManager;
        private readonly SignalBus _signalBus;
        private readonly CardView.Pool _cardPool;
        private readonly IAnimationService _animationService;
        
        private bool _isAnimating;
        private CancellationTokenSource _animationCancellationToken;
        
        [Inject]
        public WarAnimationController(
            IAssetManager assetManager,
            SignalBus signalBus,
            CardView.Pool cardPool,
            IAnimationService animationService)
        {
            _assetManager = assetManager;
            _signalBus = signalBus;
            _cardPool = cardPool;
            _animationService = animationService;
            _animationCancellationToken = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Executes the complete war animation sequence based on WarData
        /// </summary>
        public async UniTask ExecuteWarAnimationAsync(WarData warData, IPlayerController localPlayer, IPlayerController aiPlayer)
        {
            if (_isAnimating)
            {
                Debug.LogWarning("[WarAnimationController] War animation already in progress");
                return;
            }
            
            _isAnimating = true;
            Debug.Log($"[WarAnimationController] Starting war animation with {warData.TotalCardsWon} cards");
            
            try
            {
                // Fire war started signal
                _signalBus.Fire(new WarStartedEvent(warData));
                
                // Resize card pool if needed
                if (warData.RequiredPoolSize > 16)
                {
                    _signalBus.Fire(new PoolResizeEvent(warData.RequiredPoolSize));
                    await UniTask.Delay(100); // Brief delay for pool resize
                }
                
                // Execute war animation sequence
                await ExecuteWarSequenceAsync(warData, localPlayer, aiPlayer);
                
                // Show final result
                await ShowWarResultAsync(warData, localPlayer, aiPlayer);
                
                Debug.Log($"[WarAnimationController] War animation completed successfully");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[WarAnimationController] War animation was cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WarAnimationController] War animation failed: {ex.Message}");
            }
            finally
            {
                _isAnimating = false;
            }
        }
        
        private async UniTask ExecuteWarSequenceAsync(WarData warData, IPlayerController localPlayer, IPlayerController aiPlayer)
        {
            // Show initial war cards
            await ShowInitialWarCardsAsync(warData, localPlayer, aiPlayer);
            
            // Execute each war round
            foreach (var warRound in warData.AllWarRounds)
            {
                await ExecuteWarRoundAsync(warRound, localPlayer, aiPlayer);
                
                // Pause between rounds if there are more
                if (warRound != warData.AllWarRounds.Last())
                {
                    await UniTask.Delay(500, cancellationToken: _animationCancellationToken.Token);
                }
            }
        }
        
        private async UniTask ShowInitialWarCardsAsync(WarData warData, IPlayerController localPlayer, IPlayerController aiPlayer)
        {
            Debug.Log("[WarAnimationController] Showing initial war cards");
            
            // Show the cards that initiated the war
            foreach (var card in warData.InitialWarCards)
            {
                var cardView = _cardPool.Spawn();
                cardView.Setup(card);
                cardView.SetFaceUp(true, immediate: true);
                
                // Position card in war area
                // TODO: Position cards in war area based on player
                
                await UniTask.Delay(200, cancellationToken: _animationCancellationToken.Token);
            }
        }
        
        private async UniTask ExecuteWarRoundAsync(WarRound warRound, IPlayerController localPlayer, IPlayerController aiPlayer)
        {
            Debug.Log($"[WarAnimationController] Executing war round {warRound.RoundNumber}");
            
            // Place concealed cards
            await PlaceConcealedCardsAsync(warRound, localPlayer, aiPlayer);
            
            // Show fighting cards
            await ShowFightingCardsAsync(warRound, localPlayer, aiPlayer);
            
            // Show war progression indicator
            await ShowWarProgressionAsync(warRound);
        }
        
        private async UniTask PlaceConcealedCardsAsync(WarRound warRound, IPlayerController localPlayer, IPlayerController aiPlayer)
        {
            // Place player concealed cards
            if (warRound.ConcealedCards.TryGetValue(1, out var playerConcealed))
            {
                foreach (var card in playerConcealed)
                {
                    var cardView = _cardPool.Spawn();
                    cardView.Setup(card);
                    cardView.SetFaceUp(false, immediate: true);
                    
                    // Animate to concealed position
                    await localPlayer.CardSlot.PlaceConcealedCardAsync(cardView, playerConcealed.IndexOf(card));
                    await UniTask.Delay(100, cancellationToken: _animationCancellationToken.Token);
                }
            }
            
            // Place AI concealed cards
            if (warRound.ConcealedCards.TryGetValue(2, out var aiConcealed))
            {
                foreach (var card in aiConcealed)
                {
                    var cardView = _cardPool.Spawn();
                    cardView.Setup(card);
                    cardView.SetFaceUp(false, immediate: true);
                    
                    // Animate to concealed position
                    await aiPlayer.CardSlot.PlaceConcealedCardAsync(cardView, aiConcealed.IndexOf(card));
                    await UniTask.Delay(100, cancellationToken: _animationCancellationToken.Token);
                }
            }
        }
        
        private async UniTask ShowFightingCardsAsync(WarRound warRound, IPlayerController localPlayer, IPlayerController aiPlayer)
        {
            // Show player fighting card
            if (warRound.FightingCards.TryGetValue(1, out var playerFightingCard))
            {
                var playerCardView = _cardPool.Spawn();
                playerCardView.Setup(playerFightingCard);
                playerCardView.SetFaceUp(false, immediate: true);
                
                await localPlayer.CardSlot.PlaceCardAsync(playerCardView, animate: true);
                await _animationService.AnimateCardFlip(playerCardView, playerFightingCard);
            }
            
            // Show AI fighting card
            if (warRound.FightingCards.TryGetValue(2, out var aiFightingCard))
            {
                var aiCardView = _cardPool.Spawn();
                aiCardView.Setup(aiFightingCard);
                aiCardView.SetFaceUp(false, immediate: true);
                
                await aiPlayer.CardSlot.PlaceCardAsync(aiCardView, animate: true);
                await _animationService.AnimateCardFlip(aiCardView, aiFightingCard);
            }
        }
        
        private async UniTask ShowWarProgressionAsync(WarRound warRound)
        {
            // Show total cards accumulated indicator
            if (warRound.TotalCardsAccumulated > 6)
            {
                // TODO: Show "Total Troops: X" indicator
                Debug.Log($"[WarAnimationController] Total troops in war: {warRound.TotalCardsAccumulated}");
            }
            
            // Pause to let players see the cards
            await UniTask.Delay(1000, cancellationToken: _animationCancellationToken.Token);
        }
        
        private async UniTask ShowWarResultAsync(WarData warData, IPlayerController localPlayer, IPlayerController aiPlayer)
        {
            Debug.Log($"[WarAnimationController] Showing war result: Player {warData.WinningPlayerNumber} wins");
            
            // Determine which player won
            var winningPlayer = warData.WinningPlayerNumber == 1 ? localPlayer : aiPlayer;
            var losingPlayer = warData.WinningPlayerNumber == 1 ? aiPlayer : localPlayer;
            
            // Show winning animation
            await winningPlayer.ShowWarResultAsync(true, warData.FinalCardDistribution[warData.WinningPlayerNumber]);
            
            // Show losing animation
            await losingPlayer.ShowWarResultAsync(false, null);
            
            // Handle shuffle if required
            if (warData.RequiresShuffle)
            {
                await HandleShuffleAnimationAsync(localPlayer, aiPlayer);
            }
            
            // Reset pool size if it was increased
            if (warData.RequiredPoolSize > 16)
            {
                _signalBus.Fire(new PoolResizeEvent(16));
            }
        }
        
        private async UniTask HandleShuffleAnimationAsync(IPlayerController localPlayer, IPlayerController aiPlayer)
        {
            Debug.Log("[WarAnimationController] Handling shuffle animation");
            
            // Show shuffle animation for both players
            await localPlayer.Deck.AnimateShuffleAsync();
            await aiPlayer.Deck.AnimateShuffleAsync();
        }
        
        public void CancelAnimation()
        {
            _animationCancellationToken?.Cancel();
            _isAnimating = false;
        }
        
        public bool IsAnimating => _isAnimating;
        
        public void Dispose()
        {
            _animationCancellationToken?.Cancel();
            _animationCancellationToken?.Dispose();
        }
    }
}
