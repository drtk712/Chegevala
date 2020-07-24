using System;
using System.Collections.Generic;
using System.Text;

namespace Chegevala.Core.Utility
{
    public static class TopicHelper
    {
        public static string UserTopic(string servicename,string userName)
        {
            return servicename + ".All."+userName;
        }
        public static string GroupTopic(string servicename,string groupName)
        {
            return servicename+"."+ groupName + ".*";
        }
        public static string ServerTopic(string servicename)
        {
            return servicename + ".#";
        }
    }
}
