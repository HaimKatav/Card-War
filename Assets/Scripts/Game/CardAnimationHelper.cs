using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using CardWar.Animation.Data;


namespace CardWar.Game.UI
{
    public class CardAnimationHelper
    {
        private readonly AnimationDataBundle _animationData;
        private readonly CardPositionManager _positionManager;
        private bool _isPaused;
        
        public bool IsPaused => _isPaused;
        
        public CardAnimationHelper(AnimationDataBundle animationData, CardPositionManager positionManager = null)
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
        
        #region Card Movement
        
        public async UniTask MoveCardToPosition(CardView card, Vector3 targetPosition, float duration, Ease easing)
        {
            if (card == null) return;
            
            await card.transform.DOMove(targetPosition, duration)
                .SetEase(easing)
                .AsyncWaitForCompletion()
                .AsUniTask();
        }
        
        public async UniTask MoveCardsToPosition(List<CardView> cards, Vector3 targetPosition, 
            float duration, float staggerDelay, Ease easing)
        {
            if (cards == null || cards.Count == 0) return;
            
            var sequence = DOTween.Sequence();
            var delay = 0f;
            
            foreach (var card in cards)
            {
                if (card != null)
                {
                    sequence.Insert(delay, card.transform.DOMove(targetPosition, duration).SetEase(easing));
                    delay += staggerDelay;
                }
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        public async UniTask MoveCardWithScale(CardView card, Vector3 targetPosition, 
            float duration, float scaleMultiplier, Ease moveEase, Ease scaleEase)
        {
            if (card == null) return;
            
            var sequence = DOTween.Sequence();
            sequence.Append(card.transform.DOMove(targetPosition, duration).SetEase(moveEase));
            sequence.Join(card.transform.DOScale(scaleMultiplier, duration * 0.5f).SetEase(scaleEase));
            sequence.Append(card.transform.DOScale(1f, duration * 0.5f).SetEase(Ease.InBack));
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        #endregion
        
        #region Battle Animations
        
        public async UniTask AnimateBattleCardDraw(CardView playerCard, CardView opponentCard,
            Vector3 playerTarget, Vector3 opponentTarget)
        {
            if (playerCard == null || opponentCard == null) return;
            
            var drawConfig = _animationData.Battle.DrawAnimation;
            
            var tasks = new List<UniTask>
            {
                MoveCardToPosition(playerCard, playerTarget, drawConfig.Duration, drawConfig.EasingCurve),
                MoveCardToPosition(opponentCard, opponentTarget, drawConfig.Duration, drawConfig.EasingCurve)
            };
            
            await UniTask.WhenAll(tasks);
        }
        
        public async UniTask AnimateBattleCardFlip(CardView playerCard, CardView opponentCard)
        {
            if (playerCard == null || opponentCard == null) return;
            
            var flipConfig = _animationData.Battle.RevealAnimation;
            
            var tasks = new List<UniTask>
            {
                playerCard.FlipCard(true, flipConfig.Duration),
                opponentCard.FlipCard(true, flipConfig.Duration)
            };
            
            await UniTask.WhenAll(tasks);
        }
        
        #endregion
        
        #region Card Flipping
        
        public async UniTask FlipCard(CardView card, bool faceUp, float duration)
        {
            if (card == null) return;
            
            await card.FlipCard(faceUp, duration);
        }
        
        public async UniTask FlipCards(List<CardView> cards, bool faceUp, float duration, float staggerDelay)
        {
            if (cards == null || cards.Count == 0) return;
            
            var tasks = new List<UniTask>();
            var currentDelay = 0f;
            
            foreach (var card in cards)
            {
                if (card != null)
                {
                    tasks.Add(FlipCardDelayed(card, faceUp, duration, currentDelay));
                    currentDelay += staggerDelay;
                }
            }
            
            await UniTask.WhenAll(tasks);
        }
        
        private async UniTask FlipCardDelayed(CardView card, bool faceUp, float duration, float delay)
        {
            if (delay > 0)
            {
                await UniTask.Delay((int)(delay * 1000));
            }
            
            await card.FlipCard(faceUp, duration);
        }
        
        public async UniTask ConcealCards(List<CardView> cards)
        {
            if (cards == null || cards.Count == 0) return;
            
            var tasks = new List<UniTask>();
            
            foreach (var card in cards)
            {
                if (card != null && card.IsFaceUp)
                {
                    tasks.Add(card.FlipCard(false, 0.3f));
                }
            }
            
            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }
        }
        
        #endregion
        
        #region Card Collection
        
        public async UniTask CollectCardsToPosition(List<CardView> cards, Vector3 targetPosition, bool reverseOrder = false)
        {
            if (cards == null || cards.Count == 0) return;
            
            var config = _animationData.Collection;
            var sequence = DOTween.Sequence();
            var delay = 0f;
            
            var cardList = new List<CardView>(cards);
            if (reverseOrder)
            {
                cardList.Reverse();
            }
            
            foreach (var card in cardList)
            {
                if (card != null)
                {
                    sequence.Insert(delay, card.transform.DOMove(targetPosition, config.Duration)
                        .SetEase(config.EasingCurve));
                    delay += config.StaggerDelay;
                }
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        public async UniTask ReturnCardsToDecks(List<CardView> cards, CardPositionManager positionManager)
        {
            if (cards == null || cards.Count == 0 || positionManager == null) return;
            
            var config = _animationData.Collection;
            var sequence = DOTween.Sequence();
            var playerDelay = 0f;
            var opponentDelay = 0f;
            
            foreach (var card in cards)
            {
                if (card == null) continue;
                
                var isPlayerCard = positionManager.IsPlayerCard(card);
                var targetPosition = positionManager.GetCardReturnPosition(card);
                var delay = isPlayerCard ? playerDelay : opponentDelay;
                
                sequence.Insert(delay, card.transform.DOMove(targetPosition, config.Duration)
                    .SetEase(config.EasingCurve));
                
                if (isPlayerCard)
                {
                    playerDelay += 0.1f;
                }
                else
                {
                    opponentDelay += 0.1f;
                }
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        #endregion
        
        #region Visual Effects
        
        public async UniTask HighlightCard(CardView card, float scaleMultiplier, Color tintColor)
        {
            if (card == null) return;
            
            var config = _animationData.WinnerHighlight;
            var sequence = DOTween.Sequence();
            
            sequence.Append(card.transform.DOScale(scaleMultiplier, config.ScaleDuration * 0.5f)
                .SetEase(Ease.OutBack));
            sequence.Append(card.transform.DOScale(1f, config.ScaleDuration * 0.5f)
                .SetEase(Ease.InBack));
            
            if (tintColor != Color.white)
            {
                sequence.Join(card.SetTint(tintColor, config.ScaleDuration));
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        public async UniTask HighlightWinner(CardView winnerCard)
        {
            if (winnerCard == null) return;
            
            var config = _animationData.WinnerHighlight;
            await HighlightCard(winnerCard, config.ScaleMultiplier, config.TintColor);
        }
        
        public async UniTask ShowShuffleAnimation(Transform deckPosition)
        {
            if (deckPosition == null) return;
            
            var shuffleDuration = 0.8f;
            var shuffleHeight = 0.5f;
            var sequence = DOTween.Sequence();
            
            var originalPosition = deckPosition.position;
            
            sequence.Append(deckPosition.DOMove(originalPosition + Vector3.up * shuffleHeight, 
                shuffleDuration * 0.25f).SetEase(Ease.OutQuad));
            sequence.Append(deckPosition.DOMove(originalPosition, 
                shuffleDuration * 0.25f).SetEase(Ease.InQuad));
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        #endregion
        
        #region War Animations
        
        public async UniTask AnimateWarCardPlacement(List<CardView> playerCards, List<CardView> opponentCards,
            List<Vector3> playerPositions, List<Vector3> opponentPositions)
        {
            if (playerCards == null || opponentCards == null) return;
            
            var config = _animationData.War.PlaceCardsAnimation;
            var sequence = DOTween.Sequence();
            
            for (var i = 0; i < playerCards.Count && i < playerPositions.Count; i++)
            {
                if (playerCards[i] != null)
                {
                    sequence.Insert(i * 0.1f, playerCards[i].transform
                        .DOMove(playerPositions[i], config.Duration)
                        .SetEase(config.EasingCurve));
                }
            }
            
            for (var i = 0; i < opponentCards.Count && i < opponentPositions.Count; i++)
            {
                if (opponentCards[i] != null)
                {
                    sequence.Insert(i * 0.1f, opponentCards[i].transform
                        .DOMove(opponentPositions[i], config.Duration)
                        .SetEase(config.EasingCurve));
                }
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        public async UniTask RevealWarCardsSequentially(List<CardView> cards)
        {
            if (cards == null || cards.Count == 0) return;
            
            var config = _animationData.War.RevealAnimation;
            var sequence = DOTween.Sequence();
            var delay = 0f;
            
            for (var i = cards.Count - 1; i >= 0; i--)
            {
                var card = cards[i];
                if (card != null && !card.IsFaceUp)
                {
                    var capturedCard = card;
                    sequence.Insert(delay, DOTween.To(
                        () => 0f,
                        _ => { },
                        1f,
                        0.01f
                    ).OnComplete(() => capturedCard.FlipCard(true, config.Duration).Forget()));
                    
                    delay += 0.1f;
                }
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        #endregion
        
        #region Utility
        
        public void ResetCardTransform(CardView card)
        {
            if (card == null) return;
            
            card.transform.rotation = Quaternion.identity;
            card.transform.localScale = Vector3.one;
        }
        
        public void ResetMultipleCardTransforms(List<CardView> cards)
        {
            if (cards == null) return;
            
            foreach (var card in cards)
            {
                ResetCardTransform(card);
            }
        }
        
        #endregion
    }
}