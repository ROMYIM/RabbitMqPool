using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMq.Abtraction
{
    public interface IPool<T> : IDisposable where T : IDisposable
    {
        T Rent();

        void Return(T t);
    }
}
