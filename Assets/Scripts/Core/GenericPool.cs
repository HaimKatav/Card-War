using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CardWar.Core
{
    public class GenericPool<T> where T : Component
    {
        private T _prefab;
        private Transform _container;
        private Queue<T> _pool = new();
        private HashSet<T> _active = new();
        
        public int ItemsInPool => _pool.Count;
        
        public GenericPool(T prefab, Transform container, int initialSize = 10)
        {
            _prefab = prefab;
            _container = container;
            
            for (var i = 0; i < initialSize; i++)
            {
                CreatePooledItem();
            }
        }
        
        public T Get()
        {
            var item = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
            
            item.gameObject.SetActive(true);
            _active.Add(item);
            
            return item;
        }
        
        public void Return(T item)
        {
            if (item == null || !_active.Contains(item))
                return;
            
            _active.Remove(item);
            item.gameObject.SetActive(false);
            item.transform.SetParent(_container);
            _pool.Enqueue(item);
        }
        
        public void ReturnAll()
        {
            var itemsToReturn = new List<T>(_active);
            foreach (var item in itemsToReturn)
            {
                Return(item);
            }
        }

        public void Dispose()
        {
            _prefab = null;
            _container = null;

            if (_pool != null)
            {
                foreach (var component in _pool)
                {
                    Object.Destroy(component.gameObject);
                }

                _pool.Clear();
            }
            
            _active.Clear();
        }
        
        private void CreatePooledItem()
        {
            var item = CreateNew();
            item.gameObject.SetActive(false);
            _pool.Enqueue(item);
        }
        
        private T CreateNew()
        {
            var item = Object.Instantiate(_prefab, _container);
            return item;
        }
    }
}