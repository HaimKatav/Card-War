using System.Collections.Generic;
using UnityEngine;

namespace CardWar.Game.UI
{
    public class CardPool : MonoBehaviour
    {
        private CardView _cardPrefab;
        private Queue<CardView> _availableCards;
        private List<CardView> _activeCards;
        private Transform _poolContainer;

        public void Initialize(CardView cardPrefab, int initialSize = 10)
        {
            _cardPrefab = cardPrefab;
            _availableCards = new Queue<CardView>();
            _activeCards = new List<CardView>();
            
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewCard();
            }
            
            Debug.Log($"[CardPool] Initialized with {initialSize} cards");
        }

        private CardView CreateNewCard()
        {
            var cardView = Instantiate(_cardPrefab, _poolContainer);
           
            cardView.gameObject.SetActive(false);
            _availableCards.Enqueue(cardView);
            
            return cardView;
        }


        public CardView GetCard()
        {
            CardView card;
            
            if (_availableCards.Count > 0)
            {
                card = _availableCards.Dequeue();
            }
            else
            {
                card = Instantiate(_cardPrefab, _poolContainer);
                Debug.Log("[CardPool] Created new card - pool expanded");
            }
            
            card.transform.SetParent(transform.parent);
            card.gameObject.SetActive(true);
            card.ResetCard();
            _activeCards.Add(card);
            
            return card;
        }

        public void ReturnCard(CardView card)
        {
            if (card == null) return;
            
            if (_activeCards.Contains(card))
            {
                _activeCards.Remove(card);
            }
            
            card.ResetCard();
            card.gameObject.SetActive(false);
            card.transform.SetParent(_poolContainer);
            _availableCards.Enqueue(card);
        }

        public void ReturnAllCards()
        {
            while (_activeCards.Count > 0)
            {
                ReturnCard(_activeCards[0]);
            }
        }

        private void OnDestroy()
        {
            ReturnAllCards();
        }
    }
}