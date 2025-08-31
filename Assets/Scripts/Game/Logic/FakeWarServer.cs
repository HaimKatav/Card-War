using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CardWar.Common;
using CardWar.Core;
using Cysharp.Threading.Tasks;

namespace CardWar.Game.Logic
{
    public class FakeWarServer
    {
        public GameStatus Status => _gameStatus;
        public int RoundNumber => _roundNumber;
        public int PlayerCardCount => _playerDeck?.Count ?? 0;
        public int OpponentCardCount => _opponentDeck?.Count ?? 0;
        
        private GameSettings _gameSettings;
        private List<CardData> _playerDeck;
        private List<CardData> _opponentDeck;
        private List<CardData> _warPot;
        private int _roundNumber;
        private GameStatus _gameStatus;
        private System.Random _random;

        public FakeWarServer(GameSettings gameSettings)
        {
            _gameSettings = gameSettings;
            _random = new System.Random();
            _warPot = new List<CardData>();
        }

        public async UniTask<bool> InitializeNewGame()
        {
            Debug.Log("[FakeWarServer] Initializing new game");
            
            await SimulateNetworkDelay();
            
            if (ShouldSimulateFailure())
            {
                Debug.LogWarning("[FakeWarServer] Simulated network failure during InitializeNewGame");
                return false;
            }
            
            _playerDeck = new List<CardData>();
            _opponentDeck = new List<CardData>();
            _warPot = new List<CardData>();
            _roundNumber = 0;
            _gameStatus = GameStatus.InProgress;
            
            var fullDeck = GenerateFullDeck();
            ShuffleDeck(fullDeck);
            DealCards(fullDeck);
            
            Debug.Log($"[FakeWarServer] Game initialized - Player: {_playerDeck.Count}, Opponent: {_opponentDeck.Count}");
            return true;
        }

        public async UniTask<RoundData> DrawCards()
        {
            if (_gameStatus != GameStatus.InProgress)
            {
                Debug.LogWarning("[FakeWarServer] Game not in progress");
                return null;
            }
            
            await SimulateNetworkDelay();
            
            if (ShouldSimulateFailure())
            {
                Debug.LogWarning("[FakeWarServer] Simulated network failure during DrawCards");
                return null;
            }
            
            if (!HasCardsToPlay())
            {
                DetermineWinner();
                return CreateGameOverRound();
            }
            
            _roundNumber++;
            
            var playerCard = _playerDeck[0];
            var opponentCard = _opponentDeck[0];
            
            _playerDeck.RemoveAt(0);
            _opponentDeck.RemoveAt(0);
            
            var roundData = new RoundData
            {
                RoundNumber = _roundNumber,
                PlayerCard = playerCard,
                OpponentCard = opponentCard,
                PlayerCardsRemaining = _playerDeck.Count,
                OpponentCardsRemaining = _opponentDeck.Count,
                IsWar = false
            };
            
            if (playerCard.Rank == opponentCard.Rank)
            {
                roundData.IsWar = true;
                roundData.Result = RoundResult.War;
                _warPot.Add(playerCard);
                _warPot.Add(opponentCard);
                Debug.Log($"[FakeWarServer] WAR! Both played {playerCard.Rank}");
            }
            else if (playerCard.Rank > opponentCard.Rank)
            {
                roundData.Result = RoundResult.PlayerWins;
                _playerDeck.Add(playerCard);
                _playerDeck.Add(opponentCard);
                CollectWarPot(_playerDeck);
                Debug.Log($"[FakeWarServer] Player wins: {playerCard.Rank} beats {opponentCard.Rank}");
            }
            else
            {
                roundData.Result = RoundResult.OpponentWins;
                _opponentDeck.Add(opponentCard);
                _opponentDeck.Add(playerCard);
                CollectWarPot(_opponentDeck);
                Debug.Log($"[FakeWarServer] Opponent wins: {opponentCard.Rank} beats {playerCard.Rank}");
            }
            
            roundData.PlayerCardsRemaining = _playerDeck.Count;
            roundData.OpponentCardsRemaining = _opponentDeck.Count;
            
            CheckGameOver();
            
            return roundData;
        }

        public async UniTask<RoundData> ResolveWar()
        {
            if (_gameStatus != GameStatus.InProgress)
            {
                Debug.LogWarning("[FakeWarServer] Game not in progress");
                return null;
            }
            
            await SimulateNetworkDelay();
            
            if (ShouldSimulateFailure())
            {
                Debug.LogWarning("[FakeWarServer] Simulated network failure during ResolveWar");
                return null;
            }
            
            var roundData = new RoundData
            {
                RoundNumber = _roundNumber,
                IsWar = true,
                PlayerWarCards = new List<CardData>(),
                OpponentWarCards = new List<CardData>()
            };
            
            var playerWarCardCount = Math.Min(4, _playerDeck.Count);
            var opponentWarCardCount = Math.Min(4, _opponentDeck.Count);
            
            if (playerWarCardCount == 0 || opponentWarCardCount == 0)
            {
                DetermineWinner();
                return CreateGameOverRound();
            }
            
            for (var i = 0; i < playerWarCardCount; i++)
            {
                var card = _playerDeck[0];
                _playerDeck.RemoveAt(0);
                roundData.PlayerWarCards.Add(card);
                _warPot.Add(card);
            }
            
            for (var i = 0; i < opponentWarCardCount; i++)
            {
                var card = _opponentDeck[0];
                _opponentDeck.RemoveAt(0);
                roundData.OpponentWarCards.Add(card);
                _warPot.Add(card);
            }
            
            var playerBattleCard = roundData.PlayerWarCards.Last();
            var opponentBattleCard = roundData.OpponentWarCards.Last();
            
            roundData.PlayerCard = playerBattleCard;
            roundData.OpponentCard = opponentBattleCard;
            
            if (playerBattleCard.Rank == opponentBattleCard.Rank)
            {
                roundData.Result = RoundResult.War;
                roundData.HasChainedWar = true;
                Debug.Log($"[FakeWarServer] CHAINED WAR! Both played {playerBattleCard.Rank}");
            }
            else if (playerBattleCard.Rank > opponentBattleCard.Rank)
            {
                roundData.Result = RoundResult.PlayerWins;
                CollectWarPot(_playerDeck);
                Debug.Log($"[FakeWarServer] Player wins war: {playerBattleCard.Rank} beats {opponentBattleCard.Rank}");
                Debug.Log($"[FakeWarServer] Player collected {_warPot.Count} cards from war");
                _warPot.Clear();
            }
            else
            {
                roundData.Result = RoundResult.OpponentWins;
                CollectWarPot(_opponentDeck);
                Debug.Log($"[FakeWarServer] Opponent wins war: {opponentBattleCard.Rank} beats {playerBattleCard.Rank}");
                Debug.Log($"[FakeWarServer] Opponent collected {_warPot.Count} cards from war");
                _warPot.Clear();
            }
            
            roundData.PlayerCardsRemaining = _playerDeck.Count;
            roundData.OpponentCardsRemaining = _opponentDeck.Count;
            
            CheckGameOver();
            
            return roundData;
        }

        public async UniTask<GameStats> GetGameStats()
        {
            await SimulateNetworkDelay();
            
            if (ShouldSimulateFailure())
            {
                Debug.LogWarning("[FakeWarServer] Simulated network failure during GetGameStats");
                return null;
            }
            
            return new GameStats
            {
                PlayerCardCount = _playerDeck.Count,
                OpponentCardCount = _opponentDeck.Count,
                RoundNumber = _roundNumber,
                Status = _gameStatus,
                WarPotCount = _warPot.Count
            };
        }

        #region Private Methods

        private bool ShouldSimulateFailure()
        {
            if (_gameSettings == null || _gameSettings.FakeNetworkErrorRate <= 0)
                return false;
                
            var shouldFail = _random.NextDouble() < _gameSettings.FakeNetworkErrorRate;
            
            if (shouldFail)
            {
                Debug.LogWarning($"[FakeWarServer] Simulating network failure (rate: {_gameSettings.FakeNetworkErrorRate:P0})");
            }
            
            return shouldFail;
        }

        private List<CardData> GenerateFullDeck()
        {
            var deck = new List<CardData>();
            
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    deck.Add(new CardData { Suit = suit, Rank = rank });
                }
            }
            
            return deck;
        }

        private void ShuffleDeck(List<CardData> deck)
        {
            for (var i = deck.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                var temp = deck[i];
                deck[i] = deck[j];
                deck[j] = temp;
            }
        }

        private void DealCards(List<CardData> fullDeck)
        {
            for (var i = 0; i < fullDeck.Count; i++)
            {
                if (i % 2 == 0)
                    _playerDeck.Add(fullDeck[i]);
                else
                    _opponentDeck.Add(fullDeck[i]);
            }
        }

        private bool HasCardsToPlay()
        {
            return _playerDeck.Count > 0 && _opponentDeck.Count > 0;
        }

        private void CollectWarPot(List<CardData> winnerDeck)
        {
            if (_warPot.Count > 0)
            {
                winnerDeck.AddRange(_warPot);
                _warPot.Clear();
            }
        }

        private void CheckGameOver()
        {
            if (_playerDeck.Count == 0)
            {
                _gameStatus = GameStatus.OpponentWon;
                Debug.Log("[FakeWarServer] Game Over - Opponent wins!");
            }
            else if (_opponentDeck.Count == 0)
            {
                _gameStatus = GameStatus.PlayerWon;
                Debug.Log("[FakeWarServer] Game Over - Player wins!");
            }
            else if (_playerDeck.Count + _opponentDeck.Count + _warPot.Count != 52)
            {
                Debug.LogError($"[FakeWarServer] Card count error! Total: {_playerDeck.Count + _opponentDeck.Count + _warPot.Count}");
            }
        }

        private void DetermineWinner()
        {
            if (_playerDeck.Count > _opponentDeck.Count)
                _gameStatus = GameStatus.PlayerWon;
            else if (_opponentDeck.Count > _playerDeck.Count)
                _gameStatus = GameStatus.OpponentWon;
            else
                _gameStatus = GameStatus.Draw;
        }

        private RoundData CreateGameOverRound()
        {
            return new RoundData
            {
                RoundNumber = _roundNumber,
                Result = _gameStatus == GameStatus.PlayerWon ? RoundResult.PlayerWins : RoundResult.OpponentWins,
                PlayerCardsRemaining = _playerDeck.Count,
                OpponentCardsRemaining = _opponentDeck.Count
            };
        }

        private async UniTask SimulateNetworkDelay()
        {
            if (_gameSettings != null && _gameSettings.FakeNetworkDelay > 0)
            {
                await UniTask.Delay((int)(_gameSettings.FakeNetworkDelay * 1000));
            }
        }

        #endregion
    }

    public class GameStats
    {
        public int PlayerCardCount { get; set; }
        public int OpponentCardCount { get; set; }
        public int RoundNumber { get; set; }
        public GameStatus Status { get; set; }
        public int WarPotCount { get; set; }
    }
}