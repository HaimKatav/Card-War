using UnityEngine;

namespace CardWar.Services
{
    public interface IUIService : IBaseServiceProvider
    {
        GameObject GetGameAreaParent();
    }
}