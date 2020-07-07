using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chegevala.Core.RabbitMQ
{
    public class ConnectChannel
    {
        public string ChannelGuid { get; set; }

        public IModel ConsumerChannel { get; set; }

        public string ExchangeName { get; set; }

        public ExchangeType ExchangeType { get; set; }

        public string QueueName { get; set; }

        public object UserData { get; set; }

        public Func<RemoteMessage, bool> ReceiveMessageCallback { get; set; }

        public object AccessLock { get; set; }
    }
}
