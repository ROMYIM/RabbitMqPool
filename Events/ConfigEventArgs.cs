using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMq.Events
{
    public class ConfigEventArgs : EventArgs
    {
        public RabbitMqOptions Options { get; set; }
    }
}
