using System;
using CardWar.Animation.Data;
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
        
        void Initialize(AnimationDataBundle animationDataBundle);
        void SetupCardPool(int initialSize, int maxSize, bool prewarm);
        
        UniTask ShowInitialDeckSetup();
        
        UniTask DrawBattleCards(RoundData roundData);
        UniTask FlipBattleCards();
        UniTask HighlightWinner(RoundResult result);
        UniTask CollectBattleCards(RoundResult result);
        
        UniTask PlaceWarCards(RoundData warData);
        UniTask RevealWarCards();
        UniTask RevealAllWarCards();
        UniTask CollectWarCards(RoundResult result);
        UniTask ReturnWarCardsToBothPlayers();
        
        UniTask PlayRound(RoundData roundData);
        UniTask PlayWarSequence(RoundData warRound);
        
        void PauseAnimations();
        void ResumeAnimations();
        void PauseAnimationsWithTransition();
        void ResumeAnimationsWithTransition();
    }
}