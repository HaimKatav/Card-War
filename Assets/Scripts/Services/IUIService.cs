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
        
        event Action<string> OnAnimationStarted;
        event Action<string> OnAnimationCompleted;
        event Action<UIState> OnUIStateChanged;
        
        void ShowLoadingScreen(bool show);
        void ShowMainMenu(bool show);
        void ShowGameUI(bool show);
        void ShowGameOverScreen(bool show, bool playerWon);
        void SetUIState(UIState state);
        void RegisterResetCallback(Action callback);
    }
}