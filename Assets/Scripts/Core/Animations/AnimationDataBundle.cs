using UnityEngine;

namespace CardWar.Animation.Data
{
    public class AnimationDataBundle
    {
        public BattleAnimationConfig Battle { get; private set; }
        public WarAnimationConfig War { get; private set; }
        public CollectionAnimationConfig Collection { get; private set; }
        public WinnerHighlightConfig WinnerHighlight { get; private set; }
        public UtilityAnimationConfig UtilityAnimation { get; private set; }
        public TransitionAnimationConfig Transitions { get; private set; }
        public CardPoolConfig CardPool { get; private set; }
        
        private readonly bool _isUsingClonedData;
        
        private AnimationDataBundle(bool useClonedData = true)
        {
            _isUsingClonedData = useClonedData;
        }
        
        public static AnimationDataBundle CreateFromSettings(AnimationSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("[AnimationDataBundle] Cannot create bundle from null settings");
                return CreateDefault();
            }
            
            var useClonedData = settings.UseClonedData;
            
            Debug.Log($"[AnimationDataBundle] Creating bundle with {(useClonedData ? "cloned" : "direct")} data access");
            
            return new AnimationDataBundle(useClonedData)
            {
                Battle = settings.BattleAnimation,
                War = settings.WarAnimation,
                Collection = settings.CollectionAnimation,
                WinnerHighlight = settings.WinnerHighlight,
                UtilityAnimation = settings.UtilityAnimation,
                Transitions = settings.Transitions,
                CardPool = settings.CardPool
            };
        }
        
        public static AnimationDataBundle CreateDefault()
        {
            Debug.LogWarning("[AnimationDataBundle] Creating bundle with default values");
            
            return new AnimationDataBundle(true)
            {
                Battle = new BattleAnimationConfig(),
                War = new WarAnimationConfig(),
                Collection = new CollectionAnimationConfig(),
                WinnerHighlight = new WinnerHighlightConfig(),
                UtilityAnimation = new UtilityAnimationConfig(),
                Transitions = new TransitionAnimationConfig(),
                CardPool = new CardPoolConfig()
            };
        }
        
        public bool IsUsingClonedData()
        {
            return _isUsingClonedData;
        }
    }
}