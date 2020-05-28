using System;
using System.Collections.Generic;
using System.Text;

namespace Chegevala.Core.RabbitMQ.BindIngModel
{
    public class MessageBindingModel
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Content { get; set; }
    }
}
