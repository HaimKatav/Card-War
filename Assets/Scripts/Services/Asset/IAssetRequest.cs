using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Services.AssetManagement
{
    /// <summary>
    /// Base interface for all asset requests
    /// </summary>
    public interface IAssetRequest
    {
        string AssetPath { get; }
        bool IsCompleted { get; }
        bool IsCancelled { get; }
        bool IsError { get; }
        string ErrorMessage { get; }
        float Progress { get; }
        Type AssetType { get; }
        
        void Cancel();
    }

    /// <summary>
    /// Generic asset request interface for type-safe loading
    /// </summary>
    public interface IAssetRequest<T> : IAssetRequest where T : UnityEngine.Object
    {
        T Asset { get; }
        UniTask<T> LoadAsync(CancellationToken cancellationToken = default);
        event Action<T> OnLoaded;
        event Action<string> OnError;
        event Action<float> OnProgress;
    }
}