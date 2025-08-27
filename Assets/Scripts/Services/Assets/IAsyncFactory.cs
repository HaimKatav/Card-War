using System;
using System.Threading;
using CardWar.View.Cards;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Assets.Scripts.Services.AssetManagement
{
    /// <summary>
    /// Base interface for async factories
    /// </summary>
    public interface IAsyncFactory<T> where T : class
    {
        UniTask<T> CreateAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Generic async prefab factory that uses AssetManager for instantiation
    /// </summary>
    public abstract class AsyncPrefabFactory<T> : IAsyncFactory<T>, IDisposable where T : Component
    {
        protected readonly IAssetManager _assetManager;
        protected readonly DiContainer _container;
        protected readonly string _prefabPath;
        protected readonly Transform _parentTransform;
        
        private IAssetRequest<GameObject> _currentRequest;

        protected AsyncPrefabFactory(
            IAssetManager assetManager,
            DiContainer container,
            string prefabPath,
            Transform parentTransform = null)
        {
            _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _prefabPath = prefabPath ?? throw new ArgumentNullException(nameof(prefabPath));
            _parentTransform = parentTransform;
        }

        public virtual async UniTask<T> CreateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Create load request
                _currentRequest = _assetManager.CreateLoadRequest<GameObject>(_prefabPath);
                
                // Load the prefab
                var prefab = await _currentRequest.LoadAsync(cancellationToken);
                
                if (prefab == null)
                {
                    throw new AssetLoadException($"Failed to load prefab at path: {_prefabPath}");
                }

                // Instantiate the prefab
                var instance = UnityEngine.Object.Instantiate(prefab, _parentTransform);
                
                // Get the required component
                var component = instance.GetComponent<T>();
                if (component == null)
                {
                    UnityEngine.Object.Destroy(instance);
                    throw new InvalidOperationException(
                        $"Prefab at '{_prefabPath}' does not have component of type {typeof(T)}");
                }

                // Inject dependencies
                _container.Inject(component);
                
                // Perform any post-creation setup
                await OnPostCreateAsync(component, cancellationToken);
                
                return component;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[AsyncPrefabFactory] Creation of {typeof(T)} was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsyncPrefabFactory] Failed to create {typeof(T)}: {ex.Message}");
                throw;
            }
        }

        protected virtual UniTask OnPostCreateAsync(T instance, CancellationToken cancellationToken)
        {
            // Override in derived classes for custom initialization
            return UniTask.CompletedTask;
        }

        public virtual void Dispose()
        {
            _currentRequest?.Cancel();
            _currentRequest = null;
        }
    }

    /// <summary>
    /// Example: Card factory using the async pattern
    /// </summary>
    public class AsyncCardViewFactory : AsyncPrefabFactory<CardView>
    {
        private const string CARD_PREFAB_PATH = "Prefabs/Cards/CardView";

        [Inject]
        public AsyncCardViewFactory(
            IAssetManager assetManager,
            DiContainer container,
            [InjectOptional] Transform parentTransform = null)
            : base(assetManager, container, CARD_PREFAB_PATH, parentTransform)
        {
        }

        protected override async UniTask OnPostCreateAsync(CardView instance, CancellationToken cancellationToken)
        {
            // Example: Load card back sprite
            var cardBackSprite = await _assetManager.LoadAssetAsync<Sprite>(
                "Sprites/Cards/CardBack", 
                cancellationToken);
            
            // Initialize card with default back sprite
            instance.SetBackSprite(cardBackSprite);
        }
    }

    /// <summary>
    /// Factory with configuration support
    /// </summary>
    public abstract class AsyncConfigurableFactory<TProduct, TConfig> : IDisposable
        where TProduct : Component
    {
        protected readonly IAssetManager _assetManager;
        protected readonly DiContainer _container;

        protected AsyncConfigurableFactory(IAssetManager assetManager, DiContainer container)
        {
            _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public abstract UniTask<TProduct> CreateAsync(TConfig config, 
            CancellationToken cancellationToken = default);

        protected virtual async UniTask<TProduct> CreateFromPrefabAsync(
            string prefabPath, 
            Transform parent,
            CancellationToken cancellationToken)
        {
            var instance = await _assetManager.InstantiateAsync<TProduct>(
                prefabPath, 
                parent, 
                cancellationToken);
            
            _container.Inject(instance);
            return instance;
        }

        public virtual void Dispose()
        {
            // Cleanup if needed
        }
    }
}