using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Core.Context;
using CardWar.Services;
using Cysharp.Threading.Tasks;

namespace CardWar.Core.Mediator
{
    public sealed class GameMediator : IGameMediator
    {
        readonly Dictionary<Type, List<Delegate>> handlers = new();

        public void Subscribe<T>(Action<T> handler) where T : class
        {
            var type = typeof(T);
            if (!handlers.ContainsKey(type))
            {
                handlers[type] = new List<Delegate>();
            }

            handlers[type].Add(handler);
            Debug.Log($"[GameMediator] Subscribed to {type.Name}");
        }

        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            var type = typeof(T);
            if (handlers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    handlers.Remove(type);
                }

                Debug.Log($"[GameMediator] Unsubscribed from {type.Name}");
            }
        }

        public void Publish<T>(T notification) where T : class
        {
            var type = typeof(T);
            if (!handlers.TryGetValue(type, out var list))
            {
                return;
            }

            foreach (var handler in list.ToArray())
            {
                try
                {
                    if (handler is Action<T> typed)
                    {
                        typed(notification);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameMediator] Error handling {type.Name}: {ex.Message}");
                }
            }
        }

        public void Clear()
        {
            handlers.Clear();
            Debug.Log("[GameMediator] Cleared all handlers");
        }

        public void Dispose()
        {
            handlers.Clear();
        }
    }
}

