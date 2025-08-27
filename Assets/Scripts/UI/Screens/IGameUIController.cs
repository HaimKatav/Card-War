using UnityEngine;
using Zenject;
using CardWar.Core.Data;

public interface IGameUIController
{
    RectTransform GetRectTransform();
    void Initialize(SignalBus signalBus);
    void Show();
    void Hide();
    void UpdatePlayerCard(CardData card);
    void UpdateOpponentCard(CardData card);
    void ShowRoundResult(GameRoundResultData resultData);
    void ShowGameResult(GameEndResultData resultData);
    void SetInteractable(bool interactable);
    
    // NEW: Methods for war scenario handling
    Transform GetPlayerArea();
    Transform GetOpponentArea();
    void SetDrawButtonInteractable(bool interactable);
}
