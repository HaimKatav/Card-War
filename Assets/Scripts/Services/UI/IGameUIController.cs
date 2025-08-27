using UnityEngine;
using Zenject;
using CardWar.Core.Data;

namespace CardWar.Services.UI
{
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
        Transform GetPlayerArea();
        Transform GetOpponentArea();
        void SetDrawButtonInteractable(bool interactable);
    }
}
