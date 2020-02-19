using Core.Extensions;
using Microsoft.Extensions.Logging;
using RabbitMq.Abtraction;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.Impl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static RabbitMq.RabbitMqConnectionTable;

namespace RabbitMq
{
    public class RabbitMqConnection : AbractPool<RabbitMqChannel>, IPoolable
    {
        private IConnection _connection;

        private ConnectionPool _connectionPool;

        private readonly RabbitMqOptions _options;

        public RabbitMqOptions Options { get => _options; }

        public IConnection Connection { get => _connection; }

        public ILogger Logger { get => _logger; }

        public RabbitMqConnection(ConnectionPool parentPool, ushort channleMax = 50)
        {
            Check.NotNull(parentPool, nameof(parentPool));

            _options = parentPool.Options;
            _connection = parentPool.ConnectionFactory?.CreateConnection() ?? throw new ArgumentNullException(nameof(parentPool));
            _logger = RabbitMqConnectionTable.LoggerFactory.CreateLogger(GetType());
            _maxSize = channleMax;
            SetPool(parentPool);
        }

        public RabbitMqChannel CreateChannel()
        {
            var channel = new RabbitMqChannel(this);
            return channel;
        }

        public void ExchangesDeclare(string serverName)
        {
            var channel = Rent();
            var model = channel.Channel;
            foreach (var exchange in _options.Exchanges.Where(e => e.ServerName == serverName))
            {
                model.ExchangeDeclare(exchange.Name, exchange.Type, exchange.Durable, exchange.AutoDelete, exchange.Arguements);
                foreach (var queue in exchange.BindedQueues)
                {
                    model.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguements);
                    model.QueueBind(queue.Name, exchange.Name, exchange.RouteKey, exchange.Arguements);
                }
            }
            Return(channel);
        }

        public void QueuesDeclare(string serverName)
        {
            var channel = Rent();
            var model = channel.Channel;
            foreach (var queue in _options.Queues.Where(q => q.ServerName == serverName))
            {
                model.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguements);
            }
            Return(channel);
        }

        public override RabbitMqChannel Rent()
        {
            if (_pool.TryDequeue(out RabbitMqChannel channel))
            {
                return channel;
            }

            else if (Interlocked.Increment(ref _createdCount) <= _maxSize)
            {
                channel = CreateChannel();
                channel.SetPool(this);
                return channel;
            }

            else
            {
                Interlocked.Decrement(ref _createdCount);
                return default;
            }
        }

        public override void Return(RabbitMqChannel model)
        {
            int currentCount = Count;
            if (Interlocked.Increment(ref currentCount) <= _maxSize)
            {
                _pool.Enqueue(model);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(model));
            }
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                base.Dispose();
                _connection?.Close();

                _connection = null;
                _disposed = true;
            }
        }

        public virtual void SetPool(object pool)
        {
            Check.NotNull(pool, nameof(pool));
            Check.IsType<ConnectionPool>(pool, nameof(pool));

            _connectionPool = pool as ConnectionPool;
        }
    }
}
