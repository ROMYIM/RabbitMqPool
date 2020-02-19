using Core.Extensions;
using Microsoft.Extensions.Logging;
using RabbitMq.Abtraction;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMq
{
    public class RabbitMqConnectionTable : IDisposable
    {
        public class ConnectionPool : AbractPool<RabbitMqConnection>
        {
            private IConnectionFactory _connectionFactory;

            private readonly string _serverName;

            private Server _serverOptions;

            private readonly RabbitMqOptions _options;

            public readonly Task _initializedTask;

            public IConnectionFactory ConnectionFactory { get => _connectionFactory; }

            public RabbitMqOptions Options { get => _options; }

            internal ConnectionPool(string serverName, RabbitMqOptions options, Action initialize = default)
            {
                Check.NotNull(serverName, nameof(serverName));
                Check.NotNull(options, nameof(options));

                _serverName = serverName;
                _options = options;
                _logger = LoggerFactory.CreateLogger(GetType());
                initialize ??= OnInitialize;
                _initializedTask = Task.Run(initialize);
            }

            protected virtual void OnInitialize()
            {
                var serverOption = _options.Servers.First(s => s.Name == _serverName);
                if (serverOption == null)
                {
                    throw new ArgumentNullException(nameof(serverOption));
                }
                else
                {
                    _serverOptions = serverOption;
                }

                _maxSize = serverOption.ConnectionMax;
                _connectionFactory = new ConnectionFactory()
                {

                    UserName = serverOption.Username,
                    Password = serverOption.Password,
                    HostName = serverOption.HostName,
                    //Port = serverOption.Port,
                    //VirtualHost = serverOption.VirtualHost,
                    //RequestedChannelMax = serverOption.ChannelPerConnectionMax
                };

                for (int i = 0; i < serverOption.ConnectionInitializedCount; i++)
                {
                    if (++_createdCount <= _maxSize)
                    {
                        _pool.Enqueue(new RabbitMqConnection(this, serverOption.ChannelPerConnectionMax));
                    }
                    else
                    {
                        --_createdCount;
                        break;
                    }
                    
                }

                var connection = Rent();
                connection.ExchangesDeclare(serverOption.Name);
                connection.QueuesDeclare(serverOption.Name);
                Return(connection);
            }


            public override void Dispose()
            {
                if (!_disposed)
                {
                    base.Dispose();
                    _disposed = true;
                }
            }

            public override RabbitMqConnection Rent()
            {
                if (_pool.TryDequeue(out RabbitMqConnection connection))
                {
                    return connection;
                }

                else if (Interlocked.Increment(ref _createdCount) <= _maxSize)
                {
                    connection = new RabbitMqConnection(this, _serverOptions.ChannelPerConnectionMax);
                    connection.SetPool(this);
                    return connection;
                }

                else
                {
                    Interlocked.Decrement(ref _createdCount);
                    return default;
                }
            }

            public override void Return(RabbitMqConnection t)
            {
                var currentCount = Count;
                if (Interlocked.Increment(ref currentCount) <= _maxSize)
                {
                    _pool.Enqueue(t);
                }
                else
                {
                    _logger.LogError("the connection can't return the pool.Maybe the pool is full");
                    throw new ArgumentOutOfRangeException(nameof(t));
                }
            }
        }

        private readonly IDictionary<string, ConnectionPool> _connectionTable = new ConcurrentDictionary<string, ConnectionPool>();

        private readonly RabbitMqOptions _options;

        private readonly ILogger _logger;

        private bool _disposed;

        public static ILoggerFactory LoggerFactory { get; private set; }

        public RabbitMqConnectionTable(RabbitMqOptions options, ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
            _options = options;
            _logger = LoggerFactory.CreateLogger(GetType());


            foreach (var serverOptions in options.Servers)
            {
                AddConnection(serverName: serverOptions.Name);
            }
        }

        public void AddConnection(string serverName, Action initializeAction = default)
        {
            _connectionTable.Add(serverName, new ConnectionPool(serverName, _options, initializeAction));
        }

        internal async Task<ConnectionPool> GetConnection(string serverName, CancellationToken cancellationToken = default)
        {
            if (_connectionTable.TryGetValue(serverName, out ConnectionPool pool))
            {
                if (!pool._initializedTask.IsCompleted)
                {
                    await pool._initializedTask;
                }
                return pool;
            }
            else
            {
                _logger.LogError("there is not matched connection in the connection table");
                throw new MissingMemberException(nameof(serverName));
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _connectionTable.Values.ForEach(c => c.Dispose());
                _connectionTable.Clear();
                _disposed = true;
            }
        }

        
    }
}
