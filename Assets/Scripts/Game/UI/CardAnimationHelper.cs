using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using CardWar.Animation.Data;
using CardWar.Game.UI;
using CardWar.Common;

namespace CardWar.Game.Helpers
{
    public class CardAnimationHelper
    {
        private readonly AnimationDataBundle _animationData;
        private readonly CardPositionManager _positionManager;
        private bool _isPaused;
        
        public bool IsPaused => _isPaused;
        
        public CardAnimationHelper(AnimationDataBundle animationData, CardPositionManager positionManager)
        {
            _animationData = animationData;
            _positionManager = positionManager;
        }
        
        #region Animation State Management
        
        public void PauseAllAnimations()
        {
            _isPaused = true;
            DOTween.PauseAll();
            Debug.Log($"[CardAnimationHelper] All animations paused");
        }
        
        public void ResumeAllAnimations()
        {
            _isPaused = false;
            DOTween.PlayAll();
            Debug.Log($"[CardAnimationHelper] All animations resumed");
        }
        
        public void KillAllAnimations()
        {
            DOTween.KillAll();
            _isPaused = false;
            Debug.Log($"[CardAnimationHelper] All animations killed");
        }
        
        #endregion
        
        #region Battle Animations
        
        public async UniTask AnimateBattleCardDraw(CardView playerCard, CardView opponentCard)
        {
            if (playerCard == null || opponentCard == null || _positionManager == null) return;
            
            var battleConfig = _animationData.Battle;
            
            if (battleConfig.PreDrawDelay > 0)
            {
                await UniTask.Delay((int)(battleConfig.PreDrawDelay * 1000));
            }
            
            var tasks = new List<UniTask>
            {
                MoveCard(playerCard, _positionManager.PlayerBattlePosition, battleConfig.DrawAnimation),
                MoveCard(opponentCard, _positionManager.OpponentBattlePosition, battleConfig.DrawAnimation)
            };
            
            await UniTask.WhenAll(tasks);
            
            if (battleConfig.PostDrawDelay > 0)
            {
                await UniTask.Delay((int)(battleConfig.PostDrawDelay * 1000));
            }
        }
        
        public async UniTask AnimateBattleCardFlip(CardView playerCard, CardView opponentCard)
        {
            if (playerCard == null || opponentCard == null) return;
            
            var battleConfig = _animationData.Battle;
            
            if (battleConfig.PreFlipDelay > 0)
            {
                await UniTask.Delay((int)(battleConfig.PreFlipDelay * 1000));
            }
            
            var tasks = new List<UniTask>
            {
                FlipCard(playerCard, true, battleConfig.RevealAnimation),
                FlipCard(opponentCard, true, battleConfig.RevealAnimation)
            };
            
            await UniTask.WhenAll(tasks);
            
            if (battleConfig.PostFlipDelay > 0)
            {
                await UniTask.Delay((int)(battleConfig.PostFlipDelay * 1000));
            }
        }
        
        public async UniTask HighlightWinner(CardView winnerCard)
        {
            if (winnerCard == null) return;
            
            var config = _animationData.WinnerHighlight;
            var battleConfig = _animationData.Battle;
            
            if (!config.EnableHighlight) return;
            
            if (battleConfig.PreHighlightDelay > 0)
            {
                await UniTask.Delay((int)(battleConfig.PreHighlightDelay * 1000));
            }
            
            var sequence = DOTween.Sequence();
            
            sequence.Append(winnerCard.transform.DOScale(config.ScaleMultiplier, config.ScaleDuration * 0.5f)
                .SetEase(config.ScaleEase));
            sequence.Append(winnerCard.transform.DOScale(1f, config.ScaleDuration * 0.5f)
                .SetEase(Ease.InBack));
            
            if (config.UseTint && config.TintColor != Color.white)
            {
                sequence.Join(winnerCard.SetTint(config.TintColor, config.TintDuration));
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
            
            if (battleConfig.PostHighlightDelay > 0)
            {
                await UniTask.Delay((int)(battleConfig.PostHighlightDelay * 1000));
            }
        }
        
        public async UniTask CollectBattleCards(List<CardView> cards, RoundResult result)
        {
            if (cards == null || cards.Count == 0 || _positionManager == null) return;
            
            var battleConfig = _animationData.Battle;
            
            if (battleConfig.PreCollectionDelay > 0)
            {
                await UniTask.Delay((int)(battleConfig.PreCollectionDelay * 1000));
            }
            
            var targetPosition = _positionManager.GetCollectionTargetPosition(result);
            await CollectCards(cards, targetPosition, _animationData.Collection);
            
            if (battleConfig.PostCollectionDelay > 0)
            {
                await UniTask.Delay((int)(battleConfig.PostCollectionDelay * 1000));
            }
        }
        
        #endregion
        
        #region War Animations
        
        public async UniTask AnimateWarCardPlacement(List<CardView> playerCards, List<CardView> opponentCards, 
            int warDepth, float cardSpacing)
        {
            if (playerCards == null || opponentCards == null || _positionManager == null) return;
            
            var warConfig = _animationData.War;
            
            if (warConfig.PrePlacementDelay > 0)
            {
                await UniTask.Delay((int)(warConfig.PrePlacementDelay * 1000));
            }
            
            var sequence = DOTween.Sequence();
            
            for (var i = 0; i < playerCards.Count; i++)
            {
                if (playerCards[i] != null)
                {
                    var stackOffset = _positionManager.CalculateWarStackOffset(warDepth, i, cardSpacing);
                    var targetPos = _positionManager.GetWarPosition(true, i, stackOffset);
                    var delay = i * warConfig.CardPlacementStagger;
                    
                    playerCards[i].transform.SetParent(_positionManager.GetWarTransform(true, i));
                    
                    sequence.Insert(delay, playerCards[i].transform
                        .DOMove(targetPos, warConfig.PlaceCardsAnimation.Duration)
                        .SetEase(warConfig.PlaceCardsAnimation.EasingCurve));

                    sequence.Join(playerCards[i].transform
                        .DOLocalRotate(Vector3.zero, warConfig.PlaceCardsAnimation.Duration));
                }
            }
            
            for (var i = 0; i < opponentCards.Count; i++)
            {
                if (opponentCards[i] != null)
                {
                    var stackOffset = _positionManager.CalculateWarStackOffset(warDepth, i, cardSpacing);
                    var targetPos = _positionManager.GetWarPosition(false, i, stackOffset);
                    var delay = i * warConfig.CardPlacementStagger;
                    
                    opponentCards[i].transform.SetParent(_positionManager.GetWarTransform(false, i));
                    
                    sequence.Insert(delay, opponentCards[i].transform
                        .DOMove(targetPos, warConfig.PlaceCardsAnimation.Duration)
                        .SetEase(warConfig.PlaceCardsAnimation.EasingCurve));
                    
                    sequence.Join(opponentCards[i].transform
                        .DOLocalRotate(Vector3.zero, warConfig.PlaceCardsAnimation.Duration));
                }
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
            
            if (warConfig.PostPlacementDelay > 0)
            {
                await UniTask.Delay((int)(warConfig.PostPlacementDelay * 1000));
            }
        }
        
        public async UniTask RevealWarCards(List<CardView> cardsToReveal)
        {
            if (cardsToReveal == null || cardsToReveal.Count == 0) return;
            
            var warConfig = _animationData.War;
            
            if (warConfig.PreRevealDelay > 0)
            {
                await UniTask.Delay((int)(warConfig.PreRevealDelay * 1000));
            }
            
            var tasks = new List<UniTask>();
            foreach (var card in cardsToReveal)
            {
                if (card != null)
                {
                    tasks.Add(FlipCard(card, true, warConfig.RevealAnimation));
                }
            }
            
            await UniTask.WhenAll(tasks);
            
            if (warConfig.PostRevealDelay > 0)
            {
                await UniTask.Delay((int)(warConfig.PostRevealDelay * 1000));
            }
        }
        
        public async UniTask RevealWarCardsSequentially(List<CardView> cards)
        {
            if (cards == null || cards.Count == 0) return;
            
            var warConfig = _animationData.War;
            
            if (warConfig.PreSequentialRevealDelay > 0)
            {
                await UniTask.Delay((int)(warConfig.PreSequentialRevealDelay * 1000));
            }
            
            var sequence = DOTween.Sequence();
            var delay = 0f;
            
            for (var i = cards.Count - 1; i >= 0; i--)
            {
                var card = cards[i];
                if (card != null && !card.IsFaceUp)
                {
                    var capturedCard = card;
                    var flipConfig = warConfig.RevealAnimation;
                    
                    sequence.Insert(delay, DOTween.To(
                        () => 0f,
                        _ => { },
                        1f,
                        0.01f
                    ).OnComplete(() => capturedCard.FlipCard(true, flipConfig.Duration).Forget()));
                    
                    delay += warConfig.SequentialRevealStagger;
                }
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
            
            if (warConfig.PostSequentialRevealDelay > 0)
            {
                await UniTask.Delay((int)(warConfig.PostSequentialRevealDelay * 1000));
            }
        }
        
        public async UniTask CollectWarCards(List<CardView> cards, RoundResult result)
        {
            if (cards == null || cards.Count == 0 || _positionManager == null) return;
            
            var warConfig = _animationData.War;
            
            if (warConfig.PreWarCollectionDelay > 0)
            {
                await UniTask.Delay((int)(warConfig.PreWarCollectionDelay * 1000));
            }
            
            var targetPosition = _positionManager.GetCollectionTargetPosition(result);
            await CollectCards(cards, targetPosition, _animationData.Collection, true);
            
            if (warConfig.PostWarCollectionDelay > 0)
            {
                await UniTask.Delay((int)(warConfig.PostWarCollectionDelay * 1000));
            }
        }
        
        #endregion
        
        #region Utility Animations
        
        public async UniTask ShowShuffleAnimation(Transform deckTransform)
        {
            if (deckTransform == null) return;
            
            var utilityConfig = _animationData.UtilityAnimation;
            var sequence = DOTween.Sequence();
            
            var originalPosition = deckTransform.position;
            
            sequence.Append(deckTransform.DOMove(
                originalPosition + Vector3.up * utilityConfig.ShuffleHeight, 
                utilityConfig.ShuffleDuration * 0.25f)
                .SetEase(Ease.OutQuad));
            
            sequence.Append(deckTransform.DOMove(
                originalPosition, 
                utilityConfig.ShuffleDuration * 0.25f)
                .SetEase(Ease.InQuad));
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        public async UniTask ConcealCards(List<CardView> cards)
        {
            if (cards == null || cards.Count == 0) return;
            
            var utilityConfig = _animationData.UtilityAnimation;
            
            if (utilityConfig.PreConcealDelay > 0)
            {
                await UniTask.Delay((int)(utilityConfig.PreConcealDelay * 1000));
            }
            
            var tasks = new List<UniTask>();
            
            foreach (var card in cards)
            {
                if (card != null && card.IsFaceUp)
                {
                    tasks.Add(card.FlipCard(false, utilityConfig.ConcealDuration));
                }
            }
            
            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }
            
            if (utilityConfig.PostConcealDelay > 0)
            {
                await UniTask.Delay((int)(utilityConfig.PostConcealDelay * 1000));
            }
        }
        
        public async UniTask ReturnWarCardsToBothDecks(List<CardView> cards)
        {
            if (cards == null || cards.Count == 0 || _positionManager == null) return;
            
            var utilityConfig = _animationData.UtilityAnimation;
            var sequence = DOTween.Sequence();
            var playerDelay = 0f;
            var opponentDelay = 0f;
            
            foreach (var card in cards)
            {
                if (card == null) continue;
                
                var isPlayerCard = _positionManager.IsPlayerCard(card);
                var targetPosition = _positionManager.GetCardReturnPosition(card);
                var delay = isPlayerCard ? playerDelay : opponentDelay;
                
                sequence.Insert(delay, card.transform
                    .DOMove(targetPosition, utilityConfig.ReturnDuration)
                    .SetEase(utilityConfig.ReturnEase));
                
                if (isPlayerCard)
                {
                    playerDelay += utilityConfig.ReturnStagger;
                }
                else
                {
                    opponentDelay += utilityConfig.ReturnStagger;
                }
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        #endregion
        
        #region Core Animation Methods
        
        private async UniTask MoveCard(CardView card, Vector3 targetPosition, CardMoveAnimationConfig config)
        {
            if (card == null || config == null) return;
            
            var sequence = DOTween.Sequence();
            
            sequence.Append(card.transform
                .DOMove(targetPosition, config.Duration)
                .SetEase(config.EasingCurve));
                
            if (config.UseScaling)
            {
                sequence.Join(card.transform.DOScale(config.ScaleMultiplier, config.Duration * 0.5f));
                sequence.Append(card.transform.DOScale(1f, config.Duration * 0.5f));
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        private async UniTask FlipCard(CardView card, bool faceUp, CardFlipAnimationConfig config)
        {
            if (card == null || config == null) return;
            
            if (config.DelayBetweenFlips > 0)
            {
                await UniTask.Delay((int)(config.DelayBetweenFlips * 1000));
            }
            
            await card.FlipCard(faceUp, config.Duration);
        }
        
        private async UniTask CollectCards(List<CardView> cards, Vector3 targetPosition, 
            CollectionAnimationConfig config, bool reverseOrder = false)
        {
            if (cards == null || cards.Count == 0 || config == null) return;
            
            var sequence = DOTween.Sequence();
            var cardList = new List<CardView>(cards);
            
            if (reverseOrder)
            {
                cardList.Reverse();
            }
            
            if (config.UseStagger)
            {
                var delay = 0f;
                foreach (var card in cardList)
                {
                    if (card != null)
                    {
                        var tween = card.transform
                            .DOMove(targetPosition, config.Duration)
                            .SetEase(config.EasingCurve);
                        
                        if (config.ScaleOnCollection)
                        {
                            sequence.Insert(delay, card.transform.DOScale(config.CollectionScale, config.Duration * 0.5f));
                            sequence.Insert(delay + config.Duration * 0.5f, card.transform.DOScale(1f, config.Duration * 0.5f));
                        }
                        
                        sequence.Insert(delay, tween);
                        delay += config.StaggerDelay;
                    }
                }
            }
            else
            {
                foreach (var card in cardList)
                {
                    if (card != null)
                    {
                        var tween = card.transform
                            .DOMove(targetPosition, config.Duration)
                            .SetEase(config.EasingCurve);
                        
                        sequence.Insert(0, tween);
                    }
                }
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        #endregion
    }
}