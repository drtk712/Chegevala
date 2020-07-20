using System;
using System.Collections.Generic;
using System.Text;

namespace Chegevala.Core.Utility
{
    public static class LogHelper
    {
        public static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now}]---Normal");
            Console.WriteLine(message);
        }
        public static void LogWarning(string message)
        {
            Console.WriteLine($"[{DateTime.Now}]---Warning");
            Console.WriteLine(message);
        }

        public static void LogError(string message)
        {
            Console.WriteLine($"[{DateTime.Now}]---Error");
            Console.WriteLine(message);
        }
    }
}
