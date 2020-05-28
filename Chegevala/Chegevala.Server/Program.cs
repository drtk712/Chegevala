using System;

namespace Chegevala.Server
{
    class Program
    {
        public static MyServer myServer = new MyServer();
        static void Main(string[] args)
        {
            //进程退出事件
            AppDomain.CurrentDomain.ProcessExit += (a, b) =>
            {
                myServer.Close();
            };
            Console.WriteLine("Chegevala server is running.");
            Console.WriteLine("Press [enter] to close.");
            Console.ReadLine();
            myServer.Close();
        }
    }
}
