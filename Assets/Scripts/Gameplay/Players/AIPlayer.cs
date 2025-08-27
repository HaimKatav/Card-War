using CardWar.Core.Data;
using Cysharp.Threading.Tasks;

namespace CardWar.Gameplay.Players
{
    public class AIPlayer : PlayerController
    {
        public override async UniTask<CardData> PlayCard()
        {
            await UniTask.Yield();
            return null;
        }
    }
}
