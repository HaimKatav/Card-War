using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardWar.Core.Async
{
    public class AsyncOperationManager : IDisposable
    {
        private readonly List<CancellationTokenSource> _activeCancellationSources;
        private readonly CancellationTokenSource _applicationCancellationSource;
        private bool _isDisposed;

        public CancellationToken ApplicationCancellationToken => _applicationCancellationSource.Token;

        public AsyncOperationManager()
        {
            _activeCancellationSources = new List<CancellationTokenSource>();
            _applicationCancellationSource = new CancellationTokenSource();
        }

        public CancellationToken CreateTimeoutToken(float timeoutSeconds)
        {
            var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                ApplicationCancellationToken, 
                timeoutSource.Token);
            
            TrackCancellationSource(linkedSource);
            return linkedSource.Token;
        }

        public CancellationToken CreateLinkedToken(CancellationToken externalToken = default)
        {
            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                ApplicationCancellationToken, 
                externalToken);
            
            TrackCancellationSource(linkedSource);
            return linkedSource.Token;
        }

        public async UniTask<T> ExecuteSafeAsync<T>(
            Func<CancellationToken, UniTask<T>> operation,
            CancellationToken cancellationToken = default,
            string operationName = "Unknown")
        {
            var linkedToken = CreateLinkedToken(cancellationToken);
            
            try
            {
                return await operation(linkedToken);
            }
            catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
            {
                Debug.Log($"[AsyncOperationManager] Operation '{operationName}' was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsyncOperationManager] Operation '{operationName}' failed: {ex.Message}");
                throw;
            }
        }

        public void ExecuteFireAndForget(
            Func<CancellationToken, UniTask> operation,
            string operationName = "FireAndForget")
        {
            ExecuteFireAndForgetAsync(operation, operationName).Forget();
        }

        private async UniTaskVoid ExecuteFireAndForgetAsync(
            Func<CancellationToken, UniTask> operation,
            string operationName)
        {
            var cancellationToken = CreateLinkedToken();
            
            try
            {
                await operation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[AsyncOperationManager] Fire-and-forget operation '{operationName}' was cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsyncOperationManager] Fire-and-forget operation '{operationName}' failed: {ex.Message}");
            }
        }

        private void TrackCancellationSource(CancellationTokenSource source)
        {
            if (_isDisposed) return;
            
            lock (_activeCancellationSources)
            {
                _activeCancellationSources.Add(source);
            }

            source.Token.Register(() =>
            {
                lock (_activeCancellationSources)
                {
                    _activeCancellationSources.Remove(source);
                }
                source.Dispose();
            });
        }

        public void CancelAllOperations()
        {
            Debug.Log("[AsyncOperationManager] Cancelling all async operations");
            _applicationCancellationSource.Cancel();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            
            CancelAllOperations();
            
            lock (_activeCancellationSources)
            {
                foreach (var source in _activeCancellationSources)
                {
                    source?.Dispose();
                }
                _activeCancellationSources.Clear();
            }
            
            _applicationCancellationSource?.Dispose();
            _isDisposed = true;
        }
    }
}