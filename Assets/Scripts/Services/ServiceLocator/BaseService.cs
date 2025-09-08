using UnityEngine;

namespace CardWar.Services
{

    public abstract class BaseService : MonoBehaviour
    {
        protected virtual void Awake()
        {
            RegisterService();
        }

        protected virtual void OnDestroy()
        {
            UnregisterService();
        }

        private void RegisterService()
        {
            var interfaces = GetType().GetInterfaces();
            foreach (var interfaceType in interfaces)
            {
                if (interfaceType != typeof(IBaseServiceProvider) &&
                    typeof(IBaseServiceProvider).IsAssignableFrom(interfaceType))
                {
                    ServiceLocator.Instance.Register(interfaceType, this);
                }
            }
        }

        private void UnregisterService()
        {
            var interfaces = GetType().GetInterfaces();
            foreach (var interfaceType in interfaces)
            {
                if (interfaceType != typeof(IBaseServiceProvider) &&
                    typeof(IBaseServiceProvider).IsAssignableFrom(interfaceType))
                {
                    ServiceLocator.Instance.Unregister(interfaceType);
                }
            }
        }
    }
}