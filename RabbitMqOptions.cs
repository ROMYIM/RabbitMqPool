using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMq
{
    public class RabbitMqOptions
    {
        public Server[] Servers { get; set; } = new Server[0];

        public Exchange[] Exchanges { get; set; } = new Exchange[0];

        public Queue[] Queues { get; set; } = new Queue[0];

        public IConfiguration Configuration { get; set; }
    }

    /// <summary>
    /// 连接RabbitMQ服务的基本参数
    /// </summary>
    public class Server
    {
        public string Name { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string HostName { get; set; }

        public string VirtualHost { get; set; }

        public int Port { get; set; }

        public int ConnectionInitializedCount { get; set; } = 1;

        public int ConnectionMax { get; set; }

        public ushort ChannelPerConnectionInitializedCount { get; set; }

        public ushort ChannelPerConnectionMax { get; set; } = 5;
    }

    /// <summary>
    /// 交换机的基本参数
    /// </summary>
    public class Exchange
    {
        public string ServerName { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public bool Durable { get; set; }

        public bool AutoDelete { get; set; }

        public string RouteKey { get; set; }

        public IDictionary<string, object> Arguements { get; set; }

        public Queue[] BindedQueues { get; set; } = new Queue[0];
    }

    /// <summary>
    /// 队列的基本参数
    /// </summary>
    public class Queue
    {
        public string ServerName { get; set; }

        public string Name { get; set; }

        public bool Durable { get; set; }

        public bool Exclusive { get; set; }

        public bool AutoDelete { get; set; }

        public IDictionary<string, object> Arguements { get; set; }
    }

}
