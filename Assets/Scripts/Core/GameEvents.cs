using CardWar.Core.Data;

namespace CardWar.Core.Events
{
    public class GameStartSignal { }

    public class CardPlayedSignal
    {
        public CardData Card { get; }
        public CardPlayedSignal(CardData card) => Card = card;
    }

    public class GameEndSignal { }
}
