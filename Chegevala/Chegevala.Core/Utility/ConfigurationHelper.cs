using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Chegevala.Core.Utility
{
    public class ConfigurationHelper
    {
        #region 单例模式
        private static ConfigurationHelper _instance;
        private static object _lock = new object();
        public static ConfigurationHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConfigurationHelper();

                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        private IConfiguration configuration;
        public IConfiguration Configuration
        {
            get { return configuration; }
        }
        public ConfigurationHelper()
        {
            configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
        }
    }
}
