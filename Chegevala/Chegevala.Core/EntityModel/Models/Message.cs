using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Chegevala.Core.EntityModel.Models
{
    public class Message
    {
        [Key]
        public int ID { get; set; }
        public User Sender { get; set; }
        public User Receiver { get; set; }
        public DateTime CreateTime { get; set; }
        public string Content { get; set; }
        public bool Ack { get; set; }
    }
}
