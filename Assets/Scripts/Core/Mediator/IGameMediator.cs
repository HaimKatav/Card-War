using System;

namespace CardWar.Core.Mediator
{
    public interface IGameMediator : IDisposable
    {
        void Subscribe<T>(Action<T> handler) where T : class;
        void Unsubscribe<T>(Action<T> handler) where T : class;
        void Publish<T>(T notification) where T : class;
        void Clear();
    }
}
