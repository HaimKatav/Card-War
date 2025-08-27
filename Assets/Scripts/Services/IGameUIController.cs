using UnityEngine;
using Zenject;

public interface IGameUIController
{
    RectTransform GetRectTransform();
    void Initialize(SignalBus signalBus);
    void Show();
    void Hide();
    void UpdatePlayerCard(CardData card);
    void UpdateOpponentCard(CardData card);
    void ShowRoundResult(GameRoundResult result);
    void ShowGameResult(GameEndResult result);
    void SetInteractable(bool interactable);
}
