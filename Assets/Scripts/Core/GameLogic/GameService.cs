using CardWar.Core.Events;
using CardWar.Gameplay.Players;
using CardWar.Services.Network;
using CardWar.UI.Management;
using UnityEngine;
using Zenject;

namespace CardWar.Core.GameLogic
{
    public class GameService : IGameService, IInitializable
    {
        [Inject] private FakeWarServer _server;
        [Inject] private UIManager _uiManager;
        [Inject] private SignalBus _signalBus;

        private LocalPlayer _player;
        private AIPlayer _opponent;

        public void Initialize()
        {
            _signalBus.Subscribe<GameStartSignal>(StartNewGame);
            _signalBus.Subscribe<CardPlayedSignal>(OnCardPlayed);
        }

        private async void StartNewGame()
        {
            await _server.StartNewGame();
            CreatePlayers();
            _uiManager.ShowGameplayScreen();
        }

        private void CreatePlayers()
        {
            var playerArea = _uiManager.GetPlayerArea();
            var opponentArea = _uiManager.GetOpponentArea();

            _player = CreatePlayer<LocalPlayer>(playerArea);
            _opponent = CreatePlayer<AIPlayer>(opponentArea);
        }

        private T CreatePlayer<T>(Transform parent) where T : PlayerController
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private void OnCardPlayed(CardPlayedSignal signal)
        {
            // Handle card played
        }
    }
}
