using System;
using System.Collections.Generic;
using CardWar.Common;
using UnityEngine;
using UnityEngine.UI;
using CardWar.Game.Logic;
using CardWar.Services;
using CardWar.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace CardWar.Game.UI
{
    public class TableViewController : MonoBehaviour
    {
        [Header("Card Pool")]
        [SerializeField] private CardPool _cardPool;
        
        [Header("Deck Positions")]
        [SerializeField] private Transform _playerDeckPosition;
        [SerializeField] private Transform _opponentDeckPosition;
        
        [Header("Battle Positions")]
        [SerializeField] private Transform _playerBattlePosition;
        [SerializeField] private Transform _opponentBattlePosition;
        
        [Header("War Positions")]
        [SerializeField] private Transform[] _playerWarPositions = new Transform[4];
        [SerializeField] private Transform[] _opponentWarPositions = new Transform[4];
        
        [Header("Animation Settings")]
        [SerializeField] private float _cardMoveSpeed = 0.5f;
        [SerializeField] private float _cardFlipDelay = 0.3f;
        [SerializeField] private float _roundEndDelay = 1f;
        
        private IGameControllerService _gameController;
        private IAssetService _assetService;
        
        private CardView _cardPrefab;
        private CardView _playerBattleCard;
        private CardView _opponentBattleCard;
        private List<CardView> _warCards = new List<CardView>();
        
        private bool _isPaused;

        #region Initialization

        public void Initialize(IGameControllerService gameController)
        {
            _gameController = gameController;
            _assetService = ServiceLocator.Instance.Get<IAssetService>();
            
            SetupCardPool();
            
            Debug.Log("[TableViewController] Initialized");
        }

        private async UniTask GetCardPrefab()
        {
            _cardPrefab = await _assetService.LoadAssetAsync<CardView>(GameSettings.CARD_SPRITE_ASSET_PATH);
        }

        private void SetupCardPool()
        {
            _cardPool.Initialize(_cardPrefab, 20);
        }

        public void SetupInitialState(int playerCards, int opponentCards)
        {
            Debug.Log($"[TableViewController] Initial state - Player: {playerCards}, Opponent: {opponentCards}");
        }

        #endregion

        #region Round Playing

        public async UniTask PlayRound(RoundData roundData)
        {
            if (roundData == null)
            {
                Debug.LogError("[TableViewController] RoundData is null");
                return;
            }
            
            Debug.Log($"[TableViewController] Playing round - IsWar: {roundData.IsWar}");
            
            ClearBattleCards();
            
            _playerBattleCard = SpawnCard(roundData.PlayerCard, _playerDeckPosition.position);
            _opponentBattleCard = SpawnCard(roundData.OpponentCard, _opponentDeckPosition.position);
            
            await MoveCardsToCenter();
            await FlipBattleCards();
            
            if (!roundData.IsWar)
            {
                await UniTask.Delay((int)(_roundEndDelay * 1000));
                await CollectCards(roundData.Result);
            }
        }

        public async UniTask PlayWarSequence(RoundData warRound)
        {
            if (warRound == null)
            {
                Debug.LogError("[TableViewController] War round data is null");
                return;
            }
            
            Debug.Log($"[TableViewController] Playing war sequence - Chained: {warRound.HasChainedWar}");
            
            await DisplayWarCards(warRound);
            await UniTask.Delay((int)(_roundEndDelay * 1000));
            
            if (!warRound.HasChainedWar)
            {
                await CollectAllCards(warRound.Result);
                ClearAllCards();
            }
        }

        #endregion

        #region Card Management

        private CardView SpawnCard(CardData cardData, Vector3 position)
        {
            var card = _cardPool.GetCard();
            card.transform.position = position;
            card.SetCardData(cardData);
            card.FlipCard(false, 0);
            
            LoadCardSprite(card, cardData);
            
            return card;
        }

        private void LoadCardSprite(CardView card, CardData cardData)
        {
            if (_assetService != null && cardData != null)
            {
                var sprite = _assetService.GetCardSprite(cardData.GetCardKey());
                if (sprite != null)
                {
                    card.SetCardSprite(sprite);
                }
            }
        }

        private async UniTask MoveCardsToCenter()
        {
            var tasks = new List<UniTask>();
            
            if (_playerBattleCard != null)
                tasks.Add(_playerBattleCard.transform.DOMove(_playerBattlePosition.position, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
                
            if (_opponentBattleCard != null)
                tasks.Add(_opponentBattleCard.transform.DOMove(_opponentBattlePosition.position, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
            
            await UniTask.WhenAll(tasks);
        }

        private async UniTask FlipBattleCards()
        {
            await UniTask.Delay((int)(_cardFlipDelay * 1000));
            
            _playerBattleCard?.FlipCard(true);
            _opponentBattleCard?.FlipCard(true);
            
            await UniTask.Delay(300);
        }

        private async UniTask DisplayWarCards(RoundData warRound)
        {
            ClearWarCards();
            
            for (int i = 0; i < warRound.PlayerWarCards.Count && i < 4; i++)
            {
                var position = GetWarPosition(_playerWarPositions, i, _playerBattlePosition);
                var card = SpawnCard(warRound.PlayerWarCards[i], position.position);
                _warCards.Add(card);
                
                bool isLastCard = (i == warRound.PlayerWarCards.Count - 1);
                if (isLastCard)
                {
                    card.FlipCard(true);
                }
            }
            
            for (int i = 0; i < warRound.OpponentWarCards.Count && i < 4; i++)
            {
                var position = GetWarPosition(_opponentWarPositions, i, _opponentBattlePosition);
                var card = SpawnCard(warRound.OpponentWarCards[i], position.position);
                _warCards.Add(card);
                
                bool isLastCard = (i == warRound.OpponentWarCards.Count - 1);
                if (isLastCard)
                {
                    card.FlipCard(true);
                }
            }
            
            await UniTask.Delay(500);
        }

        private Transform GetWarPosition(Transform[] positions, int index, Transform fallback)
        {
            if (positions != null && index < positions.Length && positions[index] != null)
                return positions[index];
            return fallback;
        }

        private async UniTask CollectCards(RoundResult result)
        {
            Vector3 targetPosition = result == RoundResult.PlayerWins ? 
                _playerDeckPosition.position : _opponentDeckPosition.position;
            
            var tasks = new List<UniTask>();
            
            if (_playerBattleCard != null)
                tasks.Add(_playerBattleCard.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
                
            if (_opponentBattleCard != null)
                tasks.Add(_opponentBattleCard.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
            
            await UniTask.WhenAll(tasks);
            
            ReturnBattleCards();
        }

        private async UniTask CollectAllCards(RoundResult result)
        {
            Vector3 targetPosition = result == RoundResult.PlayerWins ? 
                _playerDeckPosition.position : _opponentDeckPosition.position;
            
            var tasks = new List<UniTask>();
            
            if (_playerBattleCard != null)
                tasks.Add(_playerBattleCard.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
                
            if (_opponentBattleCard != null)
                tasks.Add(_opponentBattleCard.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
            
            foreach (var card in _warCards)
            {
                if (card != null)
                    tasks.Add(card.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
            }
            
            await UniTask.WhenAll(tasks);
        }

        private void ClearBattleCards()
        {
            if (_playerBattleCard != null)
            {
                _cardPool.ReturnCard(_playerBattleCard);
                _playerBattleCard = null;
            }
            
            if (_opponentBattleCard != null)
            {
                _cardPool.ReturnCard(_opponentBattleCard);
                _opponentBattleCard = null;
            }
        }

        private void ReturnBattleCards()
        {
            ClearBattleCards();
        }

        private void ClearWarCards()
        {
            foreach (var card in _warCards)
            {
                if (card != null)
                    _cardPool.ReturnCard(card);
            }
            _warCards.Clear();
        }

        private void ClearAllCards()
        {
            ClearBattleCards();
            ClearWarCards();
        }

        #endregion

        #region Pause Handling

        public void HandlePause()
        {
            _isPaused = true;
            DOTween.PauseAll();
            Debug.Log("[TableViewController] Paused");
        }

        public void HandleResume()
        {
            _isPaused = false;
            DOTween.PlayAll();
            Debug.Log("[TableViewController] Resumed");
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            DOTween.KillAll();
            ClearAllCards();
        }

        #endregion
    }
}