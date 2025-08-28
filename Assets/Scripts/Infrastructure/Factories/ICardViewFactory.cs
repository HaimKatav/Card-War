using UnityEngine;
using CardWar.UI.Cards;

namespace CardWar.Infrastructure.Factories
{
    public interface ICardViewFactory
    {
        CardViewController Create();
        void Return(CardViewController card);
        void Prewarm(int count);
        void Clear();
    }
}