using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardWar.Core.Mediator
{
    public sealed class GameMediator : IGameMediator
    {
        readonly Dictionary<Type, List<Delegate>> handlers;
        bool disposed;

        public GameMediator()
        {
            handlers = new Dictionary<Type, List<Delegate>>();
        }

        public void Subscribe<T>(Action<T> handler) where T : class
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(GameMediator));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var type = typeof(T);

            if (!handlers.ContainsKey(type))
                handlers[type] = new List<Delegate>();

            handlers[type].Add(handler);
            Debug.Log($"[GameMediator] Subscribed handler for {type.Name}");
        }

        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(GameMediator));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var type = typeof(T);

            if (handlers.ContainsKey(type))
            {
                handlers[type].Remove(handler);

                if (handlers[type].Count == 0)
                    handlers.Remove(type);

                Debug.Log($"[GameMediator] Unsubscribed handler for {type.Name}");
            }
        }

        public void Publish<T>(T notification) where T : class
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(GameMediator));

            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            var type = typeof(T);

            if (handlers.ContainsKey(type))
            {
                var list = new List<Delegate>(handlers[type]);

                foreach (var handler in list)
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

                Debug.Log($"[GameMediator] Published {type.Name} to {list.Count} handlers");
            }
            else
            {
                Debug.Log($"[GameMediator] No handlers registered for {type.Name}");
            }
        }

        public void Clear()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(GameMediator));

            handlers.Clear();
            Debug.Log("[GameMediator] Cleared all handlers");
        }

        public void Dispose()
        {
            if (disposed)
                return;

            Clear();
            disposed = true;
            Debug.Log("[GameMediator] Disposed");
        }
    }
}

