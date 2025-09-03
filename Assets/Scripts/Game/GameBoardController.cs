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
        private AnimationConfigManager _animationConfig;
        
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

        public void Initialize()
        {
            if (_isInitialized) return;
            
            _gameController = ServiceLocator.Instance.Get<IGameControllerService>();
            _assetService = ServiceLocator.Instance.Get<IAssetService>();
            _animationConfig = new AnimationConfigManager(_assetService);
            
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

        public async UniTask ShowInitialDeckSetup(float fadeInDuration)
        {
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

        #endregion

        #region Battle Animation Methods

        public async UniTask DrawBattleCards(RoundData roundData, float moveDuration, Ease moveEase)
        {
            if (roundData == null)
            {
                Debug.LogError("[GameBoardController] RoundData is null");
                return;
            }
            
            Debug.Log($"[GameBoardController] Drawing battle cards - Duration: {moveDuration}s");
            
            ClearBattleCards();
            
            _playerBattleCard = SpawnCard(roundData.PlayerCard, _playerDeckPosition.position);
            _opponentBattleCard = SpawnCard(roundData.OpponentCard, _opponentDeckPosition.position);
            
            var sequence = DOTween.Sequence();
            sequence.Append(_playerBattleCard.transform.DOMove(_playerBattlePosition.position, moveDuration).SetEase(moveEase));
            sequence.Join(_opponentBattleCard.transform.DOMove(_opponentBattlePosition.position, moveDuration).SetEase(moveEase));
            
            await sequence.AsyncWaitForCompletion();
        }
        
        public async UniTask FlipBattleCards(float flipDuration, float delayBetweenFlips, Ease flipEase)
        {
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
        
        public async UniTask HighlightWinner(RoundResult result, float scaleMultiplier, float scaleDuration, Color tintColor)
        {
            Debug.Log($"[GameBoardController] Highlighting winner - Scale: {scaleMultiplier}x, Duration: {scaleDuration}s");
            
            CardView winnerCard = result == RoundResult.PlayerWins ? _playerBattleCard : _opponentBattleCard;
            
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
        
        public async UniTask CollectBattleCards(RoundResult result, float collectionDuration, float staggerDelay, Ease collectionEase)
        {
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

        #endregion

        #region War Animation Methods

        public async UniTask PlaceWarCards(RoundData warData, int faceDownCardsPerPlayer, float placeDuration, float cardSpacing)
        {
            if (warData == null)
            {
                Debug.LogError("[GameBoardController] War data is null");
                return;
            }
            
            Debug.Log($"[GameBoardController] Placing {faceDownCardsPerPlayer} face-down cards per player");
            
            ClearWarCards();
            
            var sequence = DOTween.Sequence();
            float delay = 0f;
            
            if (warData.PlayerWarCards != null)
            {
                for (int i = 0; i < warData.PlayerWarCards.Count && i < _playerWarPositions.Length; i++)
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
                for (int i = 0; i < warData.OpponentWarCards.Count && i < _opponentWarPositions.Length; i++)
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
        
        public async UniTask RevealWarCards(float revealDuration, Ease revealEase)
        {
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
            
            var config = _animationConfig.GetWarConfig();
            var sequence = DOTween.Sequence();
            float delay = 0f;
            
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
        
        public async UniTask CollectWarCards(RoundResult result, float collectionDuration, float staggerDelay, Ease collectionEase)
        {
            Debug.Log($"[GameBoardController] Collecting all war cards - Duration: {collectionDuration}s");
            
            var targetPosition = result == RoundResult.PlayerWins ? 
                _playerDeckPosition.position : _opponentDeckPosition.position;
            
            var sequence = DOTween.Sequence();
            float delay = 0f;
            
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
        
        public async UniTask ReturnWarCardsToBothPlayers(float returnDuration, Ease returnEase)
        {
            Debug.Log($"[GameBoardController] War ended in draw - Returning cards to both players");
            
            var sequence = DOTween.Sequence();
            
            int playerCardIndex = 0;
            int opponentCardIndex = 0;
            
            foreach (var card in _warCards)
            {
                if (card != null)
                {
                    bool isPlayerCard = false;
                    foreach (var pos in _playerWarPositions)
                    {
                        if (pos != null && Vector3.Distance(card.transform.position, pos.position) < 0.1f)
                        {
                            isPlayerCard = true;
                            break;
                        }
                    }
                    
                    var targetPosition = isPlayerCard ? _playerDeckPosition.position : _opponentDeckPosition.position;
                    float delay = isPlayerCard ? playerCardIndex * 0.1f : opponentCardIndex * 0.1f;
                    
                    sequence.Insert(delay, card.transform.DOMove(targetPosition, returnDuration).SetEase(returnEase));
                    
                    if (isPlayerCard) playerCardIndex++;
                    else opponentCardIndex++;
                }
            }
            
            await sequence.AsyncWaitForCompletion();
            
            ClearAllCards();
        }

        #endregion

        #region Legacy Methods (for backward compatibility)

        public async UniTask PlayRound(RoundData roundData)
        {
            if (roundData == null) return;
            
            var config = _animationConfig.GetBattleConfig();
            var timing = _animationConfig.GetTimingConfig();
            
            await DrawBattleCards(roundData, config.DrawAnimation.Duration, config.DrawAnimation.EasingCurve);
            await FlipBattleCards(config.RevealAnimation.Duration, config.RevealAnimation.DelayBetweenFlips, config.RevealAnimation.EasingCurve);
            
            if (!roundData.IsWar)
            {
                await UniTask.Delay((int)(timing.RoundEndDelay * 1000));
                await CollectBattleCards(roundData.Result, config.DrawAnimation.Duration, 0, Ease.InOutQuad);
            }
            
            OnRoundAnimationComplete?.Invoke();
        }

        public async UniTask PlayWarSequence(RoundData warRound)
        {
            if (warRound == null) return;
            
            var warConfig = _animationConfig.GetWarConfig();
            var timing = _animationConfig.GetTimingConfig();
            
            await PlaceWarCards(warRound, warConfig.FaceDownCardsPerPlayer, 
                warConfig.PlaceCardsAnimation.Duration, warConfig.CardSpacing);
            
            await UniTask.Delay((int)(warConfig.RevealDelay * 1000));
            
            await RevealWarCards(warConfig.RevealAnimation.Duration, warConfig.RevealAnimation.EasingCurve);
            
            await UniTask.Delay((int)(timing.RoundEndDelay * 1000));
            
            if (!warRound.HasChainedWar)
            {
                await RevealAllWarCards();
                await UniTask.Delay((int)(warConfig.SequenceDelay * 1000));
                
                var collectionConfig = _animationConfig.GetCollectionConfig();
                await CollectWarCards(warRound.Result, collectionConfig.Duration, 
                    collectionConfig.StaggerDelay, collectionConfig.EasingCurve);
            }
            
            OnRoundAnimationComplete?.Invoke();
        }

        #endregion

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
        
        public void PauseAnimationsWithTransition(float fadeDuration)
        {
            Debug.Log($"[GameBoardController] Pausing with {fadeDuration}s transition");
            PauseAnimations();
        }
        
        public void ResumeAnimationsWithTransition(float fadeDuration)
        {
            Debug.Log($"[GameBoardController] Resuming with {fadeDuration}s transition");
            ResumeAnimations();
        }

        private async UniTask WaitWhilePaused()
        {
            while (_isPaused)
            {
                await UniTask.Yield();
            }
        }

        #endregion

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

        #endregion
    }
}