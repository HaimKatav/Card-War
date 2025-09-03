namespace CardWar.Animation.Data
{
    public static class AnimationConstants
    {
        public const float MIN_ANIMATION_DURATION = 0.1f;
        public const float MAX_ANIMATION_DURATION = 5f;
        public const float DEFAULT_ANIMATION_DURATION = 0.5f;
        
        public const float MIN_DELAY = 0f;
        public const float MAX_DELAY = 3f;
        public const float DEFAULT_DELAY = 0.3f;
        
        public const int MAX_WAR_CARDS = 4;
        public const int DEFAULT_WAR_FACEDOWN_CARDS = 3;
        
        public const int MIN_POOL_SIZE = 4;
        public const int MAX_POOL_SIZE = 52;
        public const int DEFAULT_POOL_SIZE = 20;
        
        public const float DEFAULT_CARD_SPACING = 1.5f;
        public const float WAR_CARD_SPACING = 0.2f;
        
        public const float DEFAULT_WINNER_SCALE = 1.1f;
        public const float DEFAULT_FADE_DURATION = 0.2f;
        
        public enum AnimationState
        {
            Idle,
            Playing,
            Paused,
            Completed,
            Cancelled
        }
        
        public enum AnimationType
        {
            Move,
            Flip,
            Scale,
            Fade,
            Rotate,
            Collection,
            Distribution,
            Highlight
        }
        
        public enum SequenceType
        {
            Battle,
            War,
            Collection,
            GameStart,
            GameOver,
            RoundStart,
            RoundEnd,
            WarResolution
        }
    }
}