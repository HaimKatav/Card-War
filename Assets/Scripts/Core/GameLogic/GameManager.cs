using CardWar.Core.Events;
using UnityEngine;
using Zenject;

namespace CardWar.Core.GameLogic
{
    public class GameManager : MonoBehaviour, IInitializable
    {
        [Inject] private SignalBus _signalBus;

        public void Initialize()
        {
            _signalBus.Fire<GameStartSignal>();
        }
    }
}
