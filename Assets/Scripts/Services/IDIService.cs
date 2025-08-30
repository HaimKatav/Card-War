using System;

namespace CardWar.Services
{
    public interface IDIService
    {
        T GetService<T>() where T : class;
        void RegisterService<T>(T service) where T : class;
    }
}