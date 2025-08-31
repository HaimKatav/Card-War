using System;
using CardWar.Common;
using UnityEngine;
using UnityEngine.UI;
using CardWar.Game.Logic;
using DG.Tweening;

namespace CardWar.Game.UI
{
    public class CardView : MonoBehaviour
    {
        [Header("Card Images")]
        [SerializeField] private Image _cardFront;
        [SerializeField] private Image _cardBack;
        
        private CardData _cardData;
        private bool _isFaceUp = false;
        
        #region Public Methods

        private void OnDisable()
        {
            _isFaceUp = false;
            ShowCardSide(false);
        }

        private void OnEnable()
        {
            _isFaceUp = false;
            ShowCardSide(false);
        }

        public void SetCardData(CardData cardData)
        {
            _cardData = cardData;
        }

        public void SetCardSprite(Sprite frontSprite)
        {
            _cardFront.sprite = frontSprite;
        }

        public void SetBackSprite(Sprite backSprite)
        {
            _cardBack.sprite = backSprite;
        }
        
        public void FlipCard(bool faceUp, float duration = 0.3f)
        {
            _isFaceUp = faceUp;
            
            if (duration <= 0)
            {
                ShowCardSide(_isFaceUp);
                return;
            }
            
            transform.DORotateQuaternion(Quaternion.Euler(0, 90, 0), duration * 0.5f)
                .OnComplete(() =>
                {
                    ShowCardSide(_isFaceUp);
                    transform.DORotateQuaternion(Quaternion.identity, duration * 0.5f);
                });
        }
        
        public void ResetCard()
        {
            _cardData = null;
            _isFaceUp = false;
            ShowCardSide(false);
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        
        public CardData GetCardData()
        {
            return _cardData;
        }
        
        public bool IsFaceUp()
        {
            return _isFaceUp;
        }
        
        #endregion
        
        #region Private Methods
        
        private void ShowCardSide(bool showFront)
        {
            if (_cardFront != null)
                _cardFront.gameObject.SetActive(showFront);
                
            if (_cardBack != null)
                _cardBack.gameObject.SetActive(!showFront);
        }
        
        #endregion
    }
}