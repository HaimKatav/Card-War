using System;

namespace CardWar.Services
{
    public enum UIState
    {
        FirstEntry,
        Idle,
        Animating,
        Loading
    }

    public interface IUIService
    {
        UIState CurrentUIState { get; }
        
        event Action<UIState> OnUIStateChanged;
        
        void ToggleLoadingScreen(bool show);
        void ShowMainMenu(bool show);
        void ShowGameUI(bool show);
        void ToggleGameOverScreen(bool show, bool playerWon);
        void RegisterResetCallback(Action callback);
    }
}