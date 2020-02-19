using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMq.Abtraction
{
    public interface IPoolable : IDisposable
    {
        void SetPool(object pool);
    }
}
