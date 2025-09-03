using System;
using System.Collections.Generic;
using CardWar.Common;
using UnityEngine;
using CardWar.Game.Logic;
using CardWar.Services;
using CardWar.Core;
using CardWar.Animation.Data;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.UI;

namespace CardWar.Game.UI
{
    public class GameBoardController : MonoBehaviour, IGameBoardController
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
        
        public event Action OnDrawButtonPressed;
        public event Action OnRoundAnimationComplete;
        
        private IGameControllerService _gameController;
        private IAssetService _assetService;
        private AnimationDataBundle _animationDataBundle;
        
        private GenericPool<CardView> _cardPool;
        private CardView _playerBattleCard;
        private CardView _opponentBattleCard;
        private readonly List<CardView> _warCards = new();
        private readonly List<CardView> _activeCards = new();
        
        private bool _isPaused;
        private bool _isInitialized;
        private int _poolInitialSize = 20;
        private int _poolMaxSize = 52;
        
        #region Initialization

        public void Initialize(AnimationDataBundle animationDataBundle)
        {
            if (_isInitialized) return;
            
            _gameController = ServiceLocator.Instance.Get<IGameControllerService>();
            _assetService = ServiceLocator.Instance.Get<IAssetService>();

            _animationDataBundle = animationDataBundle;
            
            InitializeCardPool().Forget();
            SubscribeToEvents();
            
            _isInitialized = true;
            Debug.Log("[GameBoardController] Initialized with animation configuration");
        }
        
        public void SetupCardPool(int initialSize, int maxSize, bool prewarm)
        {
            _poolInitialSize = initialSize;
            _poolMaxSize = maxSize;
            
            if (prewarm && _cardPool != null)
            {
                Debug.Log($"[GameBoardController] Pool configured - Initial: {initialSize}, Max: {maxSize}");
            }
        }

        private async UniTask InitializeCardPool()
        {
            if (_poolContainer == null)
                _poolContainer = transform;
            
            var cardPrefab = await _assetService.LoadAssetAsync<CardView>(GameSettings.CARD_PREFAB_ASSET_PATH);
            
            if (cardPrefab != null)
            {
                _cardPool = new GenericPool<CardView>(cardPrefab, _poolContainer, _poolInitialSize);
                Debug.Log($"[GameBoardController] Card pool initialized with {_poolInitialSize} cards");
            }
            else
            {
                Debug.LogError("[GameBoardController] Failed to load card prefab");
            }
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

        public async UniTask ShowInitialDeckSetup()
        {
            var fadeInDuration = _animationDataBundle.Transitions.FadeInDuration;
            
            Debug.Log($"[GameBoardController] Showing initial deck setup - Fade: {fadeInDuration}s");
            
            var playerDeckCard = SpawnCard(null, _playerDeckPosition.position, false);
            var opponentDeckCard = SpawnCard(null, _opponentDeckPosition.position, false);
            
            playerDeckCard.SetAlpha(0);
            opponentDeckCard.SetAlpha(0);
            
            var sequence = DOTween.Sequence();
            sequence.Append(playerDeckCard.FadeIn(fadeInDuration));
            sequence.Join(opponentDeckCard.FadeIn(fadeInDuration));
            
            await sequence.AsyncWaitForCompletion();
            
            ReturnCard(playerDeckCard);
            ReturnCard(opponentDeckCard);
        }

        #endregion Initialization
        

        #region Battle Animation Methods
        public async UniTask DrawBattleCards(RoundData roundData)
        {
            if (roundData == null)
            {
                Debug.LogError("[GameBoardController] RoundData is null");
                return;
            }

            var moveDuration = _animationDataBundle.Battle.DrawAnimation.Duration;
            var moveEase = _animationDataBundle.Battle.DrawAnimation.EasingCurve;
            
            Debug.Log($"[GameBoardController] Drawing battle cards - Duration: {moveDuration}s");
            
            ClearBattleCards();
            
            _playerBattleCard = SpawnCard(roundData.PlayerCard, _playerDeckPosition.position);
            _opponentBattleCard = SpawnCard(roundData.OpponentCard, _opponentDeckPosition.position);
            
            var sequence = DOTween.Sequence();
            sequence.Append(_playerBattleCard.transform.DOMove(_playerBattlePosition.position, moveDuration).SetEase(moveEase));
            sequence.Join(_opponentBattleCard.transform.DOMove(_opponentBattlePosition.position, moveDuration).SetEase(moveEase));
            
            await sequence.AsyncWaitForCompletion();
        }
        
        public async UniTask FlipBattleCards()
        {
            var flipDuration = _animationDataBundle.War.RevealAnimation.Duration;
            var delayBetweenFlips = _animationDataBundle.War.RevealDelay;
            var flipEase = _animationDataBundle.Battle.RevealAnimation.EasingCurve;
            
            Debug.Log($"[GameBoardController] Flipping battle cards - Duration: {flipDuration}s, Delay: {delayBetweenFlips}s");

            if (_isPaused) await WaitWhilePaused();


            if (delayBetweenFlips > 0)
            {
                await UniTask.Delay((int)(delayBetweenFlips * 1000));
            }
            
            var flipTasks = new List<UniTask>();
            
            if (_playerBattleCard != null)
                flipTasks.Add(_playerBattleCard.FlipCardAsync(true, flipDuration, flipEase));
                
            if (_opponentBattleCard != null)
                flipTasks.Add(_opponentBattleCard.FlipCardAsync(true, flipDuration, flipEase));
            
            await UniTask.WhenAll(flipTasks);
        }
        
        public async UniTask HighlightWinner(RoundResult result)
        {
            var scaleMultiplier = _animationDataBundle.WinnerHighlight.ScaleMultiplier;
            var scaleDuration = _animationDataBundle.WinnerHighlight.ScaleDuration;
            var tintColor = _animationDataBundle.WinnerHighlight.TintColor;
            
            Debug.Log($"[GameBoardController] Highlighting winner - Scale: {scaleMultiplier}x, Duration: {scaleDuration}s");
            
            var winnerCard = result == RoundResult.PlayerWins ? _playerBattleCard : _opponentBattleCard;
            
            if (winnerCard != null)
            {
                var sequence = DOTween.Sequence();
                sequence.Append(winnerCard.transform.DOScale(scaleMultiplier, scaleDuration * 0.5f).SetEase(Ease.OutBack));
                sequence.Append(winnerCard.transform.DOScale(1f, scaleDuration * 0.5f).SetEase(Ease.InBack));
                
                if (tintColor != Color.white)
                {
                    sequence.Join(winnerCard.SetTint(tintColor, scaleDuration));
                }
                
                await sequence.AsyncWaitForCompletion();
            }
        }
        
        public async UniTask CollectBattleCards(RoundResult result)
        {
            var collectionDuration = _animationDataBundle.Collection.Duration;
            var staggerDelay = _animationDataBundle.Collection.StaggerDelay;
            var collectionEase = _animationDataBundle.Collection.EasingCurve;
            
            Debug.Log($"[GameBoardController] Collecting battle cards - Duration: {collectionDuration}s");
            
            var targetPosition = result == RoundResult.PlayerWins ? 
                _playerDeckPosition.position : _opponentDeckPosition.position;
            
            var sequence = DOTween.Sequence();
            
            if (_playerBattleCard != null)
            {
                sequence.Append(_playerBattleCard.transform.DOMove(targetPosition, collectionDuration).SetEase(collectionEase));
            }
            
            if (_opponentBattleCard != null)
            {
                if (staggerDelay > 0)
                    sequence.AppendInterval(staggerDelay);
                sequence.Append(_opponentBattleCard.transform.DOMove(targetPosition, collectionDuration).SetEase(collectionEase));
            }
            
            await sequence.AsyncWaitForCompletion();
            
            ClearBattleCards();
        }

        #endregion Battle Animation Methods

        
        #region War Animation Methods

        public async UniTask PlaceWarCards(RoundData warData)
        {
            if (warData == null)
            {
                Debug.LogError("[GameBoardController] War data is null");
                return;
            }

            var faceDownCardsPerPlayer = _animationDataBundle.War.FaceDownCardsPerPlayer;
            var placeDuration = _animationDataBundle.War.PlaceCardsAnimation.Duration;
            var cardSpacing = _animationDataBundle.War.CardSpacing;
            
            Debug.Log($"[GameBoardController] Placing {faceDownCardsPerPlayer} face-down cards per player");
            
            ClearWarCards();
            
            var sequence = DOTween.Sequence();
            var delay = 0f;
            
            if (warData.PlayerWarCards != null)
            {
                for (var i = 0; i < warData.PlayerWarCards.Count && i < _playerWarPositions.Length; i++)
                {
                    if (_playerWarPositions[i] != null)
                    {
                        var warCard = SpawnCard(warData.PlayerWarCards[i], _playerDeckPosition.position);
                        _warCards.Add(warCard);
                        
                        var isLastCard = (i == warData.PlayerWarCards.Count - 1);
                        if (!isLastCard)
                        {
                            warCard.FlipCard(false, 0);
                        }
                        
                        sequence.Insert(delay, warCard.transform.DOMove(_playerWarPositions[i].position, placeDuration));
                        delay += cardSpacing;
                    }
                }
            }
            
            if (warData.OpponentWarCards != null)
            {
                delay = 0f;
                for (var i = 0; i < warData.OpponentWarCards.Count && i < _opponentWarPositions.Length; i++)
                {
                    if (_opponentWarPositions[i] != null)
                    {
                        var warCard = SpawnCard(warData.OpponentWarCards[i], _opponentDeckPosition.position);
                        _warCards.Add(warCard);
                        
                        var isLastCard = (i == warData.OpponentWarCards.Count - 1);
                        if (!isLastCard)
                        {
                            warCard.FlipCard(false, 0);
                        }
                        
                        sequence.Insert(delay, warCard.transform.DOMove(_opponentWarPositions[i].position, placeDuration));
                        delay += cardSpacing;
                    }
                }
            }
            
            await sequence.AsyncWaitForCompletion();
        }
        
        public async UniTask RevealWarCards()
        {
            var revealDuration = _animationDataBundle.War.RevealAnimation.Duration;
            var revealEase = _animationDataBundle.War.RevealAnimation.EasingCurve;
   
            Debug.Log($"[GameBoardController] Revealing war cards - Duration: {revealDuration}s");
            
            var revealTasks = new List<UniTask>();
            
            foreach (var card in _warCards)
            {
                if (card != null && !card.IsFaceUp)
                {
                    var isLastPlayerCard = Array.IndexOf(_playerWarPositions, card.transform.position) == 3;
                    var isLastOpponentCard = Array.IndexOf(_opponentWarPositions, card.transform.position) == 3;
                    
                    if (isLastPlayerCard || isLastOpponentCard)
                    {
                        revealTasks.Add(card.FlipCardAsync(true, revealDuration, revealEase));
                    }
                }
            }
            
            if (revealTasks.Count > 0)
            {
                await UniTask.WhenAll(revealTasks);
            }
        }
        
        public async UniTask RevealAllWarCards()
        {
            Debug.Log($"[GameBoardController] Revealing all face-down war cards");
            
            var config = _animationDataBundle.War;
            var sequence = DOTween.Sequence();
            var delay = 0f;
            
            foreach (var card in _warCards)
            {
                if (card != null && !card.IsFaceUp)
                {
                    sequence.Insert(delay, DOTween.To(
                        () => 0f,
                        _ => { },
                        1f,
                        0.01f
                    ).OnComplete(() => card.FlipCard(true, config.RevealAnimation.Duration)));
                    
                    delay += 0.1f;
                }
            }
            
            await sequence.AsyncWaitForCompletion();
            await UniTask.Delay(500);
        }
        
        public async UniTask CollectWarCards(RoundResult result)
        {
            var collectionDuration = _animationDataBundle.Collection.Duration;
            var staggerDelay = _animationDataBundle.Collection.StaggerDelay;
            var collectionEase = _animationDataBundle.Collection.EasingCurve;
            
            Debug.Log($"[GameBoardController] Collecting all war cards - Duration: {collectionDuration}s");
            
            var targetPosition = result == RoundResult.PlayerWins ? 
                _playerDeckPosition.position : _opponentDeckPosition.position;
            
            var sequence = DOTween.Sequence();
            var delay = 0f;
            
            foreach (var card in _warCards)
            {
                if (card != null)
                {
                    sequence.Insert(delay, card.transform.DOMove(targetPosition, collectionDuration).SetEase(collectionEase));
                    delay += staggerDelay;
                }
            }
            
            if (_playerBattleCard != null)
            {
                sequence.Insert(delay, _playerBattleCard.transform.DOMove(targetPosition, collectionDuration).SetEase(collectionEase));
                delay += staggerDelay;
            }
            
            if (_opponentBattleCard != null)
            {
                sequence.Insert(delay, _opponentBattleCard.transform.DOMove(targetPosition, collectionDuration).SetEase(collectionEase));
            }
            
            await sequence.AsyncWaitForCompletion();
            
            ClearAllCards();
        }
        
        public async UniTask ReturnWarCardsToBothPlayers()
        {
            var returnDuration = _animationDataBundle.Collection.Duration;
            var returnEase = _animationDataBundle.Collection.EasingCurve;
            Debug.Log($"[GameBoardController] War ended in draw - Returning cards to both players");
            
            var sequence = DOTween.Sequence();
            
            var playerCardIndex = 0;
            var opponentCardIndex = 0;
            
            foreach (var card in _warCards)
            {
                if (card != null)
                {
                    var isPlayerCard = false;
                    foreach (var pos in _playerWarPositions)
                    {
                        if (pos != null && Vector3.Distance(card.transform.position, pos.position) < 0.1f)
                        {
                            isPlayerCard = true;
                            break;
                        }
                    }
                    
                    var targetPosition = isPlayerCard ? _playerDeckPosition.position : _opponentDeckPosition.position;
                    var delay = isPlayerCard ? playerCardIndex * 0.1f : opponentCardIndex * 0.1f;
                    
                    sequence.Insert(delay, card.transform.DOMove(targetPosition, returnDuration).SetEase(returnEase));
                    
                    if (isPlayerCard) playerCardIndex++;
                    else opponentCardIndex++;
                }
            }
            
            await sequence.AsyncWaitForCompletion();
            
            ClearAllCards();
        }

        #endregion War Animation Methods

        
        #region Pause/Resume

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
        
        public void PauseAnimationsWithTransition()
        {
            Debug.Log($"[GameBoardController] Pausing Animations");
            PauseAnimations();
        }
        
        public void ResumeAnimationsWithTransition()
        {
            Debug.Log($"[GameBoardController] Resuming animations");
            ResumeAnimations();
        }

        private async UniTask WaitWhilePaused()
        {
            while (_isPaused)
            {
                await UniTask.Yield();
            }
        }

        #endregion Pause/Resume

        
        #region Card Management

        private CardView SpawnCard(CardData cardData, Vector3 position, bool loadSprites = true)
        {
            if (_cardPool == null)
            {
                Debug.LogError("[GameBoardController] Card pool not initialized");
                return null;
            }
            
            var card = _cardPool.Get();
            card.transform.position = position;
            
            if (cardData != null)
            {
                card.SetCardData(cardData);
                if (loadSprites)
                {
                    LoadCardSprite(card, cardData);
                }
            }
            else
            {
                var backSprite = _assetService.GetCardBackSprite();
                if (backSprite != null)
                {
                    card.SetBackSprite(backSprite);
                }
            }
            
            card.FlipCard(false, 0);
            _activeCards.Add(card);
            
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

        private void ReturnCard(CardView card)
        {
            if (card != null && _cardPool != null)
            {
                _activeCards.Remove(card);
                _cardPool.Return(card);
            }
        }

        private void ClearBattleCards()
        {
            ReturnCard(_playerBattleCard);
            ReturnCard(_opponentBattleCard);
            _playerBattleCard = null;
            _opponentBattleCard = null;
        }

        private void ClearWarCards()
        {
            foreach (var card in _warCards)
            {
                ReturnCard(card);
            }
            _warCards.Clear();
        }

        private void ClearAllCards()
        {
            ClearBattleCards();
            ClearWarCards();
        }

        #endregion Card Management

        
        #region Event Handlers

        private void HandleGamePaused()
        {
            PauseAnimations();
        }

        private void HandleGameResumed()
        {
            ResumeAnimations();
        }
        

        #endregion Event Handlers

        
        #region Cleanup

        private void UnsubscribeFromEvents()
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
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            
            OnDrawButtonPressed = null;
            OnRoundAnimationComplete = null;
            
            DOTween.KillAll();
            
            foreach (var card in _activeCards)
            {
                if (card != null && _cardPool != null)
                {
                    _cardPool.Return(card);
                }
            }
            
            _activeCards.Clear();
            _cardPool?.ReturnAll();
            _warCards?.Clear();
            
            _isInitialized = false;
        }

        #endregion Cleanup
    }
}