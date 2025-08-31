using System;
using System.Collections.Generic;
using CardWar.Common;
using UnityEngine;
using CardWar.Game.Logic;
using CardWar.Services;
using CardWar.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.UI;

namespace CardWar.Game.UI
{
    public class GameBoardController : MonoBehaviour
    {
        [Header("Pool Container")]
        [SerializeField] private Transform _poolContainer;
        
        [Header("Deck Positions")]
        [SerializeField] private Transform _playerDeckPosition;
        [SerializeField] private Transform _opponentDeckPosition;
        
        [Header("Battle Positions")]
        [SerializeField] private Transform _playerBattlePosition;
        [SerializeField] private Transform _opponentBattlePosition;
        
        [Header("Draw Button")]
        [SerializeField] private Button _drawButton;
        
        [Header("War Positions")]
        [SerializeField] private Transform[] _playerWarPositions = new Transform[4];
        [SerializeField] private Transform[] _opponentWarPositions = new Transform[4];
        
        [Header("Animation Settings")]
        [SerializeField] private float _cardMoveSpeed = 0.5f;
        [SerializeField] private float _cardFlipDelay = 0.3f;
        [SerializeField] private float _roundEndDelay = 1f;
        
        public event Action OnDrawButtonPressed;
        public event Action OnRoundAnimationComplete;
        
        private IGameControllerService _gameController;
        private IAssetService _assetService;
        
        private GenericPool<CardView> _cardPool;
        private CardView _playerBattleCard;
        private CardView _opponentBattleCard;
        private List<CardView> _warCards = new();
        
        private bool _isPaused;
        
        #region Initialization

        public void Initialize()
        {
            _gameController = ServiceLocator.Instance.Get<IGameControllerService>();
            _assetService = ServiceLocator.Instance.Get<IAssetService>();
            
            SetupCardPool().Forget();
            SubscribeToEvents();
            
            Debug.Log("[GameBoardController] Initialized");
        }

        private async UniTask SetupCardPool()
        {
            if (_poolContainer == null)
                _poolContainer = transform;
            
            var cardPrefab = await _assetService.LoadAssetAsync<CardView>(GameSettings.CARD_PREFAB_ASSET_PATH);
    
            _cardPool = new GenericPool<CardView>(cardPrefab, _poolContainer, 30);
        }

        private void SubscribeToEvents()
        {
            if (_gameController != null)
            {
                _gameController.GamePausedEvent += HandleGamePaused;
                _gameController.GameResumedEvent += HandleGameResumed;
            }

            if (_drawButton != null)
            {
                _drawButton.onClick.AddListener(() => OnDrawButtonPressed?.Invoke());
            }
        }

        #endregion

        #region Public Methods

        public async UniTask PlayRound(RoundData roundData)
        {
            if (roundData == null)
            {
                Debug.LogError("[GameBoardController] RoundData is null");
                return;
            }
            
            Debug.Log($"[GameBoardController] Playing round - IsWar: {roundData.IsWar}");
            
            ClearBattleCards();
            
            _playerBattleCard = SpawnCard(roundData.PlayerCard, _playerDeckPosition.position);
            _opponentBattleCard = SpawnCard(roundData.OpponentCard, _opponentDeckPosition.position);
            
            await MoveCardsToCenter();
            
            if (_isPaused) await WaitWhilePaused();
            
            await FlipBattleCards();
            
            if (_isPaused) await WaitWhilePaused();
            
            if (!roundData.IsWar)
            {
                await UniTask.Delay((int)(_roundEndDelay * 1000));
                
                if (_isPaused) await WaitWhilePaused();
                
                await CollectCards(roundData.Result);
            }
            
            // Always signal animation complete, even for war rounds
            OnRoundAnimationComplete?.Invoke();
        }

        public async UniTask PlayWarSequence(RoundData warRound)
        {
            if (warRound == null)
            {
                Debug.LogError("[GameBoardController] War round data is null");
                return;
            }
            
            Debug.Log($"[GameBoardController] Playing war sequence");
            
            await DisplayWarCards(warRound);
            
            if (_isPaused) await WaitWhilePaused();
            
            await UniTask.Delay((int)(_roundEndDelay * 1000));
            
            if (_isPaused) await WaitWhilePaused();
            
            if (!warRound.HasChainedWar)
            {
                await CollectAllCards(warRound.Result);
                ClearAllCards();
            }
            
            // Always signal completion, even for chained wars
            OnRoundAnimationComplete?.Invoke();
        }

        public void PauseAnimations()
        {
            _isPaused = true;
            DOTween.PauseAll();
            if (_drawButton != null)
                _drawButton.interactable = false;
        }

        public void ResumeAnimations()
        {
            _isPaused = false;
            DOTween.PlayAll();
            if (_drawButton != null)
                _drawButton.interactable = true;
        }

        #endregion

        #region Card Management

        private CardView SpawnCard(CardData cardData, Vector3 position)
        {
            var card = _cardPool.Get();
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
                var sprite = _assetService.GetCardSprite(cardData.CardKey);
                if (sprite != null)
                {
                    card.SetCardSprite(sprite);
                }
                
                var backSprite = _assetService.GetCardBackSprite();
                if (backSprite != null)
                {
                    card.SetBackSprite(backSprite);
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
            
            var animationTasks = new List<UniTask>();
            
            if (warRound.PlayerWarCards != null)
            {
                for (var i = 0; i < warRound.PlayerWarCards.Count && i < _playerWarPositions.Length; i++)
                {
                    if (_playerWarPositions[i] != null)
                    {
                        var warCard = SpawnCard(warRound.PlayerWarCards[i], _playerDeckPosition.position);
                        _warCards.Add(warCard);
                        
                        var targetPosition = _playerWarPositions[i].position;
                        animationTasks.Add(warCard.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
                        
                        var shouldFlip = (i == warRound.PlayerWarCards.Count - 1);
                        if (shouldFlip)
                        {
                            UniTask.Create(async () =>
                            {
                                await UniTask.Delay((int)(_cardMoveSpeed * 1000));
                                warCard.FlipCard(true);
                            }).Forget();
                        }
                    }
                }
            }
            
            if (warRound.OpponentWarCards != null)
            {
                for (var i = 0; i < warRound.OpponentWarCards.Count && i < _opponentWarPositions.Length; i++)
                {
                    if (_opponentWarPositions[i] != null)
                    {
                        var warCard = SpawnCard(warRound.OpponentWarCards[i], _opponentDeckPosition.position);
                        _warCards.Add(warCard);
                        
                        var targetPosition = _opponentWarPositions[i].position;
                        animationTasks.Add(warCard.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
                        
                        var shouldFlip = (i == warRound.OpponentWarCards.Count - 1);
                        if (shouldFlip)
                        {
                            UniTask.Create(async () =>
                            {
                                await UniTask.Delay((int)(_cardMoveSpeed * 1000));
                                warCard.FlipCard(true);
                            }).Forget();
                        }
                    }
                }
            }
            
            await UniTask.WhenAll(animationTasks);
            await UniTask.Delay(500);
        }

        private async UniTask CollectCards(RoundResult result)
        {
            var targetPosition = result == RoundResult.PlayerWins ? 
                _playerDeckPosition.position : _opponentDeckPosition.position;
            
            var tasks = new List<UniTask>();
            
            if (_playerBattleCard != null)
            {
                tasks.Add(_playerBattleCard.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
            }
            
            if (_opponentBattleCard != null)
            {
                tasks.Add(_opponentBattleCard.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
            }
            
            await UniTask.WhenAll(tasks);
            
            ClearBattleCards();
        }

        private async UniTask CollectAllCards(RoundResult result)
        {
            var targetPosition = result == RoundResult.PlayerWins ? 
                _playerDeckPosition.position : _opponentDeckPosition.position;
            
            var tasks = new List<UniTask>();
            
            foreach (var card in _warCards)
            {
                if (card != null)
                {
                    tasks.Add(card.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
                }
            }
            
            if (_playerBattleCard != null)
            {
                tasks.Add(_playerBattleCard.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
            }
            
            if (_opponentBattleCard != null)
            {
                tasks.Add(_opponentBattleCard.transform.DOMove(targetPosition, _cardMoveSpeed).AsyncWaitForCompletion().AsUniTask());
            }
            
            await UniTask.WhenAll(tasks);
        }

        private async UniTask WaitWhilePaused()
        {
            while (_isPaused)
            {
                await UniTask.Yield();
            }
        }

        private void ClearBattleCards()
        {
            if (_playerBattleCard != null)
            {
                _cardPool.Return(_playerBattleCard);
                _playerBattleCard = null;
            }
            
            if (_opponentBattleCard != null)
            {
                _cardPool.Return(_opponentBattleCard);
                _opponentBattleCard = null;
            }
        }

        private void ClearWarCards()
        {
            foreach (var card in _warCards)
            {
                if (card != null)
                {
                    _cardPool.Return(card);
                }
            }
            _warCards.Clear();
        }

        private void ClearAllCards()
        {
            ClearBattleCards();
            ClearWarCards();
        }

        #endregion

        #region Event Handlers

        private void HandleGamePaused()
        {
            PauseAnimations();
        }

        private void HandleGameResumed()
        {
            ResumeAnimations();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PauseAnimations();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _gameController != null)
            {
                PauseAnimations();
            }
        }

        #endregion

        #region Cleanup

        private void UnsubscribeFromEvents()
        {
            if (_gameController != null)
            {
                _gameController.GamePausedEvent -= HandleGamePaused;
                _gameController.GameResumedEvent -= HandleGameResumed;
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            
            OnDrawButtonPressed = null;
            OnRoundAnimationComplete = null;
            
            DOTween.KillAll();
            _cardPool?.ReturnAll();
            _warCards?.Clear();
        }

        #endregion
    }
}