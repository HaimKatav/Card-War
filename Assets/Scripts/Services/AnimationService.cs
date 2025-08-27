using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using CardWar.Core.Data;
using CardWar.View.Cards;

public class AnimationService : IAnimationService
{
    private readonly float _defaultDuration;
    
    public AnimationService(float defaultDuration = 0.5f)
    {
        _defaultDuration = defaultDuration;
    }

    public async UniTask AnimateCardDeal(CardView cardView, Vector3 targetPosition, float duration = 0.5f)
    {
        if (cardView?.transform == null) return;

        duration = duration <= 0 ? _defaultDuration : duration;
        
        // Store starting position (could be from deck position)
        Vector3 startPosition = cardView.transform.position;
        
        // Add some curve to the animation path
        Vector3 midPoint = Vector3.Lerp(startPosition, targetPosition, 0.5f) + Vector3.up * 2f;
        
        await AnimateAlongCurve(cardView.transform, startPosition, midPoint, targetPosition, duration);
        
        Debug.Log($"AnimationService: Card dealt to {targetPosition}");
    }

    public async UniTask AnimateCardFlip(CardView cardView, CardData cardData, float duration = 0.3f)
    {
        if (cardView?.transform == null) return;

        duration = duration <= 0 ? _defaultDuration * 0.6f : duration;
        
        Transform cardTransform = cardView.transform;
        Vector3 originalScale = cardTransform.localScale;
        
        // First half - scale down to 0 on X-axis (flip away)
        await ScaleOverTime(cardTransform, originalScale, new Vector3(0f, originalScale.y, originalScale.z), duration * 0.5f);
        
        // Update card data at the midpoint
        cardView.Setup(cardData);
        
        // Second half - scale back up (flip towards viewer)
        await ScaleOverTime(cardTransform, new Vector3(0f, originalScale.y, originalScale.z), originalScale, duration * 0.5f);
        
        Debug.Log($"AnimationService: Card flipped to show {cardData}");
    }

    public async UniTask AnimateCardBattle(CardView playerCard, CardView opponentCard, bool playerWins, float duration = 1f)
    {
        if (playerCard?.transform == null || opponentCard?.transform == null) return;

        duration = duration <= 0 ? _defaultDuration * 2f : duration;

        // Store original positions
        Vector3 playerOriginalPos = playerCard.transform.position;
        Vector3 opponentOriginalPos = opponentCard.transform.position;
        
        // Calculate center point for battle
        Vector3 centerPoint = Vector3.Lerp(playerOriginalPos, opponentOriginalPos, 0.5f);
        
        // Phase 1: Move cards towards center (0.3 of duration)
        var moveToCenter = UniTask.WhenAll(
            MoveToPosition(playerCard.transform, centerPoint + Vector3.left * 0.5f, duration * 0.3f),
            MoveToPosition(opponentCard.transform, centerPoint + Vector3.right * 0.5f, duration * 0.3f)
        );
        await moveToCenter;
        
        // Phase 2: Battle effect - quick shake/highlight (0.2 of duration)
        var battleEffect = UniTask.WhenAll(
            AnimateShake(playerCard.transform, duration * 0.2f),
            AnimateShake(opponentCard.transform, duration * 0.2f)
        );
        await battleEffect;
        
        // Phase 3: Winner animation - scale up winner, move loser (0.5 of duration)
        if (playerWins)
        {
            var winnerAnimation = UniTask.WhenAll(
                AnimateWinnerEffect(playerCard.transform, duration * 0.25f),
                MoveToPosition(opponentCard.transform, playerOriginalPos, duration * 0.25f)
            );
            await winnerAnimation;
            await MoveToPosition(playerCard.transform, playerOriginalPos, duration * 0.25f);
        }
        else
        {
            var winnerAnimation = UniTask.WhenAll(
                AnimateWinnerEffect(opponentCard.transform, duration * 0.25f),
                MoveToPosition(playerCard.transform, opponentOriginalPos, duration * 0.25f)
            );
            await winnerAnimation;
            await MoveToPosition(opponentCard.transform, opponentOriginalPos, duration * 0.25f);
        }
        
        Debug.Log($"AnimationService: Battle animation completed - {(playerWins ? "Player" : "Opponent")} wins");
    }

    public async UniTask AnimateCardsToWinner(CardView[] cards, Vector3 winnerPosition, float duration = 0.8f)
    {
        if (cards == null || cards.Length == 0) return;

        duration = duration <= 0 ? _defaultDuration * 1.6f : duration;
        
        // Animate all cards to winner position with slight delays
        var animations = new UniTask[cards.Length];
        
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i]?.transform != null)
            {
                // Add small delay between each card animation
                float delay = i * 0.1f;
                animations[i] = AnimateCardToWinnerWithDelay(cards[i].transform, winnerPosition, duration, delay);
            }
        }
        
        await UniTask.WhenAll(animations);
        
        Debug.Log($"AnimationService: {cards.Length} cards animated to winner position");
    }

    private async UniTask AnimateAlongCurve(Transform target, Vector3 start, Vector3 mid, Vector3 end, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            if (target == null) break;
            
            float t = elapsedTime / duration;
            
            // Quadratic Bezier curve
            Vector3 position = CalculateQuadraticBezierPoint(t, start, mid, end);
            target.position = position;
            
            elapsedTime += Time.deltaTime;
            await UniTask.Yield();
        }
        
        if (target != null)
            target.position = end;
    }

    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        
        return (uu * p0) + (2 * u * t * p1) + (tt * p2);
    }

    private async UniTask MoveToPosition(Transform target, Vector3 targetPosition, float duration)
    {
        if (target == null) return;
        
        Vector3 startPosition = target.position;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            if (target == null) break;
            
            float t = elapsedTime / duration;
            t = EaseInOutCubic(t); // Smooth easing
            
            target.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            elapsedTime += Time.deltaTime;
            await UniTask.Yield();
        }
        
        if (target != null)
            target.position = targetPosition;
    }

    private async UniTask ScaleOverTime(Transform target, Vector3 startScale, Vector3 endScale, float duration)
    {
        if (target == null) return;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            if (target == null) break;
            
            float t = elapsedTime / duration;
            target.localScale = Vector3.Lerp(startScale, endScale, t);
            
            elapsedTime += Time.deltaTime;
            await UniTask.Yield();
        }
        
        if (target != null)
            target.localScale = endScale;
    }

    private async UniTask AnimateShake(Transform target, float duration)
    {
        if (target == null) return;
        
        Vector3 originalPosition = target.position;
        float elapsedTime = 0f;
        float shakeIntensity = 0.1f;
        
        while (elapsedTime < duration)
        {
            if (target == null) break;
            
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                UnityEngine.Random.Range(-shakeIntensity, shakeIntensity),
                0f
            );
            
            target.position = originalPosition + randomOffset;
            
            elapsedTime += Time.deltaTime;
            await UniTask.Yield();
        }
        
        if (target != null)
            target.position = originalPosition;
    }

    private async UniTask AnimateWinnerEffect(Transform target, float duration)
    {
        if (target == null) return;
        
        Vector3 originalScale = target.localScale;
        Vector3 winnerScale = originalScale * 1.2f;
        
        // Scale up
        await ScaleOverTime(target, originalScale, winnerScale, duration * 0.5f);
        // Scale back down
        await ScaleOverTime(target, winnerScale, originalScale, duration * 0.5f);
    }

    private async UniTask AnimateCardToWinnerWithDelay(Transform cardTransform, Vector3 winnerPosition, float duration, float delay)
    {
        if (delay > 0)
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
        
        await MoveToPosition(cardTransform, winnerPosition, duration);
    }

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}