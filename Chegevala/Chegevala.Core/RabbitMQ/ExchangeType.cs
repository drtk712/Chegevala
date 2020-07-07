using System;
using System.Collections.Generic;
using System.Text;

namespace Chegevala.Core.RabbitMQ
{
    /// <summary>
    /// 队列消息类型
    /// </summary>
    public enum ExchangeType
    {
        noexist,
        fanout,
        direct,
        topic,
        headers
    }
}
