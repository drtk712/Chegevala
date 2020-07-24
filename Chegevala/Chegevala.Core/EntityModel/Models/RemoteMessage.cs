using Chegevala.Core.EntityModel.Models.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Chegevala.Core.EntityModel.Models
{
    public class RemoteMessage
    {
        public RemoteMessage()
        {
            this.CorrelationId = Guid.NewGuid();
            this.Timestamp = DateTime.Now;
        }
        [Key]
        public int ID { get; set; }
        /// <summary>
        /// 消息的唯一编号（用于某些需要应答反馈的消息)
        /// </summary>
        public Guid CorrelationId { get; set; }
        /// <summary>
        /// 消息产生的时间
        /// </summary>
        public DateTime Timestamp { get; set; }        
        /// <summary>
        /// 消息接收路径
        /// </summary>
        public string TopicRoute { get; set; }
        /// <summary>
        /// 消息发送者
        /// </summary>
        public string Sender { get; set; }
        /// <summary>
        /// 分布式事件或者数据内容（为通用起见，建议为Json格式字符串)
        /// </summary>
        public string JsonContent { get; set; }
        /// <summary>
        /// 是否需要可靠传输（如果为True，发送失败时会加入待发送队列等待重发或者超时放弃）
        /// </summary>
        public bool EnablePersistent { get; set; }
        public MessageType? MessageType { get; set; }
    }
}
