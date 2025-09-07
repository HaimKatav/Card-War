using System;
using System.Collections.Generic;
using CardWar.Common;
using UnityEngine;
using CardWar.Services;
using CardWar.Animation.Data;
using CardWar.Game.Helpers;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.UI;

namespace CardWar.Game.UI
{
    public class GameBoardController : MonoBehaviour, IGameBoardController
    {
        #region Serialized Fields

        [Header("Pool Container")] [SerializeField]
        private Transform _poolContainer;

        [Header("Deck Positions")] [SerializeField]
        private Transform _playerDeckPosition;

        [SerializeField] private Transform _opponentDeckPosition;

        [Header("Battle Positions")] [SerializeField]
        private Transform _playerBattlePosition;

        [SerializeField] private Transform _opponentBattlePosition;

        [Header("Draw Button")] [SerializeField]
        private Button _drawButton;

        [Header("War Positions")] [SerializeField]
        private List<Transform> _playerWarPositions = new(4);

        [SerializeField] private List<Transform> _opponentWarPositions = new(4);

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

        private bool _isPaused;
        private bool _isInitialized;
        private CardView _playerBattleCard;
        private CardView _opponentBattleCard;
        private List<CardView> _warCards = new();

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
            _animationHelper = new CardAnimationHelper(_animationDataBundle);

            _positionManager = new CardPositionManager(
                _playerDeckPosition,
                _opponentDeckPosition,
                _playerBattlePosition,
                _opponentBattlePosition,
                _playerWarPositions,
                _opponentWarPositions
            );

            _poolManager = new CardPoolManager(_poolContainer, _assetService);
            _stateManager = new CardStateManager(_poolManager);
        }

        public void SetupCardPool(int initialSize, int maxSize, bool prewarm)
        {
            _poolManager?.InitializePool(initialSize, maxSize, prewarm);
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

            var drawDuration = _animationDataBundle.Battle.DrawAnimation.Duration;
            var drawEase = _animationDataBundle.Battle.DrawAnimation.EasingCurve;

            Debug.Log($"[GameBoardController] Drawing battle cards - Round {roundData.RoundNumber}");

            _stateManager.ClearBattleCards();

            _playerBattleCard = _poolManager.SpawnCard(roundData.PlayerCard, _positionManager.PlayerDeckPosition);
            _opponentBattleCard = _poolManager.SpawnCard(roundData.OpponentCard, _positionManager.OpponentDeckPosition);

            if (_playerBattleCard == null || _opponentBattleCard == null)
            {
                Debug.LogError("[GameBoardController] Failed to spawn battle cards");
                return;
            }

            _stateManager.SetBattleCards(_playerBattleCard, _opponentBattleCard);

            var moveTasks = new List<UniTask>
            {
                _animationHelper.MoveCardToPosition(_playerBattleCard, _positionManager.PlayerBattlePosition,
                    drawDuration, drawEase),
                _animationHelper.MoveCardToPosition(_opponentBattleCard, _positionManager.OpponentBattlePosition,
                    drawDuration, drawEase)
            };

            await UniTask.WhenAll(moveTasks);
        }

        public async UniTask FlipBattleCards()
        {
            var flipDuration = _animationDataBundle.Battle.RevealAnimation.Duration;

            Debug.Log($"[GameBoardController] Flipping battle cards");

            var flipTasks = new List<UniTask>();

            if (_stateManager.PlayerBattleCard != null)
            {
                flipTasks.Add(_stateManager.PlayerBattleCard.FlipCard(true, flipDuration));
            }

            if (_stateManager.OpponentBattleCard != null)
            {
                flipTasks.Add(_stateManager.OpponentBattleCard.FlipCard(true, flipDuration));
            }

            if (flipTasks.Count > 0)
            {
                await UniTask.WhenAll(flipTasks);
            }
        }

        public async UniTask HighlightWinner(RoundResult result)
        {
            var scaleMultiplier = _animationDataBundle.WinnerHighlight.ScaleMultiplier;
            var tintColor = _animationDataBundle.WinnerHighlight.TintColor;

            Debug.Log($"[GameBoardController] Highlighting winner: {result}");

            var winnerCard = result == RoundResult.PlayerWins
                ? _stateManager.PlayerBattleCard
                : _stateManager.OpponentBattleCard;

            if (winnerCard != null)
            {
                await _animationHelper.HighlightCard(winnerCard, scaleMultiplier, tintColor);
            }
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

            var placeDuration = _animationDataBundle.War.PlaceCardsAnimation.Duration;
            var placeCurve = _animationDataBundle.War.PlaceCardsAnimation.EasingCurve;
            var cardSpacing = _animationDataBundle.War.CardSpacing;

            var actualWarCards = Math.Min(warData.PlayerWarCards.Count, warData.OpponentWarCards.Count);
            Debug.Log(
                $"[GameBoardController] Placing {actualWarCards} cards per player for war depth {warData.WarDepth}");

            if (warData.WarDepth == 1 && !warData.HasChainedWar)
            {
                // Add existing battle cards to war cards list if they exist
                if (_playerBattleCard != null)
                {
                    _stateManager.WarCards.Add(_playerBattleCard);
                }

                if (_opponentBattleCard != null)
                {
                    _stateManager.WarCards.Add(_opponentBattleCard);
                }
            }

            var sequence = DOTween.Sequence();

            // Calculate offset for consecutive wars - stack on top of existing cards
            var warStackOffset = warData.HasChainedWar
                ? Vector3.up * cardSpacing * 0.5f * ((_stateManager.WarCards.Count - 2) / 2) // -2 to account for battle cards
                : Vector3.zero;

            
            for (var i = 0; i < actualWarCards; i++)
            {
                var playerWarCard = _poolManager.SpawnCard(warData.PlayerWarCards[i], _playerDeckPosition.position);
                var opponentWarCard =
                    _poolManager.SpawnCard(warData.OpponentWarCards[i], _opponentDeckPosition.position);

                if (playerWarCard == null || opponentWarCard == null)
                {
                    Debug.LogError("[GameBoardController] Failed to spawn war cards");
                    continue;
                }

                var isFaceDown = i < actualWarCards - 1;
                playerWarCard.SetFaceUp(!isFaceDown);
                opponentWarCard.SetFaceUp(!isFaceDown);

                _stateManager.AddWarCards(playerWarCard, opponentWarCard);

                var playerTargetPos = _playerWarPositions[i].position + warStackOffset;
                var opponentTargetPos = _opponentWarPositions[i].position + warStackOffset;
                
                sequence.Insert(i * 0.1f,
                    playerWarCard.transform.DOMove(playerTargetPos, placeDuration).SetEase(placeCurve));
                sequence.Insert(i * 0.1f,
                    opponentWarCard.transform.DOMove(opponentTargetPos, placeDuration).SetEase(placeCurve));

                // Update battle cards to the last cards played
                if (i == actualWarCards - 1)
                {
                    _playerBattleCard = playerWarCard;
                    _opponentBattleCard = opponentWarCard;
                }
            }

            await sequence.AsyncWaitForCompletion().AsUniTask();
        }

        public async UniTask RevealWarCards()
        {
            Debug.Log($"[GameBoardController] Revealing war battle cards");

            var config = _animationDataBundle.War;
            var revealTasks = new List<UniTask>();

            if (_stateManager.PlayerBattleCard != null && !_stateManager.PlayerBattleCard.IsFaceUp)
            {
                revealTasks.Add(_stateManager.PlayerBattleCard.FlipCard(true, config.RevealAnimation.Duration));
            }

            if (_stateManager.OpponentBattleCard != null && !_stateManager.OpponentBattleCard.IsFaceUp)
            {
                revealTasks.Add(_stateManager.OpponentBattleCard.FlipCard(true, config.RevealAnimation.Duration));
            }

            foreach (var card in _stateManager.WarCards)
            {
                if (card != null && !card.IsFaceUp)
                    revealTasks.Add(card.FlipCard(true, config.RevealAnimation.Duration));
            }
            

            if (revealTasks.Count > 0)
            {
                await UniTask.WhenAll(revealTasks);
            }
        }

        public async UniTask RevealAllWarCards()
        {
            Debug.Log($"[GameBoardController] Revealing all face-down war cards from last to first");

            var config = _animationDataBundle.War;
            var sequence = DOTween.Sequence();
            var delay = 0f;

            for (var i = _stateManager.WarCards.Count - 1; i >= 0; i--)
            {
                var card = _stateManager.WarCards[i];
                if (card != null && !card.IsFaceUp)
                {
                    var capturedCard = card;
                    sequence.Insert(delay, DOTween.To(
                        () => 0f,
                        _ => { },
                        1f,
                        0.01f
                    ).OnComplete(() => capturedCard.FlipCard(true, config.RevealAnimation.Duration).Forget()));

                    delay += 0.1f;
                }
            }

            await sequence.AsyncWaitForCompletion().AsUniTask();
            await UniTask.Delay(500);
        }

        #region Card Collection Methods

        public async UniTask CollectAllCards(RoundResult result)
        {
            var collectionDuration = _animationDataBundle.Collection.Duration;
            var staggerDelay = _animationDataBundle.Collection.StaggerDelay;
            var easingCurve = _animationDataBundle.Collection.EasingCurve;

            Debug.Log($"[GameBoardController] Collecting all cards - Winner: {result}");

            var targetPosition = result == RoundResult.PlayerWins
                ? _playerDeckPosition.position
                : _opponentDeckPosition.position;

            var allCards = new List<CardView>();

            // Add war cards first (in reverse order - last war first)
            for (var i = _stateManager.WarCards.Count - 1; i >= 0; i--)
            {
                if (_stateManager.WarCards[i] != null)
                {
                    allCards.Add(_stateManager.WarCards[i]);
                }
            }

            if (_playerBattleCard != null && !_stateManager.WarCards.Contains(_playerBattleCard))
            {
                allCards.Add(_playerBattleCard);
            }

            if (_opponentBattleCard != null && !_stateManager.WarCards.Contains(_opponentBattleCard))
            {
                allCards.Add(_opponentBattleCard);
            }

            if (allCards.Count == 0)
            {
                Debug.LogWarning("[GameBoardController] No cards to collect");
                return;
            }
            
            await _animationHelper.MoveCardsToPosition(allCards, targetPosition, collectionDuration,staggerDelay, easingCurve);
            
            allCards.Clear();
            _stateManager.WarCards.Clear();
            _playerBattleCard = null;
            _opponentBattleCard = null;

            _poolManager.ReturnAllActiveCards();
            
            // Fire completion event for UI update
            OnRoundAnimationComplete?.Invoke();
        }

        public async UniTask CollectBattleCards(RoundResult result)
        {
            Debug.Log($"[GameBoardController] Collecting battle cards - Winner: {result}");

            // Use the unified collection method for consistency
            await CollectAllCards(result);
        }

        public async UniTask CollectWarCards(RoundResult result)
        {
            Debug.Log($"[GameBoardController] Collecting war cards - Winner: {result}");

            // Use the unified collection method
            await CollectAllCards(result);
        }

        #endregion Card Collection Methods


        public async UniTask ConcealAllCards()
        {
            Debug.Log($"[GameBoardController] Concealing all cards");

            var faceUpCards = _stateManager.GetAllFaceUpCards();
            await _animationHelper.ConcealCards(faceUpCards);
        }

        public async UniTask ReturnWarCardsToBothPlayers()
        {
            Debug.Log($"[GameBoardController] War ended in draw - Returning cards to both players");

            await ConcealAllCards();
            await UniTask.Delay(300);

            await _animationHelper.ReturnCardsToDecks(
                _stateManager.GetAllActiveCards(),
                _playerWarPositions,
                _opponentWarPositions,
                _positionManager.PlayerDeckPosition,
                _positionManager.OpponentDeckPosition
            );

            await ShowDeckShuffleAnimation();

            _stateManager.ClearAllCards();
        }

        #endregion

        #region Utility Animations

        public async UniTask ShowInitialDeckSetup()
        {
            Debug.Log($"[GameBoardController] Showing initial deck setup");

            await _animationHelper.ShowShuffleAnimation(_playerDeckPosition);
            await _animationHelper.ShowShuffleAnimation(_opponentDeckPosition);
        }

        private async UniTask ShowDeckShuffleAnimation()
        {
            Debug.Log($"[GameBoardController] Showing deck shuffle animation");

            var tasks = new List<UniTask>
            {
                _animationHelper.ShowShuffleAnimation(_playerDeckPosition),
                _animationHelper.ShowShuffleAnimation(_opponentDeckPosition)
            };

            await UniTask.WhenAll(tasks);
        }

        #endregion

        #region Pause/Resume

        public void PauseAnimationsWithTransition()
        {
            Debug.Log($"[GameBoardController] Pausing Animations");
            _isPaused = true;
            DOTween.PauseAll();
            if (_drawButton != null)
                _drawButton.interactable = false;
        }

        public void ResumeAnimationsWithTransition()
        {
            Debug.Log($"[GameBoardController] Resuming animations");
            _isPaused = false;
            DOTween.PlayAll();
            if (_drawButton != null)
                _drawButton.interactable = true;
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

            DOTween.KillAll();

            _poolManager?.Cleanup();
            _stateManager?.Cleanup();

            _isInitialized = false;
        }

        #endregion
    }
}