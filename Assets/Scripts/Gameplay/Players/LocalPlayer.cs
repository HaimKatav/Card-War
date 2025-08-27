using CardWar.Core.Data;
using Cysharp.Threading.Tasks;

namespace CardWar.Gameplay.Players
{
    public class LocalPlayer : PlayerController
    {
        public override async UniTask<CardData> PlayCard()
        {
            await UniTask.Yield();
            return null;
        }
    }
}
