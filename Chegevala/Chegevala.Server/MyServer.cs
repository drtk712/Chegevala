using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chegevala.Core.EntityModel;
using Chegevala.Core.EntityModel.Models;
using Chegevala.Core.RabbitMQ.BindIngModel;
using Chegevala.Core.Utility;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Chegevala.Server
{
    public class MyServer
    {
        private IConnection connection;
        public MyServer()
        {
            var factory = new ConnectionFactory() { HostName = "localhost"};
            connection = factory.CreateConnection();
            //登录
            #region 登录模块初始化
            loginChannel = connection.CreateModel();
            loginChannel.QueueDeclare(queue: "login", durable: false, exclusive: false, autoDelete: false, arguments: null);
            loginConsumer = new EventingBasicConsumer(loginChannel);
            loginChannel.BasicConsume(queue: "login", autoAck: false, consumer: loginConsumer);
            loginConsumer.Received += Login;
            #endregion
            #region 通讯功能初始化
            receiveChannel = connection.CreateModel();
            receiveChannel.QueueDeclare(queue: "receive", durable: false, exclusive: false, autoDelete: false, arguments: null);
            receiveChannel.BasicQos(0, 1, false);
            receiveConsumer = new EventingBasicConsumer(receiveChannel);
            receiveChannel.BasicConsume(queue: "receive", autoAck: true, consumer: receiveConsumer);
            receiveConsumer.Received += Receive;
            #endregion
        }
        #region 登录模块
        private IModel loginChannel;
        private EventingBasicConsumer loginConsumer;
        private void Login(object model,BasicDeliverEventArgs ea)
        {
            string response = null;

            var body = ea.Body;
            var props = ea.BasicProperties;
            var replyProps = loginChannel.CreateBasicProperties();
            replyProps.CorrelationId = props.CorrelationId;
            try
            {
                User user=JsonConvert.DeserializeObject<User>(Encoding.UTF8.GetString(body.ToArray()));
                using(var da=new ChegevalaContext())
                {
                    var loginuser = da.Set<User>().Where(p => p.UserName == user.UserName).ToList().FirstOrDefault();
                    if (loginuser != null)
                    {
                        if(loginuser.PassWord == user.PassWord)
                        {
                            response = "登录成功";
                        }
                        else
                        {
                            response = "登录失败，密码错误";
                        }
                    }
                    else
                    {
                        da.Add(user);
                        response = "注册成功";
                    }
                    da.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(" [.] " + e.Message);
                response = e.Message;
            }
            finally
            {
                var responseBytes = Encoding.UTF8.GetBytes(response);
                loginChannel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: responseBytes);
                loginChannel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        }
        #endregion
        #region 通讯模块
        private IModel receiveChannel;
        private EventingBasicConsumer receiveConsumer;
        private void Receive(object model,BasicDeliverEventArgs ea)
        {
            string response = null;
            string receiver = null;
            var body = ea.Body;
            var props = ea.BasicProperties;
            var replyProps = receiveChannel.CreateBasicProperties();
            replyProps.CorrelationId = props.CorrelationId;
            try
            {
                MessageBindingModel message = JsonConvert.DeserializeObject<MessageBindingModel>(Encoding.UTF8.GetString(body.ToArray()));
                response = message.Sender + " : " + message.Content;
                receiver = message.Receiver; 
                Console.WriteLine(" [.] {0} ----> {1} ;Content:{2} ",message.Sender,message.Receiver,message.Content);
            }
            catch (Exception e)
            {
                Console.WriteLine(" [.] " + e.Message);
                response = e.Message;
            }
            finally
            {
                var responseBytes = Encoding.UTF8.GetBytes(response);
                receiveChannel.BasicPublish(exchange: "", routingKey: TpoicHelper.UserTopic(receiver), basicProperties: replyProps, body: responseBytes);
            }
        }
        #endregion
        public void Close()
        {
            //loginChannel.Close();
            //receiveChannel.Close();
            try
            {
                connection.Close();
                Environment.Exit(0);
            }
            catch
            {

            }

        }
    }
}
