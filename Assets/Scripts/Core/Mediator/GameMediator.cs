using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardWar.Core.Mediator
{
    public sealed class GameMediator : IGameMediator
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers;
        private bool _disposed;

        public GameMediator()
        {
            _handlers = new Dictionary<Type, List<Delegate>>();
        }

        public void Subscribe<T>(Action<T> handler) where T : class
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameMediator));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var type = typeof(T);

            if (!_handlers.ContainsKey(type))
                _handlers[type] = new List<Delegate>();

            _handlers[type].Add(handler);
            Debug.Log($"[GameMediator] Subscribed handler for {type.Name}");
        }

        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameMediator));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var type = typeof(T);

            if (_handlers.ContainsKey(type))
            {
                _handlers[type].Remove(handler);

                if (_handlers[type].Count == 0)
                    _handlers.Remove(type);

                Debug.Log($"[GameMediator] Unsubscribed handler for {type.Name}");
            }
        }

        public void Publish<T>(T notification) where T : class
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameMediator));

            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            var type = typeof(T);

            if (_handlers.ContainsKey(type))
            {
                var handlers = new List<Delegate>(_handlers[type]);

                foreach (var handler in handlers)
                {
                    try
                    {
                        ((Action<T>)handler)?.Invoke(notification);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[GameMediator] Error publishing {type.Name}: {ex.Message}");
                    }
                }

                Debug.Log($"[GameMediator] Published {type.Name} to {handlers.Count} handlers");
            }
            else
            {
                Debug.Log($"[GameMediator] No handlers registered for {type.Name}");
            }
        }

        public void Clear()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameMediator));

            _handlers.Clear();
            Debug.Log("[GameMediator] Cleared all handlers");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Clear();
            _disposed = true;
            Debug.Log("[GameMediator] Disposed");
        }
    }
}
