using System;
using System.Collections.Generic;
using System.Text;

namespace Chegevala.Core.RabbitMQ
{
    public class RemoteMessage
    {
        public RemoteMessage()
        {
            EnableReliableSession = true;
            this.ID = Guid.NewGuid();
            this.Timestamp = DateTime.Now;
        }
        /// <summary>
        /// 消息的唯一编号（用于某些需要应答反馈的消息)
        /// </summary>
        public Guid ID { get; set; }
        /// <summary>
        /// 消息产生的时间
        /// </summary>
        public DateTime Timestamp { get; set; }        
        /// <summary>
        /// 消息发布路径-也意味着消息类型(接收方可利用通配符匹配此路径，实现仅接收特定路径下的消息）
        /// </summary>
        public string TopicRoute { get; set; }
        /// <summary>
        /// 触发事件者的唯一标识（例如可以用客户端IP，代表事件是在某个IP客户端触发的）
        /// </summary>
        public string SenderUID { get; set; }
        /// <summary>
        /// 分布式事件或者数据内容（为通用起见，建议为Json格式字符串)
        /// </summary>
        public string JsonContent { get; set; }
        /// <summary>
        /// 是否需要可靠传输（如果为True，发送失败时会加入待发送队列等待重发或者超时放弃）
        /// </summary>
        public bool EnableReliableSession { get; set; }
    }
}
