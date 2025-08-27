using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardWar.Services.UI
{
    public interface IUIService
    {
        void ShowMainMenu();
        void ShowGameplay();
        void ShowLoading(bool isLoading);
    UniTask CreateGameUIControllerAsync();
    UniTask DestroyGameUIControllerAsync();
    UniTask ReturnToMainMenuAsync();
    Transform GetPlayerArea();
    Transform GetOpponentArea();
}
}
