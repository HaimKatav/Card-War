using System;
using System.Collections.Generic;

namespace CardWar.Services
{
    public interface IBaseServiceProvider{}
    
    public class ServiceLocator
    {
        private static ServiceLocator _instance;
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
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
        
        public void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            _services[type] = service;
        }
        
        public T Get<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
                return service as T;
            
            return null;
        }
        
        public void Clear()
        {
            _services.Clear();
        }
    }
}