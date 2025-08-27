using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardWar.Services.Assets
{
    /// <summary>
    /// Concrete implementation of asset loading request with cancellation support
    /// </summary>
    public class AssetLoadRequest<T> : IAssetRequest<T> where T : UnityEngine.Object
    {
        private readonly IAssetManager _assetManager;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed;

        public string AssetPath { get; }
        public T Asset { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsCancelled { get; private set; }
        public bool IsError { get; private set; }
        public string ErrorMessage { get; private set; }
        public float Progress { get; private set; }
        public Type AssetType => typeof(T);

        public event Action<T> OnLoaded;
        public event Action<string> OnError;
        public event Action<float> OnProgress;

        public AssetLoadRequest(string assetPath, IAssetManager assetManager)
        {
            AssetPath = assetPath ?? throw new ArgumentNullException(nameof(assetPath));
            _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async UniTask<T> LoadAsync(CancellationToken externalToken = default)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AssetLoadRequest<T>));
            }

            if (IsCompleted)
            {
                if (IsError)
                {
                    throw new AssetLoadException($"Asset loading failed: {ErrorMessage}");
                }
                return Asset;
            }

            try
            {
                // Combine external and internal cancellation tokens
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    _cancellationTokenSource.Token, 
                    externalToken))
                {
                    // Simulate progress for Resources.Load (instant in reality)
                    Progress = 0f;
                    OnProgress?.Invoke(Progress);

                    // Delegate to AssetManager for actual loading
                    Asset = await _assetManager.LoadAssetInternalAsync<T>(
                        AssetPath, 
                        UpdateProgress,
                        linkedCts.Token);

                    if (Asset == null)
                    {
                        throw new AssetLoadException($"Failed to load asset at path: {AssetPath}");
                    }

                    Progress = 1f;
                    OnProgress?.Invoke(Progress);
                    IsCompleted = true;
                    
                    OnLoaded?.Invoke(Asset);
                    return Asset;
                }
            }
            catch (OperationCanceledException)
            {
                IsCancelled = true;
                IsCompleted = true;
                ErrorMessage = "Asset loading was cancelled";
                OnError?.Invoke(ErrorMessage);
                throw;
            }
            catch (Exception ex)
            {
                IsError = true;
                IsCompleted = true;
                ErrorMessage = ex.Message;
                OnError?.Invoke(ErrorMessage);
                
                Debug.LogError($"[AssetLoadRequest] Failed to load asset '{AssetPath}': {ex.Message}");
                throw new AssetLoadException($"Failed to load asset '{AssetPath}'", ex);
            }
        }

        private void UpdateProgress(float progress)
        {
            Progress = progress;
            OnProgress?.Invoke(progress);
        }

        public void Cancel()
        {
            if (!IsCompleted && !_isDisposed)
            {
                _cancellationTokenSource?.Cancel();
                IsCancelled = true;
                IsCompleted = true;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            
            Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            
            OnLoaded = null;
            OnError = null;
            OnProgress = null;
            
            _isDisposed = true;
        }
    }

    /// <summary>
    /// Custom exception for asset loading failures
    /// </summary>
    public class AssetLoadException : Exception
    {
        public AssetLoadException(string message) : base(message) { }
        public AssetLoadException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}