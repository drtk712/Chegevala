using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Chegevala.Core.EntityModel.Models
{
    public class User
    {
        [Key]
        public int ID { get; set; }
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public bool Online { get; set; }
        public List<Friend> Friends { get; set; }
    }
}
