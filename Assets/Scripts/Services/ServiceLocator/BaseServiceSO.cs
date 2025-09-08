using System;
using UnityEngine;
using CardWar.Services;

namespace CardWar.Services
{
    public abstract class BaseServiceSO : ScriptableObject
    {
        private bool _isRegistered = false;

        public virtual void Initialize()
        {
            RegisterAsService();
        }
        
        public void RegisterAsService()
        {
            if (_isRegistered) return;
            
            var interfaces = GetType().GetInterfaces();
            
            if (interfaces.Length == 0 || 
                (interfaces.Length == 1 && interfaces[0] == typeof(IBaseServiceProvider)))
            {
                ServiceLocator.Instance.Register(GetType(), this);
                _isRegistered = true;
                Debug.Log($"[ServiceLocator] Registered ScriptableObject by type: {GetType().Name}");
                return;
            }
            
            foreach (var interfaceType in interfaces)
            {
                if (interfaceType != typeof(IBaseServiceProvider) && 
                    typeof(IBaseServiceProvider).IsAssignableFrom(interfaceType))
                {
                    ServiceLocator.Instance.Register(interfaceType, this);
                    _isRegistered = true;
                    Debug.Log($"[ServiceLocator] Registered ScriptableObject: {interfaceType.Name}");
                }
            }
            
            if (!_isRegistered)
            {
                ServiceLocator.Instance.Register(GetType(), this);
                _isRegistered = true;
                Debug.Log($"[ServiceLocator] Registered ScriptableObject by type: {GetType().Name}");
            }
        }

        private void UnregisterAsService()
        {
            if (!_isRegistered) return;
            
            var interfaces = GetType().GetInterfaces();
            
            if (interfaces.Length == 0 || 
                (interfaces.Length == 1 && interfaces[0] == typeof(IBaseServiceProvider)))
            {
                ServiceLocator.Instance.Unregister(GetType());
                _isRegistered = false;
                return;
            }
            
            foreach (var interfaceType in interfaces)
            {
                if (interfaceType != typeof(IBaseServiceProvider) && 
                    typeof(IBaseServiceProvider).IsAssignableFrom(interfaceType))
                {
                    ServiceLocator.Instance.Unregister(interfaceType);
                }
            }
            
            if (_isRegistered)
            {
                ServiceLocator.Instance.Unregister(GetType());
            }
            
            _isRegistered = false;
            Debug.Log($"[ServiceLocator] Unregistered ScriptableObject: {GetType().Name}");
        }
        
        protected virtual void OnDestroy()
        {
            UnregisterAsService();
        }
    }
}
