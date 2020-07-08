using Chegevala.Core.Utility;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chegevala.Core.RabbitMQ
{
    public class RabbitMqProvider
    {
        #region 连接RabbitMq服务器基础信息
        private string mqHostIp = "127.0.0.1";
        public string MqHostIp
        {
            get { return mqHostIp; }
            set { mqHostIp = value; }
        }

        private ushort mqHostPort = 5672;
        public ushort MqHostPort
        {
            get { return mqHostPort; }
            set { mqHostPort = value; }
        }

        private string mqUserName = "guest";
        public string MqUserName
        {
            get { return mqUserName; }
            set { mqUserName = value; }
        }

        private string mqPasswd = "guest";
        public string MqPasswd
        {
            get { return mqPasswd; }
            set { mqPasswd = value; }
        }

        private string mqVirtualHost = "/";
        public string MqVirtualHost
        {
            get { return mqVirtualHost; }
            set { mqVirtualHost = value; }
        }
        #endregion
        private IConnection consumerConn = null;//socket level

        private object listAccessLock = new object();
        private List<ConnectChannel> channelList = new List<ConnectChannel>();
        public Func<RemoteMessage, bool> ReceiveMessageCallback { get; set; }
        /// <summary>
        /// 通过配置文件构造
        /// </summary>
        public RabbitMqProvider()
        {
            mqHostIp = ConfigurationHelper.Instance.Configuration.GetValue<string>("");
            mqHostPort = ConfigurationHelper.Instance.Configuration.GetValue<ushort>("");
            mqUserName = ConfigurationHelper.Instance.Configuration.GetValue<string>("");
            mqPasswd = ConfigurationHelper.Instance.Configuration.GetValue<string>("");
            mqVirtualHost = ConfigurationHelper.Instance.Configuration.GetValue<string>("");
        }
        /// <summary>
        /// 通过参数构造
        /// </summary>
        /// <param name="hostIp"></param>
        /// <param name="hostPort"></param>
        /// <param name="loginName"></param>
        /// <param name="loginPassword"></param>
        /// <param name="virtualHost"></param>
        public RabbitMqProvider(string hostIp, ushort hostPort, string loginName, string loginPassword, string virtualHost)
        {
            if (!string.IsNullOrEmpty(hostIp))
            {
                mqHostIp = hostIp;
            }
            if (hostPort > 0 || hostPort <= 65535)
            {
                mqHostPort = hostPort;
            }
            if (!string.IsNullOrEmpty(loginName))
            {
                mqUserName = loginName;
            }
            mqPasswd = loginPassword;
            if (!string.IsNullOrEmpty(virtualHost))
            {
                mqVirtualHost = virtualHost;
            }
        }
        /// <summary>
        /// 卸载服务
        /// </summary>

        public void Unload()
        {
            DestructMqConsumerChannel();
            DestructMqConsumerConn();
        }


        /// <summary>
        /// 根据配置创建RabbitMq服务连接
        /// </summary>
        /// <param name="requestHeartBeat">Heartbeat timeout to use when negotiating with the server (in seconds).</param>
        /// <param name="networkRecoveryInterval">Amount of time client will wait for before re-trying to recover connection</param>
        /// <returns></returns>
        public bool ConstructMqConsumerConn(TimeSpan requestHeartBeat, TimeSpan networkRecoveryInterval)
        {
            bool ret = false;
            try
            {
                Uri uri = new Uri("amqp://" + mqHostIp + ":" + mqHostPort + "/");

                ConnectionFactory factory = new ConnectionFactory();

                factory.Endpoint = new AmqpTcpEndpoint(uri);
                factory.UserName = mqUserName;
                factory.Password = mqPasswd;
                factory.VirtualHost = mqVirtualHost;

                factory.RequestedHeartbeat = requestHeartBeat;

                factory.AutomaticRecoveryEnabled = true;
                factory.NetworkRecoveryInterval = networkRecoveryInterval;

                consumerConn = factory.CreateConnection();
                if (consumerConn != null)
                {
                    Console.WriteLine("Customer Connect MQ Server Successfully");
                    consumerConn.ConnectionShutdown += Connection_ConnectionShutdown;
                    ret = true;
                }
                else
                {
                    Console.WriteLine("Customer Connect MQ Server Failed");
                    ret = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Customer Connect MQ Server Failed and error message is {ex.Message}");
                ret = false;
            }
            return ret;
        }
        /// <summary>
        /// 创建simple类型的消息队列
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="msgCallback"></param>
        /// <returns></returns>
        public string ConstructSimpleQueueConsumerChannel(string queueName, Func<RemoteMessage, bool> msgCallback)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                Console.WriteLine("The Queue Name Must Not Be NULL");
                return null;
            }

            ConnectChannel customerChannel = new ConnectChannel();
            customerChannel.ChannelGuid = Guid.NewGuid().ToString();
            customerChannel.ExchangeType = ExchangeType.noexist;
            customerChannel.ExchangeName = string.Empty;
            customerChannel.QueueName = queueName;
            customerChannel.ReceiveMessageCallback = msgCallback;
            customerChannel.ConsumerChannel = null;
            customerChannel.AccessLock = new object();

            try
            {
                if (consumerConn != null)
                {
                    customerChannel.ConsumerChannel = consumerConn.CreateModel();
                    if (customerChannel.ConsumerChannel != null)
                    {
                        customerChannel.ConsumerChannel.QueueDeclare(queue: customerChannel.QueueName, durable: true, exclusive: false, autoDelete: false);//declare queue
                        customerChannel.ConsumerChannel.BasicQos(0, 1, false);

                        lock (listAccessLock)
                        {
                            channelList.Add(customerChannel);
                        }
                        return customerChannel.ChannelGuid;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Construct Mq Customer Channel Meet Error :{ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 创建Topic类型的消息队列
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="queueName"></param>
        /// <param name="bindingKey"></param>
        /// <param name="msgCallback"></param>
        /// <returns></returns>
        public string ConstructTopicExchangeConsumerChannel(string exchangeName, string queueName, string bindingKey, Func<RemoteMessage, bool> msgCallback)
        {
            if (string.IsNullOrEmpty(exchangeName) || string.IsNullOrEmpty(bindingKey))
            {
                Console.WriteLine("In topic Mode, The Exchange And BindingKey Must Not Be NULL");
                return null;
            }

            ConnectChannel customerChannel = new ConnectChannel();
            customerChannel.ChannelGuid = Guid.NewGuid().ToString();
            customerChannel.ExchangeType = ExchangeType.topic;
            customerChannel.ExchangeName = exchangeName;
            customerChannel.QueueName = queueName;
            customerChannel.UserData = bindingKey;
            customerChannel.ReceiveMessageCallback = msgCallback;
            customerChannel.ConsumerChannel = null;
            customerChannel.AccessLock = new object();

            try
            {
                if (consumerConn != null)
                {
                    customerChannel.ConsumerChannel = consumerConn.CreateModel();
                    if (customerChannel.ConsumerChannel != null)
                    {
                        customerChannel.ConsumerChannel.ExchangeDeclare(customerChannel.ExchangeName, customerChannel.ExchangeType.ToString(), true);//Construct a exchange

                        if (string.IsNullOrEmpty(customerChannel.QueueName))
                        {
                            customerChannel.QueueName = customerChannel.ConsumerChannel.QueueDeclare(durable: true, exclusive: false, autoDelete: false).QueueName;//declare queue
                        }
                        else
                        {
                            customerChannel.ConsumerChannel.QueueDeclare(queue: customerChannel.QueueName, durable: true, exclusive: false, autoDelete: false);//declare queue
                        }

                        customerChannel.ConsumerChannel.QueueBind(customerChannel.QueueName, customerChannel.ExchangeName, customerChannel.UserData.ToString(), null);//bind the queue to exchange
                        customerChannel.ConsumerChannel.BasicQos(0, 1, false);

                        lock (listAccessLock)
                        {
                            channelList.Add(customerChannel);
                        }
                        return customerChannel.ChannelGuid;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Construct Mq Customer Channel Meet Error :{ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 创建Fanout类型的消息队列
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="queueName"></param>
        /// <param name="msgCallback"></param>
        /// <returns></returns>
        public string ConstructFanoutExchangeConsumerChannel(string exchangeName, string queueName, Func<RemoteMessage, bool> msgCallback)
        {
            if (string.IsNullOrEmpty(exchangeName))
            {
                Console.WriteLine("In fanout Mode, The Exchange Must Not Be NULL");
                return null;
            }

            ConnectChannel customerChannel = new ConnectChannel();
            customerChannel.ChannelGuid = Guid.NewGuid().ToString();
            customerChannel.ExchangeType = ExchangeType.fanout;
            customerChannel.ExchangeName = exchangeName;
            customerChannel.QueueName = queueName;
            customerChannel.ReceiveMessageCallback = msgCallback;
            customerChannel.ConsumerChannel = null;
            customerChannel.AccessLock = new object();

            try
            {
                if (consumerConn != null)
                {
                    customerChannel.ConsumerChannel = consumerConn.CreateModel();
                    if (customerChannel.ConsumerChannel != null)
                    {
                        customerChannel.ConsumerChannel.ExchangeDeclare(customerChannel.ExchangeName, customerChannel.ExchangeType.ToString(), true);//Construct a exchange

                        if (string.IsNullOrEmpty(customerChannel.QueueName))
                        {
                            customerChannel.QueueName = customerChannel.ConsumerChannel.QueueDeclare(durable: true, exclusive: false, autoDelete: false).QueueName;//declare queue
                        }
                        else
                        {
                            customerChannel.ConsumerChannel.QueueDeclare(queue: customerChannel.QueueName, durable: true, exclusive: false, autoDelete: false);//declare queue
                        }

                        customerChannel.ConsumerChannel.QueueBind(customerChannel.QueueName, customerChannel.ExchangeName, "", null);//bind the queue to exchange
                        customerChannel.ConsumerChannel.BasicQos(0, 1, false);

                        lock (listAccessLock)
                        {
                            channelList.Add(customerChannel);
                        }
                        return customerChannel.ChannelGuid;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Construct Mq Customer Channel Meet Error :{ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 创建Direct类型的消息队列
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="queueName"></param>
        /// <param name="bindingKey"></param>
        /// <param name="msgCallback"></param>
        /// <returns></returns>
        public string ConstructDirectExchangeConsumerChannel(string exchangeName, string queueName, string bindingKey, Func<RemoteMessage, bool> msgCallback)
        {
            if (string.IsNullOrEmpty(exchangeName))
            {
                Console.WriteLine("In direct Mode, The Exchange And BindingKey Must Not Be NULL");
                return null;
            }

            ConnectChannel customerChannel = new ConnectChannel();
            customerChannel.ChannelGuid = Guid.NewGuid().ToString();
            customerChannel.ExchangeType = ExchangeType.direct;
            customerChannel.ExchangeName = exchangeName;
            customerChannel.QueueName = queueName;
            customerChannel.UserData = bindingKey;
            customerChannel.ReceiveMessageCallback = msgCallback;
            customerChannel.ConsumerChannel = null;
            customerChannel.AccessLock = new object();

            try
            {
                if (consumerConn != null)
                {
                    customerChannel.ConsumerChannel = consumerConn.CreateModel();
                    if (customerChannel.ConsumerChannel != null)
                    {
                        customerChannel.ConsumerChannel.ExchangeDeclare(customerChannel.ExchangeName, customerChannel.ExchangeType.ToString(), true);//Construct a exchange

                        if (string.IsNullOrEmpty(customerChannel.QueueName))
                        {
                            customerChannel.QueueName = customerChannel.ConsumerChannel.QueueDeclare(durable: true, exclusive: false, autoDelete: false).QueueName;//declare queue
                        }
                        else
                        {
                            customerChannel.ConsumerChannel.QueueDeclare(queue: customerChannel.QueueName, durable: true, exclusive: false, autoDelete: false);//declare queue
                        }

                        customerChannel.ConsumerChannel.QueueBind(customerChannel.QueueName, customerChannel.ExchangeName, customerChannel.UserData.ToString(), null);//bind the queue to exchange
                        customerChannel.ConsumerChannel.BasicQos(0, 1, false);

                        lock (listAccessLock)
                        {
                            channelList.Add(customerChannel);
                        }
                        return customerChannel.ChannelGuid;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Construct Mq Customer Channel Meet Error :{ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 创建Header类型的消息队列
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="queueName"></param>
        /// <param name="args"></param>
        /// <param name="msgCallback"></param>
        /// <returns></returns>
        public string ConstructHeadersExchangeConsumerChannel(string exchangeName, string queueName, IDictionary<string, object> args, Func<RemoteMessage, bool> msgCallback)
        {
            if (string.IsNullOrEmpty(exchangeName) || args == null)
            {
                Console.WriteLine("In headers Mode, The Exchange And Args Must Not Be NULL");
                return null;
            }

            ConnectChannel customerChannel = new ConnectChannel();
            customerChannel.ChannelGuid = Guid.NewGuid().ToString();
            customerChannel.ExchangeType = ExchangeType.headers;
            customerChannel.ExchangeName = exchangeName;
            customerChannel.QueueName = queueName;
            customerChannel.UserData = args;
            customerChannel.ReceiveMessageCallback = msgCallback;
            customerChannel.ConsumerChannel = null;
            customerChannel.AccessLock = new object();

            try
            {
                if (consumerConn != null)
                {
                    customerChannel.ConsumerChannel = consumerConn.CreateModel();
                    if (customerChannel.ConsumerChannel != null)
                    {
                        customerChannel.ConsumerChannel.ExchangeDeclare(customerChannel.ExchangeName, customerChannel.ExchangeType.ToString(), true);//Construct a exchange

                        if (string.IsNullOrEmpty(customerChannel.QueueName))
                        {
                            customerChannel.QueueName = customerChannel.ConsumerChannel.QueueDeclare(durable: true, exclusive: false, autoDelete: false).QueueName;//declare queue
                        }
                        else
                        {
                            customerChannel.ConsumerChannel.QueueDeclare(queue: customerChannel.QueueName, durable: true, exclusive: false, autoDelete: false);//declare queue
                        }

                        customerChannel.ConsumerChannel.QueueBind(customerChannel.QueueName, customerChannel.ExchangeName, customerChannel.UserData.ToString(), null);//bind the queue to exchange
                        customerChannel.ConsumerChannel.BasicQos(0, 1, false);

                        lock (listAccessLock)
                        {
                            channelList.Add(customerChannel);
                        }
                        return customerChannel.ChannelGuid;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Construct Mq Customer Channel Meet Error :{ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 为指定消息队列创建生产者
        /// </summary>
        /// <param name="channelGuid"></param>
        public void Consume(string channelGuid)
        {
            ConnectChannel temp = null;
            lock (listAccessLock)
            {
                if (channelList != null)
                {
                    temp = channelList.Where(p => p != null && p.ChannelGuid == channelGuid).FirstOrDefault();
                }
            }

            if (temp != null)
            {
                lock (temp.AccessLock)
                {
                    if (temp.ConsumerChannel != null)
                    {
                        var consumer = new EventingBasicConsumer(temp.ConsumerChannel);
                        consumer.Received += Consumer_MultiReceived;

                        bool autoDeleteMessage = false;
                        temp.ConsumerChannel.BasicConsume(temp.QueueName, autoDeleteMessage, temp.ChannelGuid, consumer);
                    }
                }
            }
        }
        /// <summary>
        /// 消息接收事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Consumer_MultiReceived(object sender, BasicDeliverEventArgs args)
        {
            string channelGuid = args.ConsumerTag;

            ConnectChannel tempChannel = null;
            lock (listAccessLock)
            {
                if (channelList != null)
                {
                    tempChannel = channelList.Where(p => p != null && p.ChannelGuid == channelGuid).FirstOrDefault();
                }
            }

            var body = args.Body;
            var message = body.ToString();

            if (tempChannel != null)
            {
                lock (tempChannel.AccessLock)
                {
                    if (tempChannel.ConsumerChannel != null)
                    {
                        bool result = tempChannel.ReceiveMessageCallback(new RemoteMessage() 
                        {
                            JsonContent = message,
                            SenderUID=args.BasicProperties.ReplyTo
                        });
                        if (result)
                        {
                            if (tempChannel.ConsumerChannel != null && !tempChannel.ConsumerChannel.IsClosed)
                            {
                                tempChannel.ConsumerChannel.BasicAck(args.DeliveryTag, false);
                            }
                        }
                        else
                        {
                        }
                    }
                }
            }
        }
        #region 关闭rabbitmq服务与通道
        public bool DestructMqConsumerChannel()
        {
            lock (listAccessLock)
            {
                if (channelList != null)
                {
                    foreach(var channel in channelList)
                    {
                        try
                        {
                            channel.ConsumerChannel.Close();
                            channel.ConsumerChannel.Dispose();
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine($"Close Customer Channel({channel.ChannelGuid}) Meet Error: {ex.Message}");
                        }
                    }
                }
            }
            return true;
        }
        public bool DestructMqConsumerConn()
        {
            if (consumerConn != null)
            {
                try
                {
                    consumerConn.Close();
                    consumerConn.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Close Customer Conn Meet Error: {ex.Message}");
                }
                consumerConn = null;
            }
            return true;
        }
        #endregion
        /// <summary>
        /// RabbitMq异常中断事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine($"Consumer Connection To Server: {mqHostIp} disconnect");
        }
    }
}
