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
        private int _currentWarDepth;
        private bool _isInWar;
        private MatchData _currentMatch;

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
            
            CleanupMatch();
            
            _currentMatch = new MatchData
            {
                MatchId = Guid.NewGuid().ToString(),
                StartTime = DateTime.Now,
                Status = GameStatus.InProgress
            };
            
            _playerDeck = new List<CardData>();
            _opponentDeck = new List<CardData>();
            _warPot = new List<CardData>();
            _roundNumber = 0;
            _gameStatus = GameStatus.InProgress;
            _currentWarDepth = 0;
            _isInWar = false;
            
            var fullDeck = GenerateFullDeck();
            ShuffleDeck(fullDeck);
            DealCards(fullDeck);
            
            Debug.Log($"[FakeWarServer] Match {_currentMatch.MatchId} initialized - Player: {_playerDeck.Count}, Opponent: {_opponentDeck.Count}");
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
            _currentWarDepth = 0;
            _isInWar = false;
            
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
                IsWar = false,
                WarDepth = 0
            };
            
            if (playerCard.Rank == opponentCard.Rank)
            {
                roundData.IsWar = true;
                roundData.Result = RoundResult.War;
                _warPot.Add(playerCard);
                _warPot.Add(opponentCard);
                _isInWar = true;
                _currentWarDepth = 1;
                roundData.WarDepth = _currentWarDepth;
                Debug.Log($"[FakeWarServer] WAR! Both played {playerCard.Rank} - War depth: {_currentWarDepth}");
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
                OpponentWarCards = new List<CardData>(),
                WarDepth = _currentWarDepth
            };

            if (_playerDeck.Count == 0 || _opponentDeck.Count == 0)
            {
                DetermineWinner();
                return CreateGameOverRound();
            }

            var playerAvailableCards = _playerDeck.Count;
            var opponentAvailableCards = _opponentDeck.Count;
            var minAvailableCards = Math.Min(playerAvailableCards, opponentAvailableCards);

            if (minAvailableCards == 0)
            {
                return CreateWarDrawRound();
            }

            var warCardsPerPlayer = Math.Min(minAvailableCards, 4);

            if (minAvailableCards < 4)
            {
                Debug.Log(
                    $"[FakeWarServer] Limited war cards - Player has {playerAvailableCards}, Opponent has {opponentAvailableCards}, using {warCardsPerPlayer} cards each");
            }

            Debug.Log($"[FakeWarServer] War #{_currentWarDepth} with {warCardsPerPlayer} cards per player");

            for (var i = 0; i < warCardsPerPlayer; i++)
            {
                var card = _playerDeck[0];
                _playerDeck.RemoveAt(0);
                roundData.PlayerWarCards.Add(card);
                _warPot.Add(card);
            }

            for (var i = 0; i < warCardsPerPlayer; i++)
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
                _currentWarDepth++;
                roundData.WarDepth = _currentWarDepth;
                Debug.Log(
                    $"[FakeWarServer] CHAINED WAR! Both played {playerBattleCard.Rank} - War depth now: {_currentWarDepth}");
            }
            else if (playerBattleCard.Rank > opponentBattleCard.Rank)
            {
                roundData.Result = RoundResult.PlayerWins;
                var collectedCards = _warPot.Count;
                CollectWarPot(_playerDeck);
                Debug.Log(
                    $"[FakeWarServer] Player wins war #{_currentWarDepth}: {playerBattleCard.Rank} beats {opponentBattleCard.Rank}");
                Debug.Log($"[FakeWarServer] Player collected {collectedCards} cards from war");
                _currentWarDepth = 0;
                _isInWar = false;
            }
            else
            {
                roundData.Result = RoundResult.OpponentWins;
                var collectedCards = _warPot.Count;
                CollectWarPot(_opponentDeck);
                Debug.Log(
                    $"[FakeWarServer] Opponent wins war #{_currentWarDepth}: {opponentBattleCard.Rank} beats {playerBattleCard.Rank}");
                Debug.Log($"[FakeWarServer] Opponent collected {collectedCards} cards from war");
                _currentWarDepth = 0;
                _isInWar = false;
            }

            roundData.PlayerCardsRemaining = _playerDeck.Count;
            roundData.OpponentCardsRemaining = _opponentDeck.Count;
            roundData.TotalCardsInPot = _warPot.Count;

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
                WarPotCount = _warPot.Count,
                CurrentWarDepth = _currentWarDepth
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
                (deck[i], deck[j]) = (deck[j], deck[i]);
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
            }
            else if (_opponentDeck.Count == 0)
            {
                _gameStatus = GameStatus.PlayerWon;
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

            if (_currentMatch != null)
            {
                _currentMatch.Status = _gameStatus;
                _currentMatch.EndTime = DateTime.Now;
                _currentMatch.TotalRounds = _roundNumber;
                Debug.Log($"[FakeWarServer] Match {_currentMatch.MatchId} ended - Status: {_gameStatus}, Rounds: {_roundNumber}");
            }
        }

        private RoundData CreateGameOverRound()
        {
            return new RoundData
            {
                RoundNumber = _roundNumber,
                IsGameOver = true,
                Result = _playerDeck.Count > _opponentDeck.Count ? 
                    RoundResult.PlayerWins : RoundResult.OpponentWins,
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

        private RoundData CreateWarDrawRound()
        {
            Debug.Log("[FakeWarServer] Creating war draw scenario - insufficient cards for consecutive war");
            
            var drawRound = new RoundData
            {
                RoundNumber = _roundNumber,
                IsWar = true,
                Result = RoundResult.Draw,
                WarEndedInDraw = true,
                PlayerCardsRemaining = _playerDeck.Count,
                OpponentCardsRemaining = _opponentDeck.Count,
                PlayerWarCards = new List<CardData>(),
                OpponentWarCards = new List<CardData>(),
                TotalCardsInPot = _warPot.Count,
                WarDepth = _currentWarDepth
            };
            
            ReturnWarPotToBothPlayers();
            
            _currentWarDepth = 0;
            _isInWar = false;
            
            return drawRound;
        }
        
        private void ReturnWarPotToBothPlayers()
        {
            Debug.Log($"[FakeWarServer] Returning {_warPot.Count} cards from war pot to both players");
            
            var playerCards = new List<CardData>();
            var opponentCards = new List<CardData>();
            
            for (var i = 0; i < _warPot.Count; i++)
            {
                if (i % 2 == 0)
                    playerCards.Add(_warPot[i]);
                else
                    opponentCards.Add(_warPot[i]);
            }
            
            _playerDeck.AddRange(playerCards);
            _opponentDeck.AddRange(opponentCards);
            
            ShuffleDeck(_playerDeck);
            ShuffleDeck(_opponentDeck);
            
            _warPot.Clear();
            
            Debug.Log($"[FakeWarServer] Cards returned - Player: {_playerDeck.Count}, Opponent: {_opponentDeck.Count}");
        }

        private void CleanupMatch()
        {
            if (_currentMatch != null)
            {
                Debug.Log($"[FakeWarServer] Cleaning up match {_currentMatch.MatchId}");
                _currentMatch = null;
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
        public int CurrentWarDepth { get; set; }
    }

    public class MatchData
    {
        public string MatchId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public GameStatus Status { get; set; }
        public int TotalRounds { get; set; }
    }
}