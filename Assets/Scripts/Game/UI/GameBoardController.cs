using System;
using System.Collections.Generic;
using CardWar.Common;
using UnityEngine;
using CardWar.Services;
using CardWar.Animation.Data;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace CardWar.Game.UI
{
    public class GameBoardController : MonoBehaviour, IGameBoardController
    {
        #region Serialized Fields
        
        [Header("Pool Container")]
        [SerializeField] private Transform _poolContainer;
        
        [Header("Deck Positions")]
        [SerializeField] private Transform _playerDeckPosition;
        [SerializeField] private Transform _opponentDeckPosition;
        
        [Header("Battle Positions")]
        [SerializeField] private Transform _playerBattlePosition;
        [SerializeField] private Transform _opponentBattlePosition;
        
        [Header("War Positions")]
        [SerializeField] private List<Transform> _playerWarPositions = new(4);
        [SerializeField] private List<Transform> _opponentWarPositions = new(4);
        
        [Header("Draw Button")]
        [SerializeField] private Button _drawButton;
        
        #endregion
        
        #region Events
        
        public event Action OnDrawButtonPressed;
        public event Action OnRoundAnimationComplete;
        
        #endregion
        
        #region Dependencies
        
        private IGameControllerService _gameController;
        private IAssetService _assetService;
        private AnimationDataBundle _animationDataBundle;
        
        private CardAnimationHelper _animationHelper;
        private CardPositionManager _positionManager;
        private CardPoolManager _poolManager;
        private CardStateManager _stateManager;
        
        #endregion
        
        #region State
        
        private bool _isInitialized;
        
        #endregion
        
        #region Initialization
        
        public void Initialize(AnimationDataBundle animationDataBundle)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[GameBoardController] Already initialized");
                return;
            }
            
            _animationDataBundle = animationDataBundle;
            
            SetupComponents();
            InitializeHelpers();
            
            _isInitialized = true;
            Debug.Log("[GameBoardController] Initialized");
        }
        
        private void SetupComponents()
        {
            _gameController = ServiceLocator.Instance.Get<IGameControllerService>();
            _assetService = ServiceLocator.Instance.Get<IAssetService>();
            
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
        
        private void InitializeHelpers()
        {
            _positionManager = new CardPositionManager(
                _playerDeckPosition,
                _opponentDeckPosition,
                _playerBattlePosition,
                _opponentBattlePosition,
                _playerWarPositions,
                _opponentWarPositions
            );
            
            _animationHelper = new CardAnimationHelper(_animationDataBundle, _positionManager);
            _poolManager = new CardPoolManager(_poolContainer, _assetService);
            _stateManager = new CardStateManager(_poolManager);
        }
        
        public void SetupCardPool(int initialSize, int maxSize, bool prewarm)
        {
            _poolManager?.InitializePool(initialSize, maxSize, prewarm).Forget();
        }
        
        #endregion
        
        #region Battle Animations
        
        public async UniTask DrawBattleCards(RoundData roundData)
        {
            if (roundData == null)
            {
                Debug.LogError("[GameBoardController] Round data is null");
                return;
            }
            
            Debug.Log($"[GameBoardController] Drawing battle cards - Round {roundData.RoundNumber}");
            
            _stateManager.ClearBattleCards();
            
            var playerCard = _poolManager.SpawnCard(
                roundData.PlayerCard, 
                _positionManager.PlayerDeckPosition
            );
            
            var opponentCard = _poolManager.SpawnCard(
                roundData.OpponentCard, 
                _positionManager.OpponentDeckPosition
            );
            
            if (playerCard == null || opponentCard == null)
            {
                Debug.LogError("[GameBoardController] Failed to spawn battle cards");
                return;
            }
            
            _stateManager.SetBattleCards(playerCard, opponentCard);
            
            await _animationHelper.AnimateBattleCardDraw(playerCard, opponentCard);
        }
        
        public async UniTask FlipBattleCards()
        {
            Debug.Log($"[GameBoardController] Flipping battle cards");
            
            await _animationHelper.AnimateBattleCardFlip(
                _stateManager.PlayerBattleCard,
                _stateManager.OpponentBattleCard
            );
        }
        
        public async UniTask HighlightWinner(RoundResult result)
        {
            Debug.Log($"[GameBoardController] Highlighting winner: {result}");
            
            var winnerCard = _stateManager.GetWinnerCard(result == RoundResult.PlayerWins);
            await _animationHelper.HighlightWinner(winnerCard);
        }
        
        public async UniTask CollectBattleCards(RoundResult result)
        {
            Debug.Log($"[GameBoardController] Collecting battle cards - Winner: {result}");
            
            var targetPosition = _positionManager.GetCollectionTargetPosition(result);
            var cardsToCollect = _stateManager.GetCardsForCollection(false);
            
            await _animationHelper.CollectCardsToPosition(cardsToCollect, targetPosition);
            
            _stateManager.ReturnAllCardsToPool();
            OnRoundAnimationComplete?.Invoke();
        }
        
        #endregion
        
        #region War Animations
        
        public async UniTask PlaceWarCards(RoundData warData)
        {
            if (warData == null)
            {
                Debug.LogError("[GameBoardController] War data is null");
                return;
            }
            
            var actualWarCards = Math.Min(
                warData.PlayerWarCards.Count, 
                warData.OpponentWarCards.Count
            );
            
            Debug.Log($"[GameBoardController] Placing {actualWarCards} cards for war depth {warData.WarDepth}");
            
            if (warData.WarDepth == 1 && !warData.HasChainedWar)
            {
                _stateManager.AddExistingBattleCardsToWar();
            }
            
            var playerWarCards = new List<CardView>();
            var opponentWarCards = new List<CardView>();
            var playerPositions = new List<Vector3>();
            var opponentPositions = new List<Vector3>();
            
            for (var i = 0; i < actualWarCards; i++)
            {
                var playerCard = _poolManager.SpawnCard(
                    warData.PlayerWarCards[i],
                    _positionManager.PlayerDeckPosition
                );
                
                var opponentCard = _poolManager.SpawnCard(
                    warData.OpponentWarCards[i],
                    _positionManager.OpponentDeckPosition
                );
                
                if (playerCard == null || opponentCard == null)
                {
                    Debug.LogError("[GameBoardController] Failed to spawn war cards");
                    continue;
                }
                
                var isFaceDown = i < actualWarCards - 1;
                playerCard.SetFaceUp(!isFaceDown);
                opponentCard.SetFaceUp(!isFaceDown);
                
                _stateManager.AddWarCards(playerCard, opponentCard);
                playerWarCards.Add(playerCard);
                opponentWarCards.Add(opponentCard);
                
                var stackOffset = _positionManager.CalculateWarStackOffset(
                    warData.WarDepth, 
                    i, 
                    _animationDataBundle.War.CardSpacing
                );
                
                playerPositions.Add(_positionManager.GetWarPosition(true, i, stackOffset));
                opponentPositions.Add(_positionManager.GetWarPosition(false, i, stackOffset));
                
                if (i == actualWarCards - 1)
                {
                    _stateManager.UpdateBattleCards(playerCard, opponentCard);
                }
            }

            await _animationHelper.AnimateWarCardPlacement(playerWarCards, opponentWarCards, warData.WarDepth);
        }
        
        public async UniTask RevealWarCards()
        {
            Debug.Log($"[GameBoardController] Revealing war cards");
            
            var cardsToReveal = new List<CardView>();
            
            if (_stateManager.PlayerBattleCard != null && !_stateManager.PlayerBattleCard.IsFaceUp)
            {
                cardsToReveal.Add(_stateManager.PlayerBattleCard);
            }
            
            if (_stateManager.OpponentBattleCard != null && !_stateManager.OpponentBattleCard.IsFaceUp)
            {
                cardsToReveal.Add(_stateManager.OpponentBattleCard);
            }
            
            foreach (var card in _stateManager.WarCards)
            {
                if (card != null && !card.IsFaceUp)
                {
                    cardsToReveal.Add(card);
                }
            }
            
            await _animationHelper.FlipCards(cardsToReveal, true, 
                _animationDataBundle.War.RevealAnimation.Duration, 0);
        }
        
        public async UniTask RevealAllWarCards()
        {
            Debug.Log($"[GameBoardController] Revealing all war cards sequentially");
            
            var warCards = new List<CardView>(_stateManager.WarCards);
            await _animationHelper.RevealWarCardsSequentially(warCards);
            await UniTask.Delay(500);
        }
        
        public async UniTask CollectWarCards(RoundResult result)
        {
            Debug.Log($"[GameBoardController] Collecting war cards - Winner: {result}");
            
            var targetPosition = _positionManager.GetCollectionTargetPosition(result);
            var cardsToCollect = _stateManager.GetCardsForCollection(true);
            
            await _animationHelper.CollectCardsToPosition(cardsToCollect, targetPosition, true);
            
            _stateManager.ReturnAllCardsToPool();
            OnRoundAnimationComplete?.Invoke();
        }
        
        public async UniTask ConcealAllCards()
        {
            Debug.Log($"[GameBoardController] Concealing all cards");
            
            var faceUpCards = _stateManager.GetAllFaceUpCards();
            await _animationHelper.ConcealCards(faceUpCards);
        }
        
        public async UniTask ReturnWarCardsToBothPlayers()
        {
            Debug.Log($"[GameBoardController] Returning war cards to both players");
            
            await ConcealAllCards();
            await UniTask.Delay(300);
            
            var activeCards = _stateManager.GetAllActiveCards();
            await _animationHelper.ReturnWarCardsToBothDecks(activeCards);
            
            await ShowDeckShuffleAnimation();
            
            _stateManager.ClearAllCards();
        }
        
        #endregion
        
        #region Utility Animations
        
        public async UniTask ShowInitialDeckSetup()
        {
            Debug.Log($"[GameBoardController] Showing initial deck setup");
            
            var tasks = new List<UniTask>
            {
                _animationHelper.ShowShuffleAnimation(_positionManager.PlayerDeckTransform),
                _animationHelper.ShowShuffleAnimation(_positionManager.OpponentDeckTransform)
            };
            
            await UniTask.WhenAll(tasks);
        }
        
        private async UniTask ShowDeckShuffleAnimation()
        {
            var tasks = new List<UniTask>
            {
                _animationHelper.ShowShuffleAnimation(_positionManager.PlayerDeckTransform),
                _animationHelper.ShowShuffleAnimation(_positionManager.OpponentDeckTransform)
            };
            
            await UniTask.WhenAll(tasks);
        }
        
        #endregion
        
        #region Pause/Resume
        
        public void PauseAnimationsWithTransition()
        {
            Debug.Log($"[GameBoardController] Pausing animations");
            
            _animationHelper.PauseAllAnimations();
            
            if (_drawButton != null)
            {
                _drawButton.interactable = false;
            }
        }
        
        public void ResumeAnimationsWithTransition()
        {
            Debug.Log($"[GameBoardController] Resuming animations");
            
            _animationHelper.ResumeAllAnimations();
            
            if (_drawButton != null)
            {
                _drawButton.interactable = true;
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleGamePaused()
        {
            PauseAnimationsWithTransition();
        }
        
        private void HandleGameResumed()
        {
            ResumeAnimationsWithTransition();
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            if (_gameController != null)
            {
                _gameController.GamePausedEvent -= HandleGamePaused;
                _gameController.GameResumedEvent -= HandleGameResumed;
            }
            
            if (_drawButton != null)
            {
                _drawButton.onClick.RemoveAllListeners();
            }
            
            OnDrawButtonPressed = null;
            OnRoundAnimationComplete = null;
            
            _animationHelper?.KillAllAnimations();
            _poolManager?.Cleanup();
            _stateManager?.Cleanup();
            _positionManager?.Cleanup();
            
            _isInitialized = false;
            
            Debug.Log("[GameBoardController] Cleanup complete");
        }
        
        #endregion
    }
}