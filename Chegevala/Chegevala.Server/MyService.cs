using Chegevala.Core;
using Chegevala.Core.EntityModel;
using Chegevala.Core.EntityModel.Models;
using Chegevala.Core.EntityModel.Models.Enum;
using Chegevala.Core.RabbitMQ;
using Chegevala.Core.Utility;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Chegevala.Server
{
    public class MyService : CoreService
    {


        public MyService(TimeSpan requestHeartBeat, TimeSpan networkRecoveryInterval)
        {
            rabbitMqProvider = new RabbitMqProvider();
            if (rabbitMqProvider.ConstructMqConsumerConn(requestHeartBeat, networkRecoveryInterval))
            {
                //登录请求处理频道
                RunSimpleConsumeChannel(MyServiceLoginChannelName, MyServiceLoginQueueName, LoginChannelCallBack);
                //消息接收处理频道
                RunSimpleConsumeChannel(MyServiceReceiveChannelName, MyServiceReceiveQueueName, ReceiveChannelCallBack);
                //消息发送处理频道
                RunExchangeChannel(MyServiceMessageChannelName, MyServiceMessageQueueName, ExchangeType.topic, 
                    MyServiceMessageExchangeName, TopicHelper.UserTopic(MyServiceName, MyServiceName), MessageChannelCallBack);
            }
        }
        private bool MessageChannelCallBack(RemoteMessage remoteMessage, BasicDeliverEventArgs args)
        {
            Console.WriteLine(remoteMessage.JsonContent);
            return true;
        }
        private bool ReceiveChannelCallBack(RemoteMessage remoteMessage,BasicDeliverEventArgs args)
        {
            switch (remoteMessage.MessageType)
            {
                case MessageType.UserMessage:
                    rabbitMqProvider.Send(MyServiceMessageChannelName, new RemoteMessage()
                    {
                        JsonContent = remoteMessage.JsonContent,
                        Sender = remoteMessage.Sender,
                        TopicRoute = TopicHelper.UserTopic(MyServiceName, remoteMessage.TopicRoute),
                        MessageType = remoteMessage.MessageType,
                        EnablePersistent = true
                    });
                    break;
                case MessageType.GroupMessage:
                    break;
            }

            return true;
        }
        private bool LoginChannelCallBack(RemoteMessage remoteMessage,BasicDeliverEventArgs args)
        {
            var param = remoteMessage.JsonContent.Split(" ");
            bool ret = false;
            string returnMessage = "true";
            if (param.Length != 2)
            {
                LogHelper.LogError($"用户{remoteMessage.Sender}请求登录参数异常");
            }
            else
            {
                var username = param[0];
                var password = param[1];
                LogHelper.Log($"{username} 请求登录。UID:{remoteMessage.Sender}；请求时间:{remoteMessage.Timestamp}；");
                try
                {
                    //using (var db = new ChegevalaContext())
                    //{
                        //var user = db.Set<User>().Where(n => n.UserName == username).FirstOrDefault();
                        //if (user == null)
                        //{
                        //    //用户未注册，为其注册账号
                        //    db.Add(new User()
                        //    {
                        //        UserName = username,
                        //        PassWord = password
                        //    });
                        //    db.SaveChanges();
                        //    ret = true;
                        //    returnMessage = "登录成功，已经成功创建账号";
                        //}
                        //else
                        //{
                        //    //用户已注册，判断密码是否正确
                        //    if (user.PassWord == password)
                        //    {
                        //        ret = true;
                        //        returnMessage = "登录成功";
                        //    }
                        //    else
                        //    {
                        //        ret = false;
                        //        returnMessage = "登录失败，密码错误";
                        //    }
                        //}
                        rabbitMqProvider.Send(MyServiceMessageChannelName, new RemoteMessage() 
                        {
                            JsonContent = returnMessage ,
                            TopicRoute=TopicHelper.UserTopic(MyServiceName,remoteMessage.Sender),
                            Sender=MyServiceName,
                            MessageType=MessageType.LoginCallBack,
                            Timestamp=DateTime.Now,
                            EnablePersistent=false
                        });
                        if (ret)
                            LogHelper.Log($"用户{username}登录成功");
                        else
                            LogHelper.Log($"用户{username}登录失败");
                    //}
                }
                catch (Exception e)
                {
                    LogHelper.LogError(e.Message);
                }

            }

            return true;
        }
    }
}
