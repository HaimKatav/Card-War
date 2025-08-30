// using System;
// using UnityEngine;
// using CardWar.Services;
// using CardWar.Game.Logic;
// using Cysharp.Threading.Tasks;
//
// namespace CardWar.Game.UI
// {
//     public class TableViewController : MonoBehaviour, IDisposable
//     {
//         [Header("Player Areas")]
//         [SerializeField] private Transform _playerAreaTransform;
//         [SerializeField] private Transform _opponentAreaTransform;
//         
//         [Header("Card Slots")]
//         [SerializeField] private Transform _playerCardSlot;
//         [SerializeField] private Transform _opponentCardSlot;
//         
//         [Header("Decks")]
//         [SerializeField] private Transform _playerDeck;
//         [SerializeField] private Transform _opponentDeck;
//         
//         [Header("War Zone")]
//         [SerializeField] private Transform _warZone;
//         [SerializeField] private GameObject _warBackground;
//         
//         [Header("UI References")]
//         [SerializeField] private GameUIView _gameUIView;
//         
//         private IGameControllerService _gameController;
//         private CardPool _cardPool;
//         private bool _isInitialized;
//         private bool _isPaused;
//
//         #region Initialization
//
//         public void Initialize(IGameControllerService gameController)
//         {
//             if (_isInitialized) return;
//             
//             _gameController = gameController ?? throw new ArgumentNullException(nameof(gameController));
//             
//             SetupComponents();
//             SubscribeToEvents();
//             InitializeCardPool();
//             
//             _isInitialized = true;
//             Debug.Log($"[{GetType().Name}] Initialized successfully");
//         }
//
//         private void SetupComponents()
//         {
//             FindGameElements();
//             
//             if (_warBackground != null)
//             {
//                 _warBackground.SetActive(false);
//             }
//             
//             if (_gameUIView == null)
//             {
//                 _gameUIView = GetComponentInChildren<GameUIView>();
//             }
//         }
//
//         private void FindGameElements()
//         {
//             var transform = this.transform;
//             
//             _playerAreaTransform = transform.Find("PlayerArea");
//             _opponentAreaTransform = transform.Find("OpponentArea");
//             
//             if (_playerAreaTransform != null)
//             {
//                 _playerCardSlot = _playerAreaTransform.Find("CardSlot");
//                 _playerDeck = _playerAreaTransform.Find("PlayerDeck");
//             }
//             
//             if (_opponentAreaTransform != null)
//             {
//                 _opponentCardSlot = _opponentAreaTransform.Find("CardSlot");
//                 _opponentDeck = _opponentAreaTransform.Find("OpponentDeck");
//             }
//             
//             _warZone = transform.Find("WarZone");
//             if (_warZone != null)
//             {
//                 _warBackground = _warZone.Find("WarBackground")?.gameObject;
//             }
//             
//             Debug.Log($"[{GetType().Name}] Game elements found - Player: {_playerAreaTransform != null}, Opponent: {_opponentAreaTransform != null}, War: {_warZone != null}");
//         }
//
//         private void InitializeCardPool()
//         {
//             var poolObject = new GameObject("CardPool");
//             poolObject.transform.SetParent(transform, false);
//             
//             _cardPool = poolObject.AddComponent<CardPool>();
//             _cardPool.Initialize(20);
//             
//             Debug.Log($"[{GetType().Name}] Card pool initialized with 20 cards");
//         }
//
//         #endregion
//
//         #region Event Management
//
//         private void SubscribeToEvents()
//         {
//             if (_gameController != null)
//             {
//                 _gameController.OnRoundStarted += HandleRoundStarted;
//                 _gameController.OnRoundCompleted += HandleRoundCompleted;
//                 _gameController.OnWarStarted += HandleWarStarted;
//                 _gameController.OnWarCompleted += HandleWarCompleted;
//                 _gameController.OnGameOver += HandleGameOver;
//             }
//         }
//
//         private void UnsubscribeFromEvents()
//         {
//             if (_gameController != null)
//             {
//                 _gameController.OnRoundStarted -= HandleRoundStarted;
//                 _gameController.OnRoundCompleted -= HandleRoundCompleted;
//                 _gameController.OnWarStarted -= HandleWarStarted;
//                 _gameController.OnWarCompleted -= HandleWarCompleted;
//                 _gameController.OnGameOver -= HandleGameOver;
//             }
//         }
//
//         #endregion
//
//         #region Game Event Handlers
//
//         private void HandleRoundStarted(RoundData roundData)
//         {
//             if (_isPaused) return;
//             
//             Debug.Log($"[{GetType().Name}] Round started - Player: {roundData.PlayerCard?.GetDisplayName()}, Opponent: {roundData.OpponentCard?.GetDisplayName()}");
//             
//             DrawCardsAsync(roundData).Forget();
//         }
//
//         private async UniTaskVoid DrawCardsAsync(RoundData roundData)
//         {
//             if (_cardPool == null) return;
//             
//             var playerCardView = _cardPool.GetCard();
//             var opponentCardView = _cardPool.GetCard();
//             
//             if (playerCardView != null && _playerDeck != null && _playerCardSlot != null)
//             {
//                 playerCardView.transform.position = _playerDeck.position;
//                 playerCardView.SetCardData(roundData.PlayerCard);
//                 playerCardView.SetFaceDown(true);
//                 playerCardView.gameObject.SetActive(true);
//                 
//                 Debug.Log($"[{GetType().Name}] Player card spawned at deck");
//             }
//             
//             if (opponentCardView != null && _opponentDeck != null && _opponentCardSlot != null)
//             {
//                 opponentCardView.transform.position = _opponentDeck.position;
//                 opponentCardView.SetCardData(roundData.OpponentCard);
//                 opponentCardView.SetFaceDown(true);
//                 opponentCardView.gameObject.SetActive(true);
//                 
//                 Debug.Log($"[{GetType().Name}] Opponent card spawned at deck");
//             }
//             
//             await UniTask.Delay(100);
//             
//             if (playerCardView != null && _playerCardSlot != null)
//             {
//                 playerCardView.transform.position = _playerCardSlot.position;
//             }
//             
//             if (opponentCardView != null && _opponentCardSlot != null)
//             {
//                 opponentCardView.transform.position = _opponentCardSlot.position;
//             }
//             
//             await UniTask.Delay(500);
//             
//             playerCardView?.SetFaceDown(false);
//             opponentCardView?.SetFaceDown(false);
//             
//             UpdateUI(roundData);
//         }
//
//         private void HandleRoundCompleted(RoundResult result)
//         {
//             if (_isPaused) return;
//             
//             Debug.Log($"[{GetType().Name}] Round completed - Winner: {result.Winner}, Player won: {result.PlayerWon}");
//             
//             CollectCardsAsync(result.PlayerWon).Forget();
//         }
//
//         private async UniTaskVoid CollectCardsAsync(bool playerWon)
//         {
//             await UniTask.Delay(1000);
//             
//             var targetDeck = playerWon ? _playerDeck : _opponentDeck;
//             
//             if (_cardPool != null && targetDeck != null)
//             {
//                 var cards = _cardPool.GetActiveCards();
//                 foreach (var card in cards)
//                 {
//                     card.transform.position = targetDeck.position;
//                 }
//                 
//                 await UniTask.Delay(500);
//                 
//                 foreach (var card in cards)
//                 {
//                     _cardPool.ReturnCard(card);
//                 }
//             }
//         }
//
//         private void HandleWarStarted(WarData warData)
//         {
//             if (_isPaused) return;
//             
//             Debug.Log($"[{GetType().Name}] War started!");
//             
//             if (_warBackground != null)
//             {
//                 _warBackground.SetActive(true);
//             }
//             
//             SetupWarCardsAsync(warData).Forget();
//         }
//
//         private async UniTaskVoid SetupWarCardsAsync(WarData warData)
//         {
//             await UniTask.Delay(100);
//             Debug.Log($"[{GetType().Name}] Setting up war cards");
//         }
//
//         private void HandleWarCompleted(WarResult result)
//         {
//             if (_isPaused) return;
//             
//             Debug.Log($"[{GetType().Name}] War completed - Player won: {result.PlayerWon}");
//             
//             if (_warBackground != null)
//             {
//                 _warBackground.SetActive(false);
//             }
//         }
//
//         private void HandleGameOver(GameOverData gameOverData)
//         {
//             Debug.Log($"[{GetType().Name}] Game over - Player won: {gameOverData.PlayerWon}");
//             
//             CleanupCards();
//         }
//
//         #endregion
//
//         #region UI Updates
//
//         private void UpdateUI(RoundData roundData)
//         {
//             if (_gameUIView != null)
//             {
//                 _gameUIView.UpdatePlayerCardCount(roundData.PlayerDeckCount);
//                 _gameUIView.UpdateOpponentCardCount(roundData.OpponentDeckCount);
//             }
//         }
//
//         #endregion
//
//         #region Pause Management
//
//         public void PauseGame()
//         {
//             _isPaused = true;
//             Debug.Log($"[{GetType().Name}] Game paused");
//         }
//
//         public void ResumeGame()
//         {
//             _isPaused = false;
//             Debug.Log($"[{GetType().Name}] Game resumed");
//         }
//
//         #endregion
//
//         #region Cleanup
//
//         private void CleanupCards()
//         {
//             if (_cardPool != null)
//             {
//                 _cardPool.ReturnAllCards();
//             }
//         }
//
//         private void OnDestroy()
//         {
//             Dispose();
//         }
//
//         public void Dispose()
//         {
//             UnsubscribeFromEvents();
//             CleanupCards();
//             
//             if (_cardPool != null)
//             {
//                 Destroy(_cardPool.gameObject);
//                 _cardPool = null;
//             }
//             
//             Debug.Log($"[{GetType().Name}] Disposed");
//         }
//
//         #endregion
//     }
// }