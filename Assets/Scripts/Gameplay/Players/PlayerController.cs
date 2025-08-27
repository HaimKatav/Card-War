using CardWar.Core.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace CardWar.Gameplay.Players
{
    public abstract class PlayerController : MonoBehaviour
    {
        [Inject] protected SignalBus _signalBus;

        public abstract UniTask<CardData> PlayCard();
        public virtual void ShowCard(CardData card) { }
        public virtual void UpdateCardCount(int count) { }
    }
}
