using System;
using System.Collections.Generic;
using CardWar.Core;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace CardWar.Services
{
    public interface IBaseServiceProvider { }
    
    public class ServiceLocator : IDisposable
    {
        private static ServiceLocator _instance;
        private static readonly Dictionary<Type, object> _services = new();
        private static readonly Dictionary<Type, List<UniTaskCompletionSource<object>>> _pendingRequests = new();
        
        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ServiceLocator();
                return _instance;
            }
        }

        private ServiceLocator() { }
        

        public static async UniTask<T> Get<T>() where T : class, IBaseServiceProvider
        {
            var type = typeof(T);
            
            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }
            
            var tcs = new UniTaskCompletionSource<object>();
            
            if (!_pendingRequests.ContainsKey(type))
            {
                _pendingRequests[type] = new List<UniTaskCompletionSource<object>>();
            }
            
            _pendingRequests[type].Add(tcs);
            
            var result = await tcs.Task;
            return result as T;
        }
        
        internal void Register(Type type, object service)
        {
            _services[type] = service;
            
            if (_pendingRequests.TryGetValue(type, out var requests))
            {
                foreach (var tcs in requests)
                {
                    tcs.TrySetResult(service);
                }
                _pendingRequests.Remove(type);
            }
            
            Debug.Log($"[ServiceLocator] Auto-registered: {type.Name}");
        }
        

        internal void Unregister(Type type)
        {
            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
                Debug.Log($"[ServiceLocator] Auto-unregistered: {type.Name}");
            }
        }
        
        public void Dispose()
        {
            _services.Clear();
            
            foreach (var requests in _pendingRequests.Values)
            {
                foreach (var tcs in requests)
                {
                    tcs.TrySetCanceled();
                }
            }
            _pendingRequests.Clear();
        }
    }
}