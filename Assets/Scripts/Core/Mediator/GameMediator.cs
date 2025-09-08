using System;
using System.Collections.Generic;
using UnityEngine;
using CardWar.Core.Context;
using CardWar.Services;
using Cysharp.Threading.Tasks;

namespace CardWar.Core.Mediator
{
    public class GameMediator : IGameMediator
    {
        readonly Dictionary<Type, List<Delegate>> handlers = new();

        public async UniTask PublishAsync<T>(T notification) where T : IGameNotification
        {
            var type = typeof(T);

            if (!handlers.TryGetValue(type, out var typeHandlers))
            {
                return;
            }

            foreach (var handler in typeHandlers.ToArray())
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
                    Debug.LogError($"[GameMediator] Error handling notification {type.Name}: {ex.Message}");
                }
            }

            await UniTask.Yield();
        }

        public void Subscribe<T>(Action<T> handler) where T : IGameNotification
        {
            var type = typeof(T);

            if (!handlers.ContainsKey(type))
            {
                handlers[type] = new List<Delegate>();
            }

            handlers[type].Add(handler);
            Debug.Log($"[GameMediator] Subscribed to {type.Name}");
        }

        public void Unsubscribe<T>(Action<T> handler) where T : IGameNotification
        {
            var type = typeof(T);

            if (handlers.TryGetValue(type, out var typeHandlers))
            {
                typeHandlers.Remove(handler);
                Debug.Log($"[GameMediator] Unsubscribed from {type.Name}");
            }
        }
    }
}
