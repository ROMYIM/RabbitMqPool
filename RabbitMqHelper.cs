using Core.Extensions;
using Microsoft.Extensions.Logging;
using RabbitMq.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMq
{
    public class RabbitMqHelper
    {
        private readonly RabbitMqOptions _oiptions;

        private readonly ILogger _logger;

        private readonly RabbitMqConnectionTable _table;

        public RabbitMqHelper(ILoggerFactory loggerFactory, RabbitMqOptions options, RabbitMqConnectionTable table)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _oiptions = options;
            _table = table;
        }

        public async Task SendMessageAsync(string serverName, string exchangeName, string routeKey, byte[] message, CancellationToken cancellationToken = default)
        {
            var connectionPool = await _table.GetConnection(serverName, cancellationToken);
            await Task.Run(() =>
            {
                var connection = connectionPool.Rent();
                connection.SendMessageAsync(exchangeName, routeKey, message);
                connectionPool.Return(connection);
            }, cancellationToken);
        }

        public void AddConnction(string serverName, Action configuration = default)
        {
            Check.NotNull(serverName, nameof(serverName));

            if (configuration == null)
            {
                _logger.LogInformation("the configuration is null.it will the default configuration from the rabbitmq options to config the connection");
            }

            _table.AddConnection(serverName, configuration);
        }
    }
}
