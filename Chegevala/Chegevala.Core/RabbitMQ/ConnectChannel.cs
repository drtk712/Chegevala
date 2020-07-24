using Chegevala.Core.EntityModel.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chegevala.Core.RabbitMQ
{
    public class ConnectChannel
    {
        public ConnectChannel()
        {
            ChannelGuid = Guid.NewGuid().ToString();
            CreateTime = DateTime.Now;
        }

        public string ChannelGuid { get; set; }

        public string ChannelName { get; set; }

        public DateTime CreateTime { get; set; }

        public IModel ConsumerChannel { get; set; }

        public string ExchangeName { get; set; }

        public ExchangeType ExchangeType { get; set; }

        public List<string> QueueNames { get; set; }

        public object UserData { get; set; }

        public Func<RemoteMessage, BasicDeliverEventArgs, bool> ReceiveMessageCallback { get; set; }

        public object AccessLock { get; set; }
    }
}
