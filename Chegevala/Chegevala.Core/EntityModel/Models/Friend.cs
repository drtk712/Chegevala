using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Chegevala.Core.EntityModel.Models
{
    public class Friend
    {
        [Key]
        public int ID { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
