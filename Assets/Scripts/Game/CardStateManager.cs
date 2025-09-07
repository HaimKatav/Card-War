using System.Collections.Generic;
using CardWar.Game.UI;
using UnityEngine;

namespace CardWar.Game.Helpers
{
    public class CardStateManager
    {
        private CardView _playerBattleCard;
        private CardView _opponentBattleCard;
        private readonly List<CardView> _warCards = new();
        private readonly CardPoolManager _poolManager;
        
        public CardView PlayerBattleCard => _playerBattleCard;
        public CardView OpponentBattleCard => _opponentBattleCard;
        public List<CardView> WarCards => _warCards;
        
        public CardStateManager(CardPoolManager poolManager)
        {
            _poolManager = poolManager;
        }
        
        public void SetBattleCards(CardView playerCard, CardView opponentCard)
        {
            _playerBattleCard = playerCard;
            _opponentBattleCard = opponentCard;
        }
        
        public void AddWarCard(CardView card)
        {
            if (card != null && !_warCards.Contains(card))
            {
                _warCards.Add(card);
            }
        }
        
        public void AddWarCards(params CardView[] cards)
        {
            foreach (var card in cards)
            {
                AddWarCard(card);
            }
        }
        
        public void ClearBattleCards()
        {
            ReturnCard(_playerBattleCard);
            ReturnCard(_opponentBattleCard);
            _playerBattleCard = null;
            _opponentBattleCard = null;
        }
        
        public void ClearWarCards()
        {
            foreach (var card in _warCards)
            {
                ReturnCard(card);
            }
            _warCards.Clear();
        }
        
        public void ClearAllCards()
        {
            ClearBattleCards();
            ClearWarCards();
        }
        
        public List<CardView> GetAllActiveCards()
        {
            var cards = new List<CardView>();
            
            if (_playerBattleCard != null)
                cards.Add(_playerBattleCard);
            
            if (_opponentBattleCard != null)
                cards.Add(_opponentBattleCard);
            
            cards.AddRange(_warCards);
            
            return cards;
        }
        
        public List<CardView> GetAllFaceUpCards()
        {
            var cards = new List<CardView>();
            
            foreach (var card in GetAllActiveCards())
            {
                if (card != null && card.IsFaceUp)
                {
                    cards.Add(card);
                }
            }
            
            return cards;
        }
        
        public bool HasActiveCards()
        {
            return _playerBattleCard != null || 
                   _opponentBattleCard != null || 
                   _warCards.Count > 0;
        }
        
        private void ReturnCard(CardView card)
        {
            _poolManager?.ReturnCard(card);
        }
        
        public void Cleanup()
        {
            ClearAllCards();
        }
    }
}