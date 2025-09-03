using UnityEngine;

namespace CardWar.Animation.Data
{
    [CreateAssetMenu(fileName = "AnimationSettings", menuName = "CardWar/AnimationSettings")]
    public class AnimationSettings : ScriptableObject
    {
        [Header("Animation Configurations")]
        [SerializeField] private BattleAnimationConfig _battleAnimation = new BattleAnimationConfig();
        [SerializeField] private WarAnimationConfig _warAnimation = new WarAnimationConfig();
        [SerializeField] private CollectionAnimationConfig _collectionAnimation = new CollectionAnimationConfig();
        [SerializeField] private WinnerHighlightConfig _winnerHighlight = new WinnerHighlightConfig();
        [SerializeField] private TransitionAnimationConfig _transitions = new TransitionAnimationConfig();
        [SerializeField] private TimingConfig _timing = new TimingConfig();
        [SerializeField] private CardPoolConfig _cardPool = new CardPoolConfig();
        
        [Header("Quick Access Presets")]
        [SerializeField] private CardMoveAnimationConfig _quickMove = new CardMoveAnimationConfig { Duration = 0.3f };
        [SerializeField] private CardMoveAnimationConfig _normalMove = new CardMoveAnimationConfig { Duration = 0.5f };
        [SerializeField] private CardMoveAnimationConfig _slowMove = new CardMoveAnimationConfig { Duration = 0.8f };
        
        public BattleAnimationConfig BattleAnimation => _battleAnimation.Clone();
        public WarAnimationConfig WarAnimation => _warAnimation.Clone();
        public CollectionAnimationConfig CollectionAnimation => _collectionAnimation.Clone();
        public WinnerHighlightConfig WinnerHighlight => _winnerHighlight.Clone();
        public TransitionAnimationConfig Transitions => _transitions.Clone();
        public TimingConfig Timing => _timing.Clone();
        public CardPoolConfig CardPool => _cardPool.Clone();
        
        public CardMoveAnimationConfig QuickMove => _quickMove.Clone();
        public CardMoveAnimationConfig NormalMove => _normalMove.Clone();
        public CardMoveAnimationConfig SlowMove => _slowMove.Clone();
        
        public BattleAnimationConfig GetBattleConfig() => _battleAnimation.Clone();
        public WarAnimationConfig GetWarConfig() => _warAnimation.Clone();
        public CollectionAnimationConfig GetCollectionConfig() => _collectionAnimation.Clone();
        public WinnerHighlightConfig GetWinnerConfig() => _winnerHighlight.Clone();
        public TransitionAnimationConfig GetTransitionConfig() => _transitions.Clone();
        public TimingConfig GetTimingConfig() => _timing.Clone();
        public CardPoolConfig GetCardPoolConfig() => _cardPool.Clone();
        
        #region Validation
        
        private void OnValidate()
        {
            ValidateBattleAnimation();
            ValidateWarAnimation();
            ValidateCollectionAnimation();
            ValidateWinnerHighlight();
            ValidateTransitions();
            ValidateTiming();
            ValidateCardPool();
        }
        
        private void ValidateBattleAnimation()
        {
            if (_battleAnimation == null)
                _battleAnimation = new BattleAnimationConfig();
                
            if (_battleAnimation.DrawAnimation == null)
                _battleAnimation.DrawAnimation = new CardMoveAnimationConfig();
                
            if (_battleAnimation.RevealAnimation == null)
                _battleAnimation.RevealAnimation = new CardFlipAnimationConfig();
        }
        
        private void ValidateWarAnimation()
        {
            if (_warAnimation == null)
                _warAnimation = new WarAnimationConfig();
                
            if (_warAnimation.PlaceCardsAnimation == null)
                _warAnimation.PlaceCardsAnimation = new CardMoveAnimationConfig();
                
            if (_warAnimation.RevealAnimation == null)
                _warAnimation.RevealAnimation = new CardFlipAnimationConfig();
                
            _warAnimation.FaceDownCardsPerPlayer = Mathf.Clamp(_warAnimation.FaceDownCardsPerPlayer, 1, 4);
        }
        
        private void ValidateCollectionAnimation()
        {
            if (_collectionAnimation == null)
                _collectionAnimation = new CollectionAnimationConfig();
        }
        
        private void ValidateWinnerHighlight()
        {
            if (_winnerHighlight == null)
                _winnerHighlight = new WinnerHighlightConfig();
        }
        
        private void ValidateTransitions()
        {
            if (_transitions == null)
                _transitions = new TransitionAnimationConfig();
        }
        
        private void ValidateTiming()
        {
            if (_timing == null)
                _timing = new TimingConfig();
        }
        
        private void ValidateCardPool()
        {
            if (_cardPool == null)
                _cardPool = new CardPoolConfig();
                
            _cardPool.InitialPoolSize = Mathf.Clamp(_cardPool.InitialPoolSize, 4, _cardPool.MaxPoolSize);
            _cardPool.MaxPoolSize = Mathf.Max(_cardPool.InitialPoolSize, _cardPool.MaxPoolSize);
        }
        
        #endregion
        
        #region Debug Helpers
        
        [ContextMenu("Reset to Defaults")]
        private void ResetToDefaults()
        {
            _battleAnimation = new BattleAnimationConfig();
            _warAnimation = new WarAnimationConfig();
            _collectionAnimation = new CollectionAnimationConfig();
            _winnerHighlight = new WinnerHighlightConfig();
            _transitions = new TransitionAnimationConfig();
            _timing = new TimingConfig();
            _cardPool = new CardPoolConfig();
            
            Debug.Log($"[AnimationSettings] Reset all configurations to defaults");
        }
        
        [ContextMenu("Log Current Settings")]
        private void LogCurrentSettings()
        {
            Debug.Log($"[AnimationSettings] Battle Move Duration: {_battleAnimation.DrawAnimation.Duration}");
            Debug.Log($"[AnimationSettings] War Cards Per Player: {_warAnimation.FaceDownCardsPerPlayer}");
            Debug.Log($"[AnimationSettings] Collection Duration: {_collectionAnimation.Duration}");
            Debug.Log($"[AnimationSettings] Round End Delay: {_timing.RoundEndDelay}");
            Debug.Log($"[AnimationSettings] Card Pool Size: {_cardPool.InitialPoolSize}/{_cardPool.MaxPoolSize}");
        }
        
        #endregion
    }
}