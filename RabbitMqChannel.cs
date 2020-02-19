using Core.Extensions;
using Microsoft.Extensions.Logging;
using RabbitMq.Abtraction;
using RabbitMq.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMq
{
    public class RabbitMqChannel : IDisposable, IPoolable
    {
        private RabbitMqConnection _connection;

        private IModel _channel;

        private bool _disposed;

        private readonly RabbitMqOptions _options;

        private readonly ILogger _logger;

        public IModel Channel { get => _channel; }
        /// <summary>
        /// 通道配置的事件回调。可以由用户自己配置再进行与mq服务的消息传递。
        /// 调用<see cref="HandleChannelConfig(RabbitMqOptions)"/>使配置生效。
        /// </summary>
        public EventHandler<ConfigEventArgs> ChannelConfiged { get; set; }

        public bool Configed { get; private set; }

        public RabbitMqChannel(RabbitMqConnection connection)
        {
            Check.NotNull(connection, nameof(connection));

            _logger = RabbitMqConnectionTable.LoggerFactory.CreateLogger(GetType());
            _channel = connection.Connection?.CreateModel();
            _options = connection.Options;
            SetPool(connection);
        }

        public Task<bool> SendAndDeleteAsync(Exchange exchange, Queue queue, byte[] body, IBasicProperties properties = default, bool mandatory = default)
        {
            if (Channel.IsOpen)
            {
                Channel.ExchangeDeclare(exchange.Name, exchange.Type, exchange.Durable, exchange.AutoDelete, null);
                Channel.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, null);
                Channel.QueueBind(queue.Name, exchange.Name, exchange.RouteKey, null);
                Channel.BasicPublish(exchange.Name, exchange.RouteKey, mandatory, properties, body);
                return Task.Run(() =>
                {
                    
                    var result = Channel.WaitForConfirms();
                    Channel.QueueDelete(queue.Name, true, true);
                    Channel.ExchangeDelete(exchange.Name, true);
                    return result;
                });
            }
            else
            {
                _logger.LogInformation("class-id:{0}\nreply-code:{1}\nreply-text:{2}", Channel.CloseReason.ClassId, Channel.CloseReason.ReplyCode, Channel.CloseReason.ReplyText);
                return Task.FromResult(false);
            }
        }

        public bool SendMessage(string exchangeName, string routeKey, byte[] body,IBasicProperties properties = default, bool mandatory = default)
        {
            if (Channel.IsOpen)
            {
                Channel.BasicPublish(exchangeName, routeKey, mandatory, properties, body);
                return Channel.WaitForConfirms();
            }
            else
            {
                _logger.LogInformation("class-id:{0}\nreply-code:{1}\nreply-text:{2}", Channel.CloseReason.ClassId, Channel.CloseReason.ReplyCode, Channel.CloseReason.ReplyText);
                return false;
            }
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                _channel?.Close();
                _channel = null;
                _connection = null;
                _disposed = true;
            }
        }

        public virtual void SetPool(object pool)
        {
            Check.NotNull(pool, nameof(pool));
            Check.IsType<RabbitMqConnection>(pool, nameof(pool));

            _connection = pool as RabbitMqConnection;
        }

        /// <summary>
        /// 执行通道(channel)通道配置事件。调用事件<see cref="ChannelConfiged"/>，并设置<see cref="Configed"/>为true
        /// </summary>
        /// <param name="options">配置选项</param>
        /// <seealso cref="ChannelConfiged"/>
        public void HandleChannelConfig(RabbitMqOptions options)
        {
            Check.NotNull(options, nameof(options));

            var source = new ConfigEventArgs()
            {
                Options = options
            };

            if (ChannelConfiged != null)
            {
                ChannelConfiged(this, source);
                Configed = true;
            }
        }
    }
}
