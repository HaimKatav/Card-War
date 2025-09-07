using UnityEngine;

namespace CardWar.Animation.Data
{
    [CreateAssetMenu(fileName = "AnimationSettings", menuName = "CardWar/AnimationSettings")]
    public class AnimationSettings : ScriptableObject
    {
        [Header("Data Access Mode")]
        [Tooltip("If true, returns cloned configs (safer). If false, returns direct references (faster, but changes affect the asset).")]
        [SerializeField] private bool _useClonedData = true;
        
        [Header("Animation Configurations")]
        [SerializeField] private BattleAnimationConfig _battleAnimation = new();
        [SerializeField] private WarAnimationConfig _warAnimation = new();
        [SerializeField] private CollectionAnimationConfig _collectionAnimation = new();
        [SerializeField] private WinnerHighlightConfig _winnerHighlight = new();
        [SerializeField] private UtilityAnimationConfig _utilityAnimation = new();
        [SerializeField] private TransitionAnimationConfig _transitions = new();
        [SerializeField] private CardPoolConfig _cardPool = new();
        
        public bool UseClonedData => _useClonedData;
        
        public BattleAnimationConfig BattleAnimation => _useClonedData ? _battleAnimation.Clone() : _battleAnimation;
        public WarAnimationConfig WarAnimation => _useClonedData ? _warAnimation.Clone() : _warAnimation;
        public CollectionAnimationConfig CollectionAnimation => _useClonedData ? _collectionAnimation.Clone() : _collectionAnimation;
        public WinnerHighlightConfig WinnerHighlight => _useClonedData ? _winnerHighlight.Clone() : _winnerHighlight;
        public UtilityAnimationConfig UtilityAnimation => _useClonedData ? _utilityAnimation.Clone() : _utilityAnimation;
        public TransitionAnimationConfig Transitions => _useClonedData ? _transitions.Clone() : _transitions;
        public CardPoolConfig CardPool => _useClonedData ? _cardPool.Clone() : _cardPool;
        
        #region Validation
        
        private void OnValidate()
        {
            ValidateAllConfigurations();
        }
        
        private void ValidateAllConfigurations()
        {
            if (_battleAnimation == null) _battleAnimation = new BattleAnimationConfig();
            if (_warAnimation == null) _warAnimation = new WarAnimationConfig();
            if (_collectionAnimation == null) _collectionAnimation = new CollectionAnimationConfig();
            if (_winnerHighlight == null) _winnerHighlight = new WinnerHighlightConfig();
            if (_utilityAnimation == null) _utilityAnimation = new UtilityAnimationConfig();
            if (_transitions == null) _transitions = new TransitionAnimationConfig();
            if (_cardPool == null) _cardPool = new CardPoolConfig();
            
            _warAnimation.FaceDownCardsPerPlayer = Mathf.Clamp(_warAnimation.FaceDownCardsPerPlayer, 1, 4);
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
            _utilityAnimation = new UtilityAnimationConfig();
            _transitions = new TransitionAnimationConfig();
            _cardPool = new CardPoolConfig();
            
            Debug.Log($"[AnimationSettings] Reset all configurations to defaults");
        }
        
        [ContextMenu("Apply Fast Animations")]
        private void ApplyFastAnimations()
        {
            _battleAnimation.DrawAnimation.Duration = 0.3f;
            _battleAnimation.RevealAnimation.Duration = 0.2f;
            _battleAnimation.PreFlipDelay = 0.1f;
            _battleAnimation.PostFlipDelay = 0.2f;
            
            _warAnimation.PlaceCardsAnimation.Duration = 0.3f;
            _warAnimation.RevealAnimation.Duration = 0.2f;
            _warAnimation.PostPlacementDelay = 0.1f;
            
            _collectionAnimation.Duration = 0.4f;
            _collectionAnimation.StaggerDelay = 0.05f;
            
            Debug.Log($"[AnimationSettings] Applied fast animation preset");
        }
        
        [ContextMenu("Apply Slow Animations")]
        private void ApplySlowAnimations()
        {
            _battleAnimation.DrawAnimation.Duration = 0.8f;
            _battleAnimation.RevealAnimation.Duration = 0.5f;
            _battleAnimation.PreFlipDelay = 0.5f;
            _battleAnimation.PostFlipDelay = 0.8f;
            
            _warAnimation.PlaceCardsAnimation.Duration = 0.6f;
            _warAnimation.RevealAnimation.Duration = 0.5f;
            _warAnimation.PostPlacementDelay = 0.5f;
            
            _collectionAnimation.Duration = 0.8f;
            _collectionAnimation.StaggerDelay = 0.15f;
            
            Debug.Log($"[AnimationSettings] Applied slow animation preset");
        }
        
        [ContextMenu("Toggle Clone Mode")]
        private void ToggleCloneMode()
        {
            _useClonedData = !_useClonedData;
            Debug.Log($"[AnimationSettings] Clone mode: {(_useClonedData ? "Enabled (Safer)" : "Disabled (Direct Access)")}");
        }
        
        #endregion
    }
}