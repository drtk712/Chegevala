using Chegevala.Core.EntityModel.Models;
using Chegevala.Core.RabbitMQ.BindIngModel;
using Chegevala.Core.Utility;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Chegevala.Client
{
    public class MyClient
    {
        private IConnection connection;

        private BlockingCollection<string> respQueue = new BlockingCollection<string>();
        public MyClient()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            connection = factory.CreateConnection();
            #region 登录模块初始化
            loginChannel = connection.CreateModel();
            loginQueueName = loginChannel.QueueDeclare().QueueName;
            loginConsumer = new EventingBasicConsumer(loginChannel);
            props = loginChannel.CreateBasicProperties();
            correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = loginQueueName;
            loginConsumer.Received += LoginMonitor;
            #endregion
            #region 通讯功能初始化
            cheatChannel = connection.CreateModel();
            cheatConsumer = new EventingBasicConsumer(cheatChannel);
            #endregion
        }
        #region 登录模块
        private User user;
        public User currentUser;
        private IModel loginChannel;
        private EventingBasicConsumer loginConsumer;
        private string loginQueueName;
        private IBasicProperties props;
        private string correlationId;
        public void Login(string userName,string passWord)
        {
            user = new User()
            {
                UserName = userName,
                PassWord = passWord
            };
            var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(user));
            loginChannel.BasicPublish(exchange: "",routingKey: "login",basicProperties: props,body: messageBytes);
            loginChannel.BasicConsume(consumer: loginConsumer, queue: loginQueueName, autoAck: true);
        }
        public void LoginMonitor(object model,BasicDeliverEventArgs ea)
        {
            var body = ea.Body;
            var response = Encoding.UTF8.GetString(body.ToArray());
            if (ea.BasicProperties.CorrelationId == correlationId)
            {
                Console.WriteLine(response);
                if (response.Contains("成功"))
                {
                    currentUser = user;
                    cheatChannel = connection.CreateModel();
                    cheatChannel.QueueDeclare(queue: TpoicHelper.UserTopic(currentUser.UserName), durable: false, exclusive: false, autoDelete: false, arguments: null);
                    cheatChannel.BasicConsume(queue: TpoicHelper.UserTopic(currentUser.UserName), autoAck: true, consumer: cheatConsumer);
                    cheatConsumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var response = Encoding.UTF8.GetString(body.ToArray());
                        Console.WriteLine(response);
                    };
                }
                else
                {

                }
            }
        }
        #endregion
        #region 通讯模块
        private IModel cheatChannel;
        private EventingBasicConsumer cheatConsumer;
        public void SendMessage(string receiver,string content)
        {
            if (currentUser != null)
            {
                var message = new MessageBindingModel()
                {
                    Sender = currentUser.UserName,
                    Receiver = receiver,
                    Content = content
                };
                var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
                cheatChannel.BasicPublish(exchange: "", routingKey: "receive", basicProperties: props, body: messageBytes);
            }
            else
            {
                Console.WriteLine("请先登录");
            }
        }
        #endregion
        public void Close()
        {
            connection.Close();
            Environment.Exit(0);
        }
    }
}