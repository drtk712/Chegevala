using System;
using System.Collections.Generic;
using System.Text;

namespace Chegevala.Core.Utility
{
    public static class TpoicHelper
    {
        public static string UserTopic(string userName)
        {
            return "MyServer.All."+userName;
        }
        public static string GroupTopic(string groupName)
        {
            return "";
        }
        public static string ServerTopic()
        {
            return "MyService";
        }
    }
}
