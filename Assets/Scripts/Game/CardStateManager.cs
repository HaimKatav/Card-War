using System.Collections.Generic;
using System.Linq;
using CardWar.Game.UI;
using UnityEngine;

namespace CardWar.Game.UI
{
    public class CardStateManager
    {
        private readonly CardPoolManager _poolManager;
        
        private CardView _playerBattleCard;
        private CardView _opponentBattleCard;
        private readonly List<CardView> _warCards = new();
        private readonly List<CardView> _allActiveCards = new();
        
        public CardView PlayerBattleCard => _playerBattleCard;
        public CardView OpponentBattleCard => _opponentBattleCard;
        public IReadOnlyList<CardView> WarCards => _warCards;
        public IReadOnlyList<CardView> AllActiveCards => _allActiveCards;
        
        public CardStateManager(CardPoolManager poolManager)
        {
            _poolManager = poolManager;
        }
        
        #region Battle Card Management
        
        public void SetBattleCards(CardView playerCard, CardView opponentCard)
        {
            ClearBattleCards();
            
            _playerBattleCard = playerCard;
            _opponentBattleCard = opponentCard;
            
            RegisterActiveCard(playerCard);
            RegisterActiveCard(opponentCard);
            
            Debug.Log($"[CardStateManager] Battle cards set - Player: {playerCard?.GetCardData()?.CardKey}, Opponent: {opponentCard?.GetCardData()?.CardKey}");
        }
        
        public void UpdateBattleCards(CardView playerCard, CardView opponentCard)
        {
            _playerBattleCard = playerCard;
            _opponentBattleCard = opponentCard;
            
            Debug.Log($"[CardStateManager] Battle cards updated for war");
        }
        
        public void ClearBattleCards()
        {
            if (_playerBattleCard != null)
            {
                UnregisterActiveCard(_playerBattleCard);
                _poolManager.ReturnCard(_playerBattleCard);
                _playerBattleCard = null;
            }
            
            if (_opponentBattleCard != null)
            {
                UnregisterActiveCard(_opponentBattleCard);
                _poolManager.ReturnCard(_opponentBattleCard);
                _opponentBattleCard = null;
            }
        }
        
        #endregion
        
        #region War Card Management
        
        public void AddWarCard(CardView card)
        {
            if (card != null && !_warCards.Contains(card))
            {
                _warCards.Add(card);
                RegisterActiveCard(card);
                Debug.Log($"[CardStateManager] War card added: {card.GetCardData()?.CardKey}");
            }
        }
        
        public void AddWarCards(params CardView[] cards)
        {
            foreach (var card in cards)
            {
                AddWarCard(card);
            }
        }
        
        public void AddExistingBattleCardsToWar()
        {
            if (_playerBattleCard != null && !_warCards.Contains(_playerBattleCard))
            {
                _warCards.Add(_playerBattleCard);
                Debug.Log($"[CardStateManager] Added player battle card to war cards");
            }
            
            if (_opponentBattleCard != null && !_warCards.Contains(_opponentBattleCard))
            {
                _warCards.Add(_opponentBattleCard);
                Debug.Log($"[CardStateManager] Added opponent battle card to war cards");
            }
        }
        
        public void ClearWarCards()
        {
            foreach (var card in _warCards)
            {
                if (card != null)
                {
                    UnregisterActiveCard(card);
                    _poolManager.ReturnCard(card);
                }
            }
            _warCards.Clear();
            Debug.Log($"[CardStateManager] War cards cleared");
        }
        
        #endregion
        
        #region Active Card Tracking
        
        private void RegisterActiveCard(CardView card)
        {
            if (card != null && !_allActiveCards.Contains(card))
            {
                _allActiveCards.Add(card);
            }
        }
        
        private void UnregisterActiveCard(CardView card)
        {
            if (card != null)
            {
                _allActiveCards.Remove(card);
            }
        }
        
        public List<CardView> GetAllActiveCards()
        {
            return new List<CardView>(_allActiveCards);
        }
        
        public List<CardView> GetAllFaceUpCards()
        {
            return _allActiveCards.Where(card => card != null && card.IsFaceUp).ToList();
        }
        
        public List<CardView> GetAllFaceDownCards()
        {
            return _allActiveCards.Where(card => card != null && !card.IsFaceUp).ToList();
        }
        
        public List<CardView> GetCardsForCollection(bool includeWarCards = true)
        {
            var cards = new List<CardView>();
            
            if (includeWarCards)
            {
                for (var i = _warCards.Count - 1; i >= 0; i--)
                {
                    if (_warCards[i] != null)
                    {
                        cards.Add(_warCards[i]);
                    }
                }
            }
            
            if (_playerBattleCard != null && !_warCards.Contains(_playerBattleCard))
            {
                cards.Add(_playerBattleCard);
            }
            
            if (_opponentBattleCard != null && !_warCards.Contains(_opponentBattleCard))
            {
                cards.Add(_opponentBattleCard);
            }
            
            return cards;
        }
        
        #endregion
        
        #region State Queries
        
        public bool HasBattleCards()
        {
            return _playerBattleCard != null || _opponentBattleCard != null;
        }
        
        public bool HasWarCards()
        {
            return _warCards.Count > 0;
        }
        
        public bool HasActiveCards()
        {
            return _allActiveCards.Count > 0;
        }
        
        public CardView GetWinnerCard(bool playerWon)
        {
            return playerWon ? _playerBattleCard : _opponentBattleCard;
        }
        
        public int GetWarCardCount()
        {
            return _warCards.Count;
        }
        
        public int GetActiveCardCount()
        {
            return _allActiveCards.Count;
        }
        
        #endregion
        
        #region Cleanup
        
        public void ClearAllCards()
        {
            ClearBattleCards();
            ClearWarCards();
            _allActiveCards.Clear();
            
            Debug.Log($"[CardStateManager] All cards cleared");
        }
        
        public void ReturnAllCardsToPool()
        {
            foreach (var card in _allActiveCards)
            {
                if (card != null)
                {
                    _poolManager.ReturnCard(card);
                }
            }
            
            _playerBattleCard = null;
            _opponentBattleCard = null;
            _warCards.Clear();
            _allActiveCards.Clear();
            
            Debug.Log($"[CardStateManager] All cards returned to pool");
        }
        
        public void Cleanup()
        {
            ReturnAllCardsToPool();
        }
        
        #endregion
    }
}