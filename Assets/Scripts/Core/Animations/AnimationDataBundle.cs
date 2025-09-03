using UnityEngine;

namespace CardWar.Animation.Data
{
    public class AnimationDataBundle
    {
        public BattleAnimationConfig Battle { get; private set; }
        public WarAnimationConfig War { get; private set; }
        public CollectionAnimationConfig Collection { get; private set; }
        public WinnerHighlightConfig WinnerHighlight { get; private set; }
        public TransitionAnimationConfig Transitions { get; private set; }
        public TimingConfig Timing { get; private set; }
        public CardPoolConfig CardPool { get; private set; }
        
        private AnimationDataBundle() { }
        
        public static AnimationDataBundle CreateFromSettings(AnimationSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("[AnimationDataBundle] Cannot create bundle from null settings");
                return CreateDefault();
            }
            
            return new AnimationDataBundle
            {
                Battle = settings.BattleAnimation,
                War = settings.WarAnimation,
                Collection = settings.CollectionAnimation,
                WinnerHighlight = settings.WinnerHighlight,
                Transitions = settings.Transitions,
                Timing = settings.Timing,
                CardPool = settings.CardPool
            };
        }
        
        public static AnimationDataBundle CreateDefault()
        {
            Debug.LogWarning("[AnimationDataBundle] Creating bundle with default values");
            
            return new AnimationDataBundle
            {
                Battle = new BattleAnimationConfig(),
                War = new WarAnimationConfig(),
                Collection = new CollectionAnimationConfig(),
                WinnerHighlight = new WinnerHighlightConfig(),
                Transitions = new TransitionAnimationConfig(),
                Timing = new TimingConfig(),
                CardPool = new CardPoolConfig()
            };
        }
        
        public static AnimationDataBundle CreateForBattle(AnimationSettings settings)
        {
            if (settings == null) return CreateDefault();
            
            return new AnimationDataBundle
            {
                Battle = settings.BattleAnimation,
                Collection = settings.CollectionAnimation,
                WinnerHighlight = settings.WinnerHighlight,
                Transitions = settings.Transitions,
                Timing = settings.Timing,
                War = null,
                CardPool = null
            };
        }
        
        public static AnimationDataBundle CreateForWar(AnimationSettings settings)
        {
            if (settings == null) return CreateDefault();
            
            return new AnimationDataBundle
            {
                War = settings.WarAnimation,
                Collection = settings.CollectionAnimation,
                WinnerHighlight = settings.WinnerHighlight,
                Transitions = settings.Transitions,
                Timing = settings.Timing,
                Battle = null,
                CardPool = null
            };
        }
        
        public AnimationDataBundle WithCustomTiming(TimingConfig customTiming)
        {
            Timing = customTiming;
            return this;
        }
        
        public AnimationDataBundle WithCustomCollection(CollectionAnimationConfig customCollection)
        {
            Collection = customCollection;
            return this;
        }
        
        public AnimationDataBundle DisableWinnerHighlight()
        {
            if (WinnerHighlight != null)
            {
                WinnerHighlight.EnableHighlight = false;
            }
            return this;
        }
    }
}