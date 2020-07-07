using Chegevala.Core.EntityModel.Models;
using Chegevala.Core.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;

namespace Chegevala.Core.EntityModel
{
    public class ChegevalaContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //注入Sql链接字符串
            optionsBuilder.UseMySql(ConfigurationHelper.Instance.Configuration.GetConnectionString("Default"));
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
    }
}
