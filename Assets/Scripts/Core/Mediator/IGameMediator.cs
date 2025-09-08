using System;
using Cysharp.Threading.Tasks;

namespace CardWar.Core.Mediator
{
    public interface IGameMediator
    {
        UniTask PublishAsync<T>(T notification) where T : IGameNotification;
        void Subscribe<T>(Action<T> handler) where T : IGameNotification;
        void Unsubscribe<T>(Action<T> handler) where T : IGameNotification;
    }

    public interface IGameNotification { }
}
