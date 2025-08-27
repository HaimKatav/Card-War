using UnityEngine;
using Zenject;

public interface IMainMenuController
{
    RectTransform GetRectTransform();
    void Initialize(SignalBus signalBus);
    void Show();
    void Hide();
}
