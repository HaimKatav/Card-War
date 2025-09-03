using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using CardWar.Game.Logic;
using CardWar.Common;
using DG.Tweening;

namespace CardWar.Game.UI
{
    public interface IGameBoardController
    {
        event Action OnDrawButtonPressed;
        event Action OnRoundAnimationComplete;
        
        void Initialize();
        void SetupCardPool(int initialSize, int maxSize, bool prewarm);
        
        UniTask ShowInitialDeckSetup(float fadeInDuration);
        
        UniTask DrawBattleCards(RoundData roundData, float moveDuration, Ease moveEase);
        UniTask FlipBattleCards(float flipDuration, float delayBetweenFlips, Ease flipEase);
        UniTask HighlightWinner(RoundResult result, float scaleMultiplier, float scaleDuration, Color tintColor);
        UniTask CollectBattleCards(RoundResult result, float collectionDuration, float staggerDelay, Ease collectionEase);
        
        UniTask PlaceWarCards(RoundData warData, int faceDownCardsPerPlayer, float placeDuration, float cardSpacing);
        UniTask RevealWarCards(float revealDuration, Ease revealEase);
        UniTask RevealAllWarCards();
        UniTask CollectWarCards(RoundResult result, float collectionDuration, float staggerDelay, Ease collectionEase);
        UniTask ReturnWarCardsToBothPlayers(float returnDuration, Ease returnEase);
        
        UniTask PlayRound(RoundData roundData);
        UniTask PlayWarSequence(RoundData warRound);
        
        void PauseAnimations();
        void ResumeAnimations();
        void PauseAnimationsWithTransition(float fadeDuration);
        void ResumeAnimationsWithTransition(float fadeDuration);
    }
}