using Chegevala.Core.EntityModel;
using Chegevala.Core.EntityModel.Models;
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
    public class MyService
    {
        #region 基础队列名称设置
        private const string MyServiceName = "Chegevala";
        private const string MyServiceLoginQueueName = "login";
        private const string MyServiceReceiveQueueName = "receive";
        #endregion
        private string loginQueueGuid=null;
        private string testQueueGuid = null;
        private string receiveQueueGuid=null;

        private RabbitMqProvider rabbitMqProvider = null;
        public MyService()
        {
            rabbitMqProvider = new RabbitMqProvider();
        }
        /// <summary>
        /// 创建RabbitMQ服务器连接
        /// </summary>
        /// <param name="requestHeartBeat"></param>
        /// <param name="networkRecoveryInterval"></param>
        /// <returns>创建成功为true，失败为false</returns>
        public bool RunConnection(TimeSpan requestHeartBeat, TimeSpan networkRecoveryInterval)
        {
            return rabbitMqProvider.ConstructMqConsumerConn(requestHeartBeat, networkRecoveryInterval);
        }
        #region 登录模块
        /// <summary>
        /// 创建登录模块
        /// </summary>
        /// <returns>创建成功为true，失败为false</returns>
        public bool RunLoginModule()
        {
            loginQueueGuid = rabbitMqProvider.ConstructSimpleQueueConsumerChannel(MyServiceLoginQueueName ,LoginModuleCallBack);
            testQueueGuid = rabbitMqProvider.ConstructTopicExchangeConsumerChannel("Message_Exchage", "test", "Service", null);
            if (loginQueueGuid != null)
            {
                rabbitMqProvider.Consume(loginQueueGuid);
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 接收到登录消息后的回调
        /// </summary>
        /// <param name="remoteMessage"></param>
        /// <returns></returns>
        private bool LoginModuleCallBack(RemoteMessage remoteMessage, BasicDeliverEventArgs args)
        {
            var param = remoteMessage.JsonContent.Split(" ");
            bool ret = false;
            string returnMessage="";
            if (param.Length != 2)
            {
                LogHelper.LogError($"用户{remoteMessage.SenderUID}请求登录参数异常");
            }
            else
            {
                var username = param[0];
                var password = param[1];
                LogHelper.Log($"{username} 请求登录。UID:{remoteMessage.SenderUID}；请求时间:{remoteMessage.Timestamp}；");
                try
                {
                    using (var db = new ChegevalaContext())
                    {
                        var user = db.Set<User>().Where(n => n.UserName == username).FirstOrDefault();
                        if (user == null)
                        {
                            //用户未注册，为其注册账号
                            db.Add(new User()
                            {
                                UserName = username,
                                PassWord = password
                            });
                            db.SaveChanges();
                            ret = true;
                            returnMessage = "登录成功，已经成功创建账号";
                        }
                        else
                        {
                            //用户已注册，判断密码是否正确
                            if (user.PassWord == password)
                            {
                                ret = true;
                                returnMessage = "登录成功";
                            }
                            else
                            {
                                ret = false;
                                returnMessage = "登录失败，密码错误";
                            }
                        }

                        var body = args.Body;
                        var props = args.BasicProperties;
                        var replyProps = rabbitMqProvider.ConnectChannel(testQueueGuid).ConsumerChannel.CreateBasicProperties();
                        rabbitMqProvider.Send(testQueueGuid, new RemoteMessage() { JsonContent = returnMessage }, props.ReplyTo);
                        if (ret)
                            LogHelper.Log($"用户{username}登录成功");
                        else
                            LogHelper.Log($"用户{username}登录失败");
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogError(e.Message);
                }

            }

            return true;
        }
        #endregion
        #region 消息处理模块
        /// <summary>
        /// 创建消息处理模块
        /// </summary>
        /// <returns></returns>
        public bool RunMessageModule()
        {
            loginQueueGuid = rabbitMqProvider.ConstructSimpleQueueConsumerChannel(MyServiceLoginQueueName, MessageModuleCallBack);
            if (loginQueueGuid != null)
            {
                rabbitMqProvider.Consume(loginQueueGuid);
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 接收到用户发送消息后的回调
        /// </summary>
        /// <param name="remoteMessage"></param>
        /// <returns></returns>
        private bool MessageModuleCallBack(RemoteMessage remoteMessage,BasicDeliverEventArgs args)
        {
            return true;
        }
        #endregion
        /// <summary>
        /// 为指定队列添加额外的消费者
        /// </summary>
        /// <param name="channelConsumer"></param>
        public void AddConsumer(string channelConsumer)
        {
            rabbitMqProvider.Consume(channelConsumer);
        }
    }
}
