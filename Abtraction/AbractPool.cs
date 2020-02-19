using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace RabbitMq.Abtraction
{
    public abstract class AbractPool<T> : IPool<T> where T : IPoolable
    {
        protected readonly ConcurrentQueue<T> _pool = new ConcurrentQueue<T>();

        protected int _maxSize;

        protected int _createdCount;

        protected bool _disposed;

        protected ILogger _logger;

        protected virtual int Count { get => _pool.Count; }

        public virtual void Dispose()
        {
            if (!_pool.IsEmpty)
            {
                while (_pool.TryDequeue(out T item))
                {
                    item?.Dispose();
                }
            }
        }

        public abstract T Rent();

        public abstract void Return(T t);
    }
}
