using Cysharp.Threading.Tasks;

public interface IUIService
{
    void ShowMainMenu();
    void ShowGameplay();
    void ShowLoading(bool isLoading);
    UniTask CreateGameUIControllerAsync();
    UniTask DestroyGameUIControllerAsync();
    UniTask ReturnToMainMenuAsync();
}