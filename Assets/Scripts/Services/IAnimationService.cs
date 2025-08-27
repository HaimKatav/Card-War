using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IAnimationService
{
    UniTask AnimateCardDeal(CardView cardView, Vector3 targetPosition, float duration = 0.5f);
    UniTask AnimateCardFlip(CardView cardView, CardData cardData, float duration = 0.3f);
    UniTask AnimateCardBattle(CardView playerCard, CardView opponentCard, bool playerWins, float duration = 1f);
    UniTask AnimateCardsToWinner(CardView[] cards, Vector3 winnerPosition, float duration = 0.8f);
}