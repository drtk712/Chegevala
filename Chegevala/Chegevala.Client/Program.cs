using Chegevala.Core.RabbitMQ;
using RabbitMQ.Client.Events;
using System;
using System.Linq;
using System.Text;

namespace Chegevala.Client
{
    class Program
    {
        //public static MyClient client;
        static void Main(string[] args)
        {
            //client = new MyClient();   
            ////进程退出事件
            //AppDomain.CurrentDomain.ProcessExit += (a, b) =>
            //{
            //    client.Close();
            //};


            //Console.WriteLine("Chegevala client is running.");


            //while (true)
            //{
            //    if (client.currentUser == null)
            //    {
            //        Console.Write("NoLogin >");
            //    }
            //    else
            //    {
            //        Console.Write(client.currentUser.UserName + " >");
            //    }
            //    MenuHelpper(Console.ReadLine());
            //}

            RabbitMqProvider rabbitMqProvider = new RabbitMqProvider();
            if (rabbitMqProvider.ConstructMqConsumerConn(new TimeSpan(0, 0, 10), new TimeSpan(0, 0, 10)))
            {
                var channelguid = rabbitMqProvider.ConstructSimpleQueueConsumerChannel( "login",  null);

                var channelguid2 = rabbitMqProvider.ConstructTopicExchangeConsumerChannel("Message_Exchange", "dr", "Service", test);
                rabbitMqProvider.Consume(channelguid2);

                rabbitMqProvider.ConnectChannel(channelguid2).ConsumerChannel.BasicPublish()
                rabbitMqProvider.Send(channelguid, new RemoteMessage() { JsonContent = "dr 123"});

                Console.ReadLine();
            }

        }

        public static bool test(RemoteMessage message, BasicDeliverEventArgs args)
        {
            Console.WriteLine(message.JsonContent);
            Console.WriteLine(args.BasicProperties.ReplyTo);
            Console.WriteLine(args.BasicProperties.CorrelationId);
            return true;
        }

        //static void MenuHelpper(string command)
        //{
        //    if (command == null || command.Length == 0)
        //    {
        //        Console.WriteLine("请输入指令,键入help查看帮助");
        //        return;
        //    }
        //    var commandlist = command.ToLower().Split(" ").ToList();
        //    switch(commandlist.First())
        //    {
        //        case "help": PrintHelp(); break;
        //        case "login": Login(command); break;
        //        case "exit": client.Close() ; ; break;
        //        default: Send(command); break;
        //    }
        //}
        //static void PrintHelp()
        //{
        //    Console.WriteLine("1.help ----------------------------------------->查看帮助");
        //    Console.WriteLine("2.login <username> <password> ------------------>登录");
        //    Console.WriteLine("3.<username> <message> ------------------------->发送信息");
        //    Console.WriteLine("4.exit ----------------------------------------->退出");
        //}
        //static void Login(string command)
        //{
        //    var commandlist= command.Split(" ").ToList();
        //    if (commandlist.Count == 3)
        //    {
        //        client.Login(commandlist[1], commandlist[2]);
        //    }
        //    else
        //    {
        //        Console.WriteLine("指令使用错误,键入help查看帮助");
        //    }
        //}
        //static void Send(string command)
        //{
        //    var commandlist = command.Split(" ").ToList();
        //    if (commandlist.Count >= 2)
        //    {
        //        var receiver = commandlist.First();
        //        commandlist.RemoveAt(0);
        //        var content = string.Join(" ", commandlist);
        //        client.SendMessage(receiver, content);
        //    }
        //    else
        //    {
        //        Console.WriteLine("指令使用错误,键入help查看帮助");
        //    }
        //}
    }
}
