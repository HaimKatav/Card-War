using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using CardWar.Core.GameLogic;

namespace CardWar.Services.Network
{
    public class FakeWarServer : IFakeServerService
    {
        private GameStateData _gameStateData;
        private Queue<CardData> _playerDeck;
        private Queue<CardData> _opponentDeck;
        private List<CardData> _allCards;
        private WarResolver _warResolver;
        
        // Network simulation settings
        private readonly float _baseNetworkDelay = 0.1f;
        private readonly float _maxNetworkDelay = 2.0f;
        private readonly float _errorChance = 0.05f; // 5% chance of network error
        private readonly float _timeoutChance = 0.02f; // 2% chance of timeout

        public FakeWarServer()
        {
            _warResolver = new WarResolver();
            InitializeGame();
        }

        public async UniTask<ServerResponseData<bool>> StartNewGame()
        {
            await SimulateNetworkDelay();
            
            if (ShouldSimulateError())
            {
                return new ServerResponseData<bool>(false, false, "Failed to start new game: Network error");
            }

            try
            {
                InitializeGame();
                return new ServerResponseData<bool>(true, true);
            }
            catch (Exception ex)
            {
                return new ServerResponseData<bool>(false, false, $"Server error: {ex.Message}");
            }
        }

        public async UniTask<ServerResponseData<GameRoundResultData>> DrawCard()
        {
            var networkDelay = await SimulateNetworkDelay();
            
            if (ShouldSimulateTimeout())
            {
                await UniTask.Delay(TimeSpan.FromSeconds(5f)); // Simulate timeout
                return new ServerResponseData<GameRoundResultData>(null, false, "Request timeout", networkDelay);
            }
            
            if (ShouldSimulateError())
            {
                return new ServerResponseData<GameRoundResultData>(null, false, "Network error occurred", networkDelay);
            }

            try
            {
                var result = ProcessCardDraw();
                return new ServerResponseData<GameRoundResultData>(result, true, networkDelay: networkDelay);
            }
            catch (Exception ex)
            {
                return new ServerResponseData<GameRoundResultData>(null, false, $"Server error: {ex.Message}", networkDelay);
            }
        }

        public async UniTask<ServerResponseData<GameStateData>> GetGameState()
        {
            var networkDelay = await SimulateNetworkDelay();
            
            if (ShouldSimulateError())
            {
                return new ServerResponseData<GameStateData>(null, false, "Failed to get game state", networkDelay);
            }

            return new ServerResponseData<GameStateData>(_gameStateData, true, networkDelay: networkDelay);
        }

        private void InitializeGame()
        {
            Debug.Log("FakeWarServer: Initializing new game");
            
            CreateAndShuffleDeck();
            DealCards();
            
            _gameStateData = new GameStateData
            {
                PlayerCardsCount = _playerDeck.Count,
                OpponentCardsCount = _opponentDeck.Count,
                IsGameActive = true,
                RoundsPlayed = 0
            };
        }

        private void CreateAndShuffleDeck()
        {
            _allCards = new List<CardData>();
            
            // Create standard 52-card deck
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardRank rank in Enum.GetValues(typeof(CardRank)))
                {
                    _allCards.Add(new CardData(suit, rank));
                }
            }
            
            // Shuffle using Fisher-Yates algorithm
            for (int i = _allCards.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                var temp = _allCards[i];
                _allCards[i] = _allCards[randomIndex];
                _allCards[randomIndex] = temp;
            }
        }

        private void DealCards()
        {
            _playerDeck = new Queue<CardData>();
            _opponentDeck = new Queue<CardData>();
            
            // Deal cards alternately
            for (int i = 0; i < _allCards.Count; i++)
            {
                if (i % 2 == 0)
                    _playerDeck.Enqueue(_allCards[i]);
                else
                    _opponentDeck.Enqueue(_allCards[i]);
            }
        }

        private GameRoundResultData ProcessCardDraw()
        {
            if (_playerDeck.Count == 0 || _opponentDeck.Count == 0)
            {
                return CreateGameEndResult();
            }

            var playerCard = _playerDeck.Dequeue();
            var opponentCard = _opponentDeck.Dequeue();
            
            _gameStateData.LastPlayerCard = playerCard;
            _gameStateData.LastOpponentCard = opponentCard;
            _gameStateData.RoundsPlayed++;

            var result = DetermineRoundWinner(playerCard, opponentCard);
            var roundResult = new GameRoundResultData
            {
                PlayerCard = playerCard,
                OpponentCard = opponentCard,
                Result = result.gameResult,
                CardsWon = result.cardsWon,
                WarData = result.warData,
                RequiredPoolSize = result.warData?.RequiredPoolSize ?? 16, // Use war data or default minimum
                AnimationDuration = result.warData?.EstimatedAnimationDuration ?? 2.0f // Use war data or default duration
            };

            // Update deck counts
            _gameStateData.PlayerCardsCount = _playerDeck.Count;
            _gameStateData.OpponentCardsCount = _opponentDeck.Count;
            
            // Check if game has ended
            if (_playerDeck.Count == 0 || _opponentDeck.Count == 0)
            {
                _gameStateData.IsGameActive = false;
                roundResult.IsGameEnded = true;
            }

            return roundResult;
        }

        private (GameResult gameResult, int cardsWon, WarData warData) DetermineRoundWinner(CardData playerCard, CardData opponentCard)
        {
            if (playerCard.Value > opponentCard.Value)
            {
                // Player wins - add both cards to player deck
                _playerDeck.Enqueue(playerCard);
                _playerDeck.Enqueue(opponentCard);
                return (GameResult.PlayerWins, 2, null);
            }
            else if (opponentCard.Value > playerCard.Value)
            {
                // Opponent wins - add both cards to opponent deck
                _opponentDeck.Enqueue(opponentCard);
                _opponentDeck.Enqueue(playerCard);
                return (GameResult.OpponentWins, 2, null);
            }
            else
            {
                // War situation - resolve complete war scenario
                var warData = ResolveCompleteWar(new List<CardData> { playerCard, opponentCard });
                
                // Return war result with complete data
                return (GameResult.War, warData.TotalCardsWon, warData);
            }
        }

        private WarData ResolveCompleteWar(List<CardData> initialCards)
        {
            Debug.Log($"[FakeWarServer] Starting war resolution with {initialCards.Count} initial cards");
            
            // Create copies of decks for war resolution (don't modify original decks yet)
            var playerDeckCopy = new Queue<CardData>(_playerDeck);
            var opponentDeckCopy = new Queue<CardData>(_opponentDeck);
            
            // Resolve complete war using WarResolver
            var warData = _warResolver.ResolveWar(initialCards, playerDeckCopy, opponentDeckCopy);
            
            // Update actual decks with war results
            _playerDeck = playerDeckCopy;
            _opponentDeck = opponentDeckCopy;
            
            // Handle shuffle if required
            if (warData.RequiresShuffle)
            {
                Debug.Log("[FakeWarServer] War resulted in draw - shuffling decks");
                HandleWarDraw();
            }
            
            Debug.Log($"[FakeWarServer] War resolved: Player {warData.WinningPlayerNumber} wins {warData.TotalCardsWon} cards");
            
            return warData;
        }

        private void HandleWarDraw()
        {
            // Collect all cards from both decks
            var allCards = new List<CardData>();
            
            while (_playerDeck.Count > 0)
                allCards.Add(_playerDeck.Dequeue());
                
            while (_opponentDeck.Count > 0)
                allCards.Add(_opponentDeck.Dequeue());
            
            // Shuffle all cards
            for (int i = allCards.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                var temp = allCards[i];
                allCards[i] = allCards[randomIndex];
                allCards[randomIndex] = temp;
            }
            
            // Redeal cards
            for (int i = 0; i < allCards.Count; i++)
            {
                if (i % 2 == 0)
                    _playerDeck.Enqueue(allCards[i]);
                else
                    _opponentDeck.Enqueue(allCards[i]);
            }
            
            Debug.Log($"[FakeWarServer] Cards shuffled and redealt: Player {_playerDeck.Count}, Opponent {_opponentDeck.Count}");
        }

        private GameRoundResultData CreateGameEndResult()
        {
            bool playerWon = _playerDeck.Count > _opponentDeck.Count;
            return new GameRoundResultData
            {
                PlayerCard = _gameStateData.LastPlayerCard,
                OpponentCard = _gameStateData.LastOpponentCard,
                Result = playerWon ? GameResult.PlayerWins : GameResult.OpponentWins,
                IsGameEnded = true,
                CardsWon = 0,
                RequiredPoolSize = 16,
                AnimationDuration = 1.0f
            };
        }

        private async UniTask<float> SimulateNetworkDelay()
        {
            var delay = Random.Range(_baseNetworkDelay, _maxNetworkDelay);
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            return delay;
        }

        private bool ShouldSimulateError()
        {
            return Random.value < _errorChance;
        }

        private bool ShouldSimulateTimeout()
        {
            return Random.value < _timeoutChance;
        }
    }
}