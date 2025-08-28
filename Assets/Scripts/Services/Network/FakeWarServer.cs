using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CardWar.Core.Data;
using CardWar.Core.Enums;
using Random = UnityEngine.Random;

namespace CardWar.Services.Network
{
    public class FakeWarServer : IFakeServerService
    {
        private readonly NetworkErrorConfig _errorConfig;
        
        private GameStateData _currentGameState;
        private Queue<CardData> _playerDeck;
        private Queue<CardData> _opponentDeck;
        private List<CardData> _warPile;
        private bool _isGameActive;
        
        public FakeWarServer(NetworkErrorConfig errorConfig)
        {
            _errorConfig = errorConfig ?? CreateDefaultErrorConfig();
            _warPile = new List<CardData>();
        }
        
        public async UniTask<ServerResponseData<bool>> StartNewGame()
        {
            await SimulateNetworkDelay();
            
            if (ShouldSimulateError())
            {
                return new ServerResponseData<bool>(false, false, GetRandomErrorMessage(), _errorConfig.maxNetworkDelay);
            }
            
            try
            {
                InitializeNewGame();
                
                _currentGameState = new GameStateData
                {
                    PlayerCardsCount = _playerDeck.Count,
                    OpponentCardsCount = _opponentDeck.Count,
                    IsGameActive = true,
                    RoundsPlayed = 0,
                    GameStartTime = DateTime.Now,
                    TotalWars = 0
                };
                
                _isGameActive = true;
                
                Debug.Log($"[FakeWarServer] New game started. Player: {_playerDeck.Count}, Opponent: {_opponentDeck.Count}");
                return new ServerResponseData<bool>(true, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FakeWarServer] Failed to start game: {ex.Message}");
                return new ServerResponseData<bool>(false, false, "Failed to initialize game");
            }
        }
        
        public async UniTask<ServerResponseData<GameRoundResultData>> DrawCard()
        {
            await SimulateNetworkDelay();
            
            if (ShouldSimulateError())
            {
                return new ServerResponseData<GameRoundResultData>(null, false, GetRandomErrorMessage(), _errorConfig.maxNetworkDelay);
            }
            
            if (!_isGameActive)
            {
                return new ServerResponseData<GameRoundResultData>(null, false, "Game is not active");
            }
            
            if (_playerDeck.Count == 0 || _opponentDeck.Count == 0)
            {
                return new ServerResponseData<GameRoundResultData>(
                    CreateGameEndResult(), 
                    true);
            }
            
            try
            {
                var result = ProcessGameRound();
                UpdateGameState();
                
                Debug.Log($"[FakeWarServer] Round complete. Result: {result.Result}, Player: {_playerDeck.Count}, Opponent: {_opponentDeck.Count}");
                
                return new ServerResponseData<GameRoundResultData>(result, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FakeWarServer] Round processing failed: {ex.Message}");
                return new ServerResponseData<GameRoundResultData>(null, false, "Round processing failed");
            }
        }
        
        public async UniTask<ServerResponseData<GameStateData>> GetGameState()
        {
            await SimulateNetworkDelay();
            
            if (ShouldSimulateError())
            {
                return new ServerResponseData<GameStateData>(null, false, GetRandomErrorMessage(), _errorConfig.maxNetworkDelay);
            }
            
            return new ServerResponseData<GameStateData>(_currentGameState, true);
        }
        
        public async UniTask<ServerResponseData<bool>> EndGame()
        {
            await SimulateNetworkDelay();
            
            _isGameActive = false;
            _playerDeck?.Clear();
            _opponentDeck?.Clear();
            _warPile?.Clear();
            
            return new ServerResponseData<bool>(true, true);
        }
        
        private void InitializeNewGame()
        {
            var allCards = CreateStandardDeck();
            ShuffleDeck(allCards);
            
            _playerDeck = new Queue<CardData>();
            _opponentDeck = new Queue<CardData>();
            _warPile.Clear();
            
            // Deal cards evenly
            for (int i = 0; i < allCards.Count; i++)
            {
                if (i % 2 == 0)
                    _playerDeck.Enqueue(allCards[i]);
                else
                    _opponentDeck.Enqueue(allCards[i]);
            }
        }
        
        private List<CardData> CreateStandardDeck()
        {
            var deck = new List<CardData>();
            
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardRank rank in Enum.GetValues(typeof(CardRank)))
                {
                    deck.Add(new CardData(suit, rank));
                }
            }
            
            return deck;
        }
        
        private void ShuffleDeck(List<CardData> deck)
        {
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
            }
        }
        
        private GameRoundResultData ProcessGameRound()
        {
            var playerCard = _playerDeck.Dequeue();
            var opponentCard = _opponentDeck.Dequeue();
            
            var result = new GameRoundResultData
            {
                PlayerCard = playerCard,
                OpponentCard = opponentCard,
                CardsWon = 2  // Base case: winner gets both cards
            };
            
            if (playerCard.Value > opponentCard.Value)
            {
                result.Result = GameResult.PlayerWin;
                _playerDeck.Enqueue(playerCard);
                _playerDeck.Enqueue(opponentCard);
                
                // Add any war pile cards
                foreach (var card in _warPile)
                {
                    _playerDeck.Enqueue(card);
                    result.CardsWon++;
                }
                _warPile.Clear();
            }
            else if (opponentCard.Value > playerCard.Value)
            {
                result.Result = GameResult.OpponentWin;
                _opponentDeck.Enqueue(opponentCard);
                _opponentDeck.Enqueue(playerCard);
                
                // Add any war pile cards
                foreach (var card in _warPile)
                {
                    _opponentDeck.Enqueue(card);
                    result.CardsWon++;
                }
                _warPile.Clear();
            }
            else  // WAR!
            {
                result.Result = GameResult.War;
                result.WarDetails = ProcessWar(playerCard, opponentCard);
                result.CardsWon = result.WarDetails.TotalCardsWon;
            }
            
            // Check for game end
            result.IsGameEnded = _playerDeck.Count == 0 || _opponentDeck.Count == 0;
            
            return result;
        }
        
        private WarData ProcessWar(CardData initialPlayerCard, CardData initialOpponentCard)
        {
            var warData = new WarData();
            warData.InitialWarCards.Add(initialPlayerCard);
            warData.InitialWarCards.Add(initialOpponentCard);
            
            _warPile.Add(initialPlayerCard);
            _warPile.Add(initialOpponentCard);
            
            bool warContinues = true;
            int roundNumber = 1;
            
            while (warContinues && _playerDeck.Count > 0 && _opponentDeck.Count > 0)
            {
                var warRound = new WarRound { RoundNumber = roundNumber };
                
                // Each player places up to 3 face-down cards
                int cardsToPlace = Math.Min(3, Math.Min(_playerDeck.Count - 1, _opponentDeck.Count - 1));
                
                if (cardsToPlace <= 0)
                {
                    // Not enough cards for war - handle edge case
                    warData.RequiresShuffle = true;
                    HandleWarCardExhaustion();
                    break;
                }
                
                // Place face-down cards
                for (int i = 0; i < cardsToPlace; i++)
                {
                    if (_playerDeck.Count > 1)  // Keep at least 1 for fighting
                    {
                        var card = _playerDeck.Dequeue();
                        warRound.PlayerCards.Add(card);
                        _warPile.Add(card);
                    }
                    
                    if (_opponentDeck.Count > 1)  // Keep at least 1 for fighting
                    {
                        var card = _opponentDeck.Dequeue();
                        warRound.OpponentCards.Add(card);
                        _warPile.Add(card);
                    }
                }
                
                // Fighting cards (face-up)
                if (_playerDeck.Count > 0 && _opponentDeck.Count > 0)
                {
                    var playerFightCard = _playerDeck.Dequeue();
                    var opponentFightCard = _opponentDeck.Dequeue();
                    
                    warRound.PlayerFightingCard = playerFightCard;
                    warRound.OpponentFightingCard = opponentFightCard;
                    
                    _warPile.Add(playerFightCard);
                    _warPile.Add(opponentFightCard);
                    
                    if (playerFightCard.Value > opponentFightCard.Value)
                    {
                        warContinues = false;
                        warData.WinningPlayerNumber = 1;
                        
                        // Player wins all war cards
                        foreach (var card in _warPile)
                        {
                            _playerDeck.Enqueue(card);
                        }
                    }
                    else if (opponentFightCard.Value > playerFightCard.Value)
                    {
                        warContinues = false;
                        warData.WinningPlayerNumber = 2;
                        
                        // Opponent wins all war cards
                        foreach (var card in _warPile)
                        {
                            _opponentDeck.Enqueue(card);
                        }
                    }
                    else
                    {
                        // Another war!
                        warRound.ResultedInWar = true;
                    }
                }
                
                warData.AllWarRounds.Add(warRound);
                roundNumber++;
                
                // Prevent infinite wars
                if (roundNumber > 10)
                {
                    Debug.LogWarning("[FakeWarServer] War exceeded 10 rounds, forcing resolution");
                    warContinues = false;
                    warData.WinningPlayerNumber = Random.Range(1, 3);
                    break;
                }
            }
            
            warData.TotalCardsWon = _warPile.Count;
            
            if (!warData.RequiresShuffle)
            {
                _warPile.Clear();
            }
            
            _currentGameState.TotalWars++;
            
            return warData;
        }
        
        private void HandleWarCardExhaustion()
        {
            // Shuffle war pile and redistribute
            var allRemainingCards = new List<CardData>(_warPile);
            allRemainingCards.AddRange(_playerDeck);
            allRemainingCards.AddRange(_opponentDeck);
            
            ShuffleDeck(allRemainingCards);
            
            _playerDeck.Clear();
            _opponentDeck.Clear();
            _warPile.Clear();
            
            // Redistribute evenly
            for (int i = 0; i < allRemainingCards.Count; i++)
            {
                if (i % 2 == 0)
                    _playerDeck.Enqueue(allRemainingCards[i]);
                else
                    _opponentDeck.Enqueue(allRemainingCards[i]);
            }
            
            Debug.Log("[FakeWarServer] Cards exhausted during war - redistributed all cards");
        }
        
        private GameRoundResultData CreateGameEndResult()
        {
            _isGameActive = false;
            
            return new GameRoundResultData
            {
                IsGameEnded = true,
                Result = _playerDeck.Count > _opponentDeck.Count ? GameResult.PlayerWin : GameResult.OpponentWin,
                CardsWon = 0
            };
        }
        
        private void UpdateGameState()
        {
            if (_currentGameState == null) return;
            
            _currentGameState.RoundsPlayed++;
            _currentGameState.PlayerCardsCount = _playerDeck.Count;
            _currentGameState.OpponentCardsCount = _opponentDeck.Count;
            _currentGameState.IsGameActive = _isGameActive;
        }
        
        private async UniTask SimulateNetworkDelay()
        {
            float delay = Random.Range(_errorConfig.minNetworkDelay, _errorConfig.maxNetworkDelay);
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
        }
        
        private bool ShouldSimulateError()
        {
            float random = Random.value;
            return random < (_errorConfig.networkErrorRate + _errorConfig.serverErrorRate);
        }
        
        private string GetRandomErrorMessage()
        {
            bool isNetworkError = Random.value < 0.5f;
            var messages = isNetworkError ? _errorConfig.networkErrorMessages : _errorConfig.serverErrorMessages;
            return messages[Random.Range(0, messages.Length)];
        }
        
        private NetworkErrorConfig CreateDefaultErrorConfig()
        {
            return new NetworkErrorConfig
            {
                timeoutRate = 0.02f,
                networkErrorRate = 0.05f,
                serverErrorRate = 0.01f,
                corruptionRate = 0.005f,
                minNetworkDelay = 0.1f,
                maxNetworkDelay = 0.5f,
                timeoutDuration = 5.0f,
                retryBaseDelay = 1.0f
            };
        }
    }
}