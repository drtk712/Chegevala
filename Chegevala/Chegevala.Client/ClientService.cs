using Chegevala.Core;
using Chegevala.Core.EntityModel.Models;
using Chegevala.Core.EntityModel.Models.Enum;
using Chegevala.Core.RabbitMQ;
using Chegevala.Core.Utility;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chegevala.Client
{
    public class ClientService : CoreService
    {
        public string ClientUserName;
        public bool IsLogin = false;
        public bool Logining = false;
        public ClientService(TimeSpan requestHeartBeat, TimeSpan networkRecoveryInterval)
        {
            rabbitMqProvider = new RabbitMqProvider();
            if (rabbitMqProvider.ConstructMqConsumerConn(requestHeartBeat, networkRecoveryInterval))
            {
                //登录请求处理频道
                RunSimpleChannel(MyServiceLoginChannelName, MyServiceLoginQueueName, null);
                //消息接收处理频道
                RunSimpleChannel(MyServiceReceiveChannelName, MyServiceReceiveQueueName, null);
            }
        }
        public void Login(string username,string password)
        {
            if (!IsLogin)
            {
                if (!Logining)
                {
                    Logining = true;
                    ClientUserName = username;
                    //消息发送处理频道
                    RunExchangeConsumeChannel(MyServiceMessageChannelName, TopicHelper.UserTopic(MyServiceName, ClientUserName), ExchangeType.topic,
                        MyServiceMessageExchangeName, TopicHelper.UserTopic(MyServiceName, ClientUserName), MessageChannelCallBack);
                    
                    rabbitMqProvider.Send(MyServiceLoginChannelName, new RemoteMessage()
                    {
                        JsonContent = username + " " + password,
                        Sender = ClientUserName,
                        EnablePersistent =true,
                        MessageType=MessageType.Unknown,
                    }, queueName: MyServiceLoginQueueName);
                }
                else
                {
                    Console.WriteLine("正在登录中");
                }

            }
            else
            {
                Console.WriteLine("已经登录");
            }
        }

        public void SendMessage(string senderTo,string message)
        {
            if (IsLogin)
            {
                rabbitMqProvider.Send(MyServiceReceiveChannelName, new RemoteMessage()
                {
                    JsonContent = message,
                    Sender = ClientUserName,
                    TopicRoute = senderTo,
                    MessageType = MessageType.UserMessage,
                    EnablePersistent = true
                },MyServiceReceiveQueueName);
            }
            else
            {
                Console.WriteLine("尚未登陆");
            }
        }
        private bool MessageChannelCallBack(RemoteMessage remoteMessage, BasicDeliverEventArgs args)
        {
            switch (remoteMessage.MessageType)
            {
                case MessageType.LoginCallBack:
                    if (remoteMessage.JsonContent == "true")
                    {
                        Console.WriteLine("登录成功");
                        IsLogin = true;
                        Logining = false;
                    }
                    else
                    {
                        
                        Console.WriteLine("登录失败");
                        Logining = false;
                        rabbitMqProvider.DeleteQueue(MyServiceMessageChannelName, TopicHelper.UserTopic(MyServiceName, ClientUserName));
                    }
                    break;
                case MessageType.SystemMessage:
                    break;
                case MessageType.GroupMessage:
                    break;
                case MessageType.UserMessage:
                    Console.WriteLine(remoteMessage.JsonContent);
                    break;
            }

            return true;
        }
    }
}
