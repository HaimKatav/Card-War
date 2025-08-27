using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class FakeWarServer : IFakeServerService
{
    private GameState _gameState;
    private Queue<CardData> _playerDeck;
    private Queue<CardData> _opponentDeck;
    private List<CardData> _allCards;
    
    // Network simulation settings
    private readonly float _baseNetworkDelay = 0.1f;
    private readonly float _maxNetworkDelay = 2.0f;
    private readonly float _errorChance = 0.05f; // 5% chance of network error
    private readonly float _timeoutChance = 0.02f; // 2% chance of timeout

    public FakeWarServer()
    {
        InitializeGame();
    }

    public async UniTask<ServerResponse<bool>> StartNewGame()
    {
        await SimulateNetworkDelay();
        
        if (ShouldSimulateError())
        {
            return new ServerResponse<bool>(false, false, "Failed to start new game: Network error");
        }

        try
        {
            InitializeGame();
            return new ServerResponse<bool>(true, true);
        }
        catch (Exception ex)
        {
            return new ServerResponse<bool>(false, false, $"Server error: {ex.Message}");
        }
    }

    public async UniTask<ServerResponse<GameRoundResult>> DrawCard()
    {
        var networkDelay = await SimulateNetworkDelay();
        
        if (ShouldSimulateTimeout())
        {
            await UniTask.Delay(TimeSpan.FromSeconds(5f)); // Simulate timeout
            return new ServerResponse<GameRoundResult>(null, false, "Request timeout", networkDelay);
        }
        
        if (ShouldSimulateError())
        {
            return new ServerResponse<GameRoundResult>(null, false, "Network error occurred", networkDelay);
        }

        try
        {
            var result = ProcessCardDraw();
            return new ServerResponse<GameRoundResult>(result, true, networkDelay: networkDelay);
        }
        catch (Exception ex)
        {
            return new ServerResponse<GameRoundResult>(null, false, $"Server error: {ex.Message}", networkDelay);
        }
    }

    public async UniTask<ServerResponse<GameState>> GetGameState()
    {
        var networkDelay = await SimulateNetworkDelay();
        
        if (ShouldSimulateError())
        {
            return new ServerResponse<GameState>(null, false, "Failed to get game state", networkDelay);
        }

        return new ServerResponse<GameState>(_gameState, true, networkDelay: networkDelay);
    }

    private void InitializeGame()
    {
        Debug.Log("FakeWarServer: Initializing new game");
        
        CreateAndShuffleDeck();
        DealCards();
        
        _gameState = new GameState
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

    private GameRoundResult ProcessCardDraw()
    {
        if (_playerDeck.Count == 0 || _opponentDeck.Count == 0)
        {
            return CreateGameEndResult();
        }

        var playerCard = _playerDeck.Dequeue();
        var opponentCard = _opponentDeck.Dequeue();
        
        _gameState.LastPlayerCard = playerCard;
        _gameState.LastOpponentCard = opponentCard;
        _gameState.RoundsPlayed++;

        var result = DetermineRoundWinner(playerCard, opponentCard);
        var roundResult = new GameRoundResult
        {
            PlayerCard = playerCard,
            OpponentCard = opponentCard,
            Result = result.gameResult,
            CardsWon = result.cardsWon
        };

        // Update deck counts
        _gameState.PlayerCardsCount = _playerDeck.Count;
        _gameState.OpponentCardsCount = _opponentDeck.Count;
        
        // Check if game has ended
        if (_playerDeck.Count == 0 || _opponentDeck.Count == 0)
        {
            _gameState.IsGameActive = false;
            roundResult.IsGameEnded = true;
        }

        return roundResult;
    }

    private (GameResult gameResult, int cardsWon) DetermineRoundWinner(CardData playerCard, CardData opponentCard)
    {
        if (playerCard.Value > opponentCard.Value)
        {
            // Player wins - add both cards to player deck
            _playerDeck.Enqueue(playerCard);
            _playerDeck.Enqueue(opponentCard);
            return (GameResult.PlayerWins, 2);
        }
        else if (opponentCard.Value > playerCard.Value)
        {
            // Opponent wins - add both cards to opponent deck
            _opponentDeck.Enqueue(opponentCard);
            _opponentDeck.Enqueue(playerCard);
            return (GameResult.OpponentWins, 2);
        }
        else
        {
            // War situation - simplified: each player draws 3 cards face down, then 1 face up
            return ProcessWar(new List<CardData> { playerCard, opponentCard });
        }
    }

    private (GameResult gameResult, int cardsWon) ProcessWar(List<CardData> cardsInPlay)
    {
        // Simplified war: each player puts 3 cards face down, then draws 1 for comparison
        int warCards = Math.Min(4, Math.Min(_playerDeck.Count, _opponentDeck.Count));
        
        if (warCards == 0)
        {
            // Not enough cards for war, game ends
            return _playerDeck.Count > _opponentDeck.Count ? 
                (GameResult.PlayerWins, cardsInPlay.Count) : 
                (GameResult.OpponentWins, cardsInPlay.Count);
        }

        // Draw war cards
        for (int i = 0; i < warCards - 1; i++)
        {
            if (_playerDeck.Count > 0 && _opponentDeck.Count > 0)
            {
                cardsInPlay.Add(_playerDeck.Dequeue());
                cardsInPlay.Add(_opponentDeck.Dequeue());
            }
        }

        // Draw final comparison cards
        if (_playerDeck.Count > 0 && _opponentDeck.Count > 0)
        {
            var playerWarCard = _playerDeck.Dequeue();
            var opponentWarCard = _opponentDeck.Dequeue();
            cardsInPlay.Add(playerWarCard);
            cardsInPlay.Add(opponentWarCard);

            // Determine war winner
            if (playerWarCard.Value > opponentWarCard.Value)
            {
                foreach (var card in cardsInPlay)
                    _playerDeck.Enqueue(card);
                return (GameResult.PlayerWins, cardsInPlay.Count);
            }
            else if (opponentWarCard.Value > playerWarCard.Value)
            {
                foreach (var card in cardsInPlay)
                    _opponentDeck.Enqueue(card);
                return (GameResult.OpponentWins, cardsInPlay.Count);
            }
            else
            {
                // Another war - recursive call
                return ProcessWar(cardsInPlay);
            }
        }
        
        // Fallback if something goes wrong
        return (GameResult.War, cardsInPlay.Count);
    }

    private GameRoundResult CreateGameEndResult()
    {
        bool playerWon = _playerDeck.Count > _opponentDeck.Count;
        return new GameRoundResult
        {
            PlayerCard = _gameState.LastPlayerCard,
            OpponentCard = _gameState.LastOpponentCard,
            Result = playerWon ? GameResult.PlayerWins : GameResult.OpponentWins,
            IsGameEnded = true,
            CardsWon = 0
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