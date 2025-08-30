using System;
using UnityEngine;
using Zenject;
using CardWar.Services;

namespace CardWar.Core
{
    public class SimpleDIService : IDIService
    {
        private DiContainer _container;
        
        [Inject]
        public void Construct(DiContainer container)
        {
            _container = container;
            Debug.Log($"[SimpleDIService] Initialized with Zenject container");
        }
        
        public T GetService<T>() where T : class
        {
            try
            {
                return _container.Resolve<T>();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleDIService] Failed to resolve {typeof(T).Name}: {e.Message}");
                return null;
            }
        }
        
        public void RegisterService<T>(T service) where T : class
        {
            _container.Bind<T>().FromInstance(service).AsSingle();
            Debug.Log($"[SimpleDIService] Registered service: {typeof(T).Name}");
        }
    }
}