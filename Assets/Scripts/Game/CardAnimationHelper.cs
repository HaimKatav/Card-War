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
        
        public CardAnimationHelper(AnimationDataBundle animationData)
        {
            _animationData = animationData;
        }
        
        #region Card Movement
        
        public async UniTask MoveCardToPosition(CardView card, Vector3 targetPosition, float duration, Ease easing)
        {
            if (card == null) return;
            
            await card.transform.DOMove(targetPosition, duration)
                .SetEase(easing)
                .AsyncWaitForCompletion().AsUniTask();
        }
        
        public async UniTask MoveCardsToPosition(List<CardView> cards, Vector3 targetPosition, float duration, float staggerDelay, Ease easing)
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
        
        public async UniTask MoveCardWithScale(CardView card, Vector3 targetPosition, float duration, float scaleMultiplier, Ease moveEase, Ease scaleEase)
        {
            if (card == null) return;
            
            var sequence = DOTween.Sequence();
            sequence.Append(card.transform.DOMove(targetPosition, duration).SetEase(moveEase));
            sequence.Join(card.transform.DOScale(scaleMultiplier, duration * 0.5f).SetEase(scaleEase));
            sequence.Append(card.transform.DOScale(1f, duration * 0.5f).SetEase(Ease.InBack));
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
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
                    var delayedFlip = FlipCardDelayed(card, faceUp, duration, currentDelay);
                    tasks.Add(delayedFlip);
                    currentDelay += staggerDelay;
                }
            }
            
            await UniTask.WhenAll(tasks);
        }
        
        private async UniTask FlipCardDelayed(CardView card, bool faceUp, float duration, float delay)
        {
            if (delay > 0)
                await UniTask.Delay((int)(delay * 1000));
            
            await card.FlipCard(faceUp, duration);
        }
        
        #endregion
        
        #region Card Collection
        
        public async UniTask CollectCardsToPosition(List<CardView> cards, Vector3 targetPosition, bool reverseOrder = false)
        {
            if (cards == null || cards.Count == 0) return;
            
            var collectionDuration = _animationData.Collection.Duration;
            var staggerDelay = _animationData.Collection.StaggerDelay;
            var collectionEase = _animationData.Collection.EasingCurve;
            
            var sequence = DOTween.Sequence();
            var delay = 0f;
            
            var cardList = new List<CardView>(cards);
            if (reverseOrder)
                cardList.Reverse();
            
            foreach (var card in cardList)
            {
                if (card != null)
                {
                    sequence.Insert(delay, card.transform.DOMove(targetPosition, collectionDuration).SetEase(collectionEase));
                    delay += staggerDelay;
                }
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        //TODO: This doesnt do what it should - need to fix and make sure each card returns to the right owner.
        public async UniTask ReturnCardsToDecks(List<CardView> cards, List<Transform> playerPositions, List<Transform> opponentPositions, 
            Vector3 playerDeckPosition, Vector3 opponentDeckPosition)
        {
            if (cards == null || cards.Count == 0) return;
            
            var returnDuration = _animationData.Collection.Duration;
            var returnEase = _animationData.Collection.EasingCurve;
            
            var sequence = DOTween.Sequence();
            var playerCardIndex = 0;
            var opponentCardIndex = 0;
            
            for (var i = cards.Count - 1; i >= 0; i--)
            {
                var card = cards[i];
                if (card == null) continue;
                
                var isPlayerCard = IsPlayerCard(card, playerPositions);
                var targetPosition = isPlayerCard ? playerDeckPosition : opponentDeckPosition;
                var delay = isPlayerCard ? playerCardIndex * 0.1f : opponentCardIndex * 0.1f;
                
                sequence.Insert(delay, card.transform.DOMove(targetPosition, returnDuration).SetEase(returnEase));
                
                if (isPlayerCard) playerCardIndex++;
                else opponentCardIndex++;
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        private bool IsPlayerCard(CardView card, List<Transform> playerPositions)
        {
            foreach (var pos in playerPositions)
            {
                if (pos != null && Vector3.Distance(card.transform.position, pos.position) < 0.1f)
                {
                    return true;
                }
            }
            return false;
        }
        
        #endregion
        
        #region Visual Effects
        
        public async UniTask HighlightCard(CardView card, float scaleMultiplier, Color tintColor)
        {
            if (card == null) return;
            
            var scaleDuration = _animationData.WinnerHighlight.ScaleDuration;
            
            var sequence = DOTween.Sequence();
            sequence.Append(card.transform.DOScale(scaleMultiplier, scaleDuration * 0.5f).SetEase(Ease.OutBack));
            sequence.Append(card.transform.DOScale(1f, scaleDuration * 0.5f).SetEase(Ease.InBack));
            
            if (tintColor != Color.white)
            {
                sequence.Join(card.SetTint(tintColor, scaleDuration));
            }
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        
        public async UniTask ShowShuffleAnimation(Transform deckPosition)
        {
            if (deckPosition == null) return;
            
            var shuffleDuration = 0.8f;
            var shuffleHeight = 0.5f;
            
            var sequence = DOTween.Sequence();
            
            var originalPosition = deckPosition.position;
            sequence.Append(deckPosition.DOMove(originalPosition + Vector3.up * shuffleHeight, shuffleDuration * 0.25f).SetEase(Ease.OutQuad));
            sequence.Append(deckPosition.DOMove(originalPosition, shuffleDuration * 0.25f).SetEase(Ease.InQuad));
            
            await sequence.AsyncWaitForCompletion().AsUniTask();
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
    }
}