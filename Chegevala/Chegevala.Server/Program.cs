using System;

namespace Chegevala.Server
{
    class Program
    {
        //public static MyServer myServer;
        static void Main(string[] args)
        {
            //myServer=new MyServer();
            ////进程退出事件
            //AppDomain.CurrentDomain.ProcessExit += (a, b) =>
            //{
            //    myServer.Close();
            //};
            MyService myService = new MyService(new TimeSpan(0, 0, 5), new TimeSpan(0, 0, 30));
            Console.WriteLine("Chegevala server is running.");
            Console.WriteLine("Press [enter] to close.");
            Console.ReadLine();
            //myServer.Close();
        }
    }
}
