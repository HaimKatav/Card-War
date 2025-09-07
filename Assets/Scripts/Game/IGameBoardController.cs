using System;
using CardWar.Common;
using CardWar.Animation.Data;
using Cysharp.Threading.Tasks;

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
        UniTask ConcealAllCards();
        UniTask ReturnWarCardsToBothPlayers();
        
        void PauseAnimationsWithTransition();
        void ResumeAnimationsWithTransition();
    }
}