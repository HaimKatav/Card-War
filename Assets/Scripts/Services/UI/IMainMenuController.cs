using UnityEngine;
using Zenject;

namespace CardWar.Services.UI
{
    public interface IMainMenuController
    {
        RectTransform GetRectTransform();
        void Initialize(SignalBus signalBus);
        void Show();
        void Hide();
    }
}
