using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMq.Extensions
{
    public static class ConnectionExtension
    {
        public static Task SendMessageAsync(this RabbitMqConnection connection, string exchangeName, string routeKey, byte[] message)
        {
            return Task.Run(() =>
            {
                var channel = connection.Rent();
                if (!channel.SendMessage(exchangeName, routeKey, message))
                {
                    connection.Logger.LogError("send message failed");
                }
                connection.Return(channel);
            });

        }
    }
}
