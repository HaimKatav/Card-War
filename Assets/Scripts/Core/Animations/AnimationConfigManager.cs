using UnityEngine;
using CardWar.Services;
using CardWar.Core;

namespace CardWar.Animation.Data
{
    public class AnimationConfigManager
    {
        private AnimationSettings _settings;
        private AnimationDataBundle _cachedBundle;
        
        public AnimationSettings Settings => _settings;
        public bool IsLoaded => _settings != null;
        
        public AnimationConfigManager(IAssetService assetService)
        {
            LoadSettings(assetService);
        }
        
        public AnimationConfigManager(AnimationSettings settings)
        {
            _settings = settings;
            Debug.Log($"[AnimationConfigManager] Initialized with provided settings");
        }
        
        public BattleAnimationConfig GetBattleConfig()
        {
            return _settings?.GetBattleConfig() ?? new BattleAnimationConfig();
        }
        
        public WarAnimationConfig GetWarConfig()
        {
            return _settings?.GetWarConfig() ?? new WarAnimationConfig();
        }
        
        public CollectionAnimationConfig GetCollectionConfig()
        {
            return _settings?.GetCollectionConfig() ?? new CollectionAnimationConfig();
        }
        
        public WinnerHighlightConfig GetWinnerConfig()
        {
            return _settings?.GetWinnerConfig() ?? new WinnerHighlightConfig();
        }
        
        public TransitionAnimationConfig GetTransitionConfig()
        {
            return _settings?.GetTransitionConfig() ?? new TransitionAnimationConfig();
        }
        
        public TimingConfig GetTimingConfig()
        {
            return _settings?.GetTimingConfig() ?? new TimingConfig();
        }
        
        public CardPoolConfig GetCardPoolConfig()
        {
            return _settings?.GetCardPoolConfig() ?? new CardPoolConfig();
        }
        
        public CardMoveAnimationConfig GetMovePreset(MoveSpeed speed)
        {
            if (_settings == null) return new CardMoveAnimationConfig();
            
            return speed switch
            {
                MoveSpeed.Quick => _settings.QuickMove,
                MoveSpeed.Slow => _settings.SlowMove,
                _ => _settings.NormalMove
            };
        }
        
        public AnimationDataBundle GetBundle()
        {
            if (_cachedBundle == null && _settings != null)
            {
                _cachedBundle = AnimationDataBundle.CreateFromSettings(_settings);
            }
            return _cachedBundle ?? AnimationDataBundle.CreateDefault();
        }
        
        private void LoadSettings(IAssetService assetService)
        {
            if (assetService == null)
            {
                Debug.LogError($"[AnimationConfigManager] AssetService is null, using default settings");
                _settings = ScriptableObject.CreateInstance<AnimationSettings>();
                return;
            }
            
            var gameSettings = assetService.LoadAsset<GameSettings>("Settings/GameSettings");
            if (gameSettings != null && gameSettings.AnimationSettings != null)
            {
                _settings = gameSettings.AnimationSettings;
                Debug.Log($"[AnimationConfigManager] Settings loaded from GameSettings");
                return;
            }
            
            _settings = assetService.LoadAsset<AnimationSettings>(GameSettings.ANIMATION_SETTINGS_ASSET_PATH);
            if (_settings != null)
            {
                Debug.Log($"[AnimationConfigManager] Settings loaded directly");
                return;
            }
            
            Debug.LogWarning($"[AnimationConfigManager] Could not load settings, using defaults");
            _settings = ScriptableObject.CreateInstance<AnimationSettings>();
        }
    }
    
    public enum MoveSpeed
    {
        Quick,
        Normal,
        Slow
    }
}