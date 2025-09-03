using System;
using UnityEngine;
using DG.Tweening;

namespace CardWar.Animation.Data
{
    [Serializable]
    public class BaseAnimationConfig { }

    [Serializable]
    public class CardMoveAnimationConfig : BaseAnimationConfig
    {
        [Header("Movement Settings")]
        [Range(0.1f, 2f)] public float Duration = 0.5f;
        public Ease EasingCurve = Ease.OutCubic;
        [Range(0.8f, 1.5f)] public float ScaleMultiplier = 1.0f;
        public bool UseScaling = false;
        
        public CardMoveAnimationConfig Clone()
        {
            return new CardMoveAnimationConfig
            {
                Duration = Duration,
                EasingCurve = EasingCurve,
                ScaleMultiplier = ScaleMultiplier,
                UseScaling = UseScaling
            };
        }
    }
    
    [Serializable]
    public class CardFlipAnimationConfig : BaseAnimationConfig
    {
        [Header("Flip Settings")]
        [Range(0.1f, 1f)] public float Duration = 0.3f;
        [Range(0f, 1f)] public float DelayBetweenFlips = 0.3f;
        public Ease EasingCurve = Ease.InOutQuad;
        public Vector3 RotationAxis = Vector3.up;
        public float RotationAngle = 180f;
        
        public CardFlipAnimationConfig Clone()
        {
            return new CardFlipAnimationConfig
            {
                Duration = Duration,
                DelayBetweenFlips = DelayBetweenFlips,
                EasingCurve = EasingCurve,
                RotationAxis = RotationAxis,
                RotationAngle = RotationAngle
            };
        }
    }
    
    [Serializable]
    public class BattleAnimationConfig : BaseAnimationConfig
    {
        [Header("Battle Sequence")]
        public CardMoveAnimationConfig DrawAnimation = new();
        public CardFlipAnimationConfig RevealAnimation = new();
        [Range(0f, 2f)] public float PreBattleDelay = 0.5f;
        [Range(0f, 2f)] public float PostBattleDelay = 1.0f;
        [Range(0.5f, 3f)] public float CardSpacing = 1.5f;
        
        public BattleAnimationConfig Clone()
        {
            return new BattleAnimationConfig
            {
                DrawAnimation = DrawAnimation.Clone(),
                RevealAnimation = RevealAnimation.Clone(),
                PreBattleDelay = PreBattleDelay,
                PostBattleDelay = PostBattleDelay,
                CardSpacing = CardSpacing
            };
        }
    }
    
    [Serializable]
    public class WarAnimationConfig : BaseAnimationConfig
    {
        [Header("War Sequence")]
        public CardMoveAnimationConfig PlaceCardsAnimation = new();
        public CardFlipAnimationConfig RevealAnimation = new();
        [Range(1, 4)] public int FaceDownCardsPerPlayer = 3;
        [Range(0.1f, 1f)] public float CardSpacing = 0.2f;
        [Range(0f, 2f)] public float SequenceDelay = 0.5f;
        [Range(0f, 1f)] public float RevealDelay = 0.3f;
        
        public WarAnimationConfig Clone()
        {
            return new WarAnimationConfig
            {
                PlaceCardsAnimation = PlaceCardsAnimation.Clone(),
                RevealAnimation = RevealAnimation.Clone(),
                FaceDownCardsPerPlayer = FaceDownCardsPerPlayer,
                CardSpacing = CardSpacing,
                SequenceDelay = SequenceDelay,
                RevealDelay = RevealDelay
            };
        }
    }
    
    [Serializable]
    public class CollectionAnimationConfig : BaseAnimationConfig
    {
        [Header("Collection Settings")]
        [Range(0.1f, 2f)] public float Duration = 0.6f;
        [Range(0f, 0.5f)] public float StaggerDelay = 0.1f;
        public Ease EasingCurve = Ease.InBack;
        public bool UseStagger = true;
        public bool ScaleOnCollection = false;
        [Range(0.8f, 1.2f)] public float CollectionScale = 0.9f;
        
        public CollectionAnimationConfig Clone()
        {
            return new CollectionAnimationConfig
            {
                Duration = Duration,
                StaggerDelay = StaggerDelay,
                EasingCurve = EasingCurve,
                UseStagger = UseStagger,
                ScaleOnCollection = ScaleOnCollection,
                CollectionScale = CollectionScale
            };
        }
    }
    
    [Serializable]
    public class WinnerHighlightConfig : BaseAnimationConfig
    {
        [Header("Winner Effects")]
        public bool EnableHighlight = true;
        [Range(1f, 1.5f)] public float ScaleMultiplier = 1.1f;
        [Range(0.1f, 1f)] public float ScaleDuration = 0.3f;
        public Ease ScaleEase = Ease.OutBack;
        public Color TintColor = new(1f, 1f, 0.8f, 1f);
        public bool UseTint = true;
        
        public WinnerHighlightConfig Clone()
        {
            return new WinnerHighlightConfig
            {
                EnableHighlight = EnableHighlight,
                ScaleMultiplier = ScaleMultiplier,
                ScaleDuration = ScaleDuration,
                ScaleEase = ScaleEase,
                TintColor = TintColor,
                UseTint = UseTint
            };
        }
    }
    
    [Serializable]
    public class TransitionAnimationConfig : BaseAnimationConfig
    {
        [Header("Transitions")]
        [Range(0.1f, 1f)] public float FadeInDuration = 0.2f;
        [Range(0.1f, 1f)] public float FadeOutDuration = 0.3f;
        public Ease FadeInEase = Ease.OutQuad;
        public Ease FadeOutEase = Ease.InQuad;
        [Range(0.1f, 0.5f)] public float PauseFadeDuration = 0.2f;
        
        public TransitionAnimationConfig Clone()
        {
            return new TransitionAnimationConfig
            {
                FadeInDuration = FadeInDuration,
                FadeOutDuration = FadeOutDuration,
                FadeInEase = FadeInEase,
                FadeOutEase = FadeOutEase,
                PauseFadeDuration = PauseFadeDuration
            };
        }
    }
    
    [Serializable]
    public class TimingConfig : BaseAnimationConfig
    {
        [Header("Round Timing")]
        [Range(0f, 3f)] public float RoundStartDelay = 0.3f;
        [Range(0f, 3f)] public float RoundEndDelay = 1.0f;
        [Range(0f, 5f)] public float GameOverDelay = 2.0f;
        [Range(0f, 2f)] public float BetweenActionsDelay = 0.5f;
        
        public TimingConfig Clone()
        {
            return new TimingConfig
            {
                RoundStartDelay = RoundStartDelay,
                RoundEndDelay = RoundEndDelay,
                GameOverDelay = GameOverDelay,
                BetweenActionsDelay = BetweenActionsDelay
            };
        }
    }
    
    [Serializable]
    public class CardPoolConfig : BaseAnimationConfig
    {
        [Header("Object Pooling")]
        [Range(4, 52)] public int InitialPoolSize = 20;
        [Range(4, 52)] public int MaxPoolSize = 52;
        public bool PrewarmPool = true;
        public bool ExpandDynamically = true;
        
        public CardPoolConfig Clone()
        {
            return new CardPoolConfig
            {
                InitialPoolSize = InitialPoolSize,
                MaxPoolSize = MaxPoolSize,
                PrewarmPool = PrewarmPool,
                ExpandDynamically = ExpandDynamically
            };
        }
    }
}