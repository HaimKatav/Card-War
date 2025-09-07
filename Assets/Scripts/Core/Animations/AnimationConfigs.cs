using System;
using UnityEngine;
using DG.Tweening;

namespace CardWar.Animation.Data
{
    [Serializable]
    public abstract class BaseAnimationConfig
    {
        public virtual void OnValidate() { }
    }
    
    [Serializable]
    public class BattleAnimationConfig : BaseAnimationConfig
    {
        [Header("Draw Phase")]
        public CardMoveAnimationConfig DrawAnimation = new() { Duration = 0.5f, EasingCurve = Ease.OutCubic };
        [Range(0f, 2f)] public float PreDrawDelay = 0f;
        [Range(0f, 2f)] public float PostDrawDelay = 0.2f;
        
        [Header("Flip Phase")]
        public CardFlipAnimationConfig RevealAnimation = new() { Duration = 0.3f };
        [Range(0f, 2f)] public float PreFlipDelay = 0.3f;
        [Range(0f, 2f)] public float PostFlipDelay = 0.5f;
        
        [Header("Highlight Phase")]
        [Range(0f, 2f)] public float PreHighlightDelay = 0.2f;
        [Range(0f, 2f)] public float PostHighlightDelay = 0.3f;
        
        [Header("Collection Phase")]
        [Range(0f, 2f)] public float PreCollectionDelay = 0.2f;
        [Range(0f, 2f)] public float PostCollectionDelay = 0.3f;
        
        [Header("General")]
        [Range(0.1f, 2f)] public float CardSpacing = 0.5f;
        
        public BattleAnimationConfig Clone()
        {
            return new BattleAnimationConfig
            {
                DrawAnimation = DrawAnimation.Clone(),
                PreDrawDelay = PreDrawDelay,
                PostDrawDelay = PostDrawDelay,
                RevealAnimation = RevealAnimation.Clone(),
                PreFlipDelay = PreFlipDelay,
                PostFlipDelay = PostFlipDelay,
                PreHighlightDelay = PreHighlightDelay,
                PostHighlightDelay = PostHighlightDelay,
                PreCollectionDelay = PreCollectionDelay,
                PostCollectionDelay = PostCollectionDelay,
                CardSpacing = CardSpacing
            };
        }
    }
    
    [Serializable]
    public class WarAnimationConfig : BaseAnimationConfig
    {
        [Header("Placement Phase")]
        public CardMoveAnimationConfig PlaceCardsAnimation = new() { Duration = 0.4f, EasingCurve = Ease.OutCubic };
        [Range(0f, 2f)] public float PrePlacementDelay = 0f;
        [Range(0f, 2f)] public float PostPlacementDelay = 0.3f;
        [Range(0f, 1f)] public float CardPlacementStagger = 0.1f;
        
        [Header("Reveal Phase")]
        public CardFlipAnimationConfig RevealAnimation = new() { Duration = 0.3f };
        [Range(0f, 2f)] public float PreRevealDelay = 0.2f;
        [Range(0f, 2f)] public float PostRevealDelay = 0.5f;
        
        [Header("Sequential Reveal")]
        [Range(0f, 2f)] public float PreSequentialRevealDelay = 0.3f;
        [Range(0f, 1f)] public float SequentialRevealStagger = 0.1f;
        [Range(0f, 2f)] public float PostSequentialRevealDelay = 0.5f;
        
        [Header("Collection Phase")]
        [Range(0f, 2f)] public float PreWarCollectionDelay = 0.3f;
        [Range(0f, 2f)] public float PostWarCollectionDelay = 0.5f;
        
        [Header("General")]
        [Range(1, 4)] public int FaceDownCardsPerPlayer = 3;
        [Range(0.1f, 1f)] public float CardSpacing = 0.2f;
        
        public WarAnimationConfig Clone()
        {
            return new WarAnimationConfig
            {
                PlaceCardsAnimation = PlaceCardsAnimation.Clone(),
                PrePlacementDelay = PrePlacementDelay,
                PostPlacementDelay = PostPlacementDelay,
                CardPlacementStagger = CardPlacementStagger,
                RevealAnimation = RevealAnimation.Clone(),
                PreRevealDelay = PreRevealDelay,
                PostRevealDelay = PostRevealDelay,
                PreSequentialRevealDelay = PreSequentialRevealDelay,
                SequentialRevealStagger = SequentialRevealStagger,
                PostSequentialRevealDelay = PostSequentialRevealDelay,
                PreWarCollectionDelay = PreWarCollectionDelay,
                PostWarCollectionDelay = PostWarCollectionDelay,
                FaceDownCardsPerPlayer = FaceDownCardsPerPlayer,
                CardSpacing = CardSpacing
            };
        }
    }
    
    [Serializable]
    public class CardMoveAnimationConfig : BaseAnimationConfig
    {
        [Range(0.1f, 2f)] public float Duration = 0.5f;
        public Ease EasingCurve = Ease.OutCubic;
        [Range(0.5f, 2f)] public float ScaleMultiplier = 1f;
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
        [Range(0.1f, 1f)] public float Duration = 0.3f;
        [Range(0f, 1f)] public float DelayBetweenFlips = 0f;
        public Ease EasingCurve = Ease.InOutQuad;
        public Vector3 RotationAxis = Vector3.up;
        [Range(90f, 360f)] public float RotationAngle = 180f;
        
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
    public class CollectionAnimationConfig : BaseAnimationConfig
    {
        [Header("Movement")]
        [Range(0.1f, 2f)] public float Duration = 0.6f;
        [Range(0f, 0.5f)] public float StaggerDelay = 0.1f;
        public Ease EasingCurve = Ease.InBack;
        public bool UseStagger = true;
        
        [Header("Effects")]
        public bool ScaleOnCollection = false;
        [Range(0.5f, 1.5f)] public float CollectionScale = 0.9f;
        
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
        [Header("Highlight Settings")]
        public bool EnableHighlight = true;
        [Range(1f, 2f)] public float ScaleMultiplier = 1.3f;
        [Range(0.1f, 1f)] public float ScaleDuration = 0.3f;
        public Ease ScaleEase = Ease.OutBack;
        
        [Header("Tint Settings")]
        public Color TintColor = Color.white;
        public bool UseTint = false;
        [Range(0.1f, 1f)] public float TintDuration = 0.3f;
        
        public WinnerHighlightConfig Clone()
        {
            return new WinnerHighlightConfig
            {
                EnableHighlight = EnableHighlight,
                ScaleMultiplier = ScaleMultiplier,
                ScaleDuration = ScaleDuration,
                ScaleEase = ScaleEase,
                TintColor = TintColor,
                UseTint = UseTint,
                TintDuration = TintDuration
            };
        }
    }
    
    [Serializable]
    public class UtilityAnimationConfig : BaseAnimationConfig
    {
        [Header("Shuffle Animation")]
        [Range(0.3f, 2f)] public float ShuffleDuration = 0.8f;
        [Range(0.1f, 1f)] public float ShuffleHeight = 0.5f;
        public Ease ShuffleEase = Ease.InOutQuad;
        
        [Header("Conceal Animation")]
        [Range(0.1f, 1f)] public float ConcealDuration = 0.3f;
        [Range(0f, 2f)] public float PreConcealDelay = 0f;
        [Range(0f, 2f)] public float PostConcealDelay = 0.3f;
        
        [Header("Return to Deck")]
        [Range(0.3f, 2f)] public float ReturnDuration = 0.5f;
        [Range(0f, 0.5f)] public float ReturnStagger = 0.1f;
        public Ease ReturnEase = Ease.InCubic;
        
        public UtilityAnimationConfig Clone()
        {
            return new UtilityAnimationConfig
            {
                ShuffleDuration = ShuffleDuration,
                ShuffleHeight = ShuffleHeight,
                ShuffleEase = ShuffleEase,
                ConcealDuration = ConcealDuration,
                PreConcealDelay = PreConcealDelay,
                PostConcealDelay = PostConcealDelay,
                ReturnDuration = ReturnDuration,
                ReturnStagger = ReturnStagger,
                ReturnEase = ReturnEase
            };
        }
    }
    
    [Serializable]
    public class TransitionAnimationConfig : BaseAnimationConfig
    {
        [Header("Fade Transitions")]
        [Range(0.1f, 1f)] public float FadeInDuration = 0.3f;
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