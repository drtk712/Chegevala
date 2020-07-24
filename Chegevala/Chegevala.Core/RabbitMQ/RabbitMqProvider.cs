using Chegevala.Core.EntityModel.Models;
using Chegevala.Core.EntityModel.Models.Enum;
using Chegevala.Core.Utility;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
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
        private IConnection consumerConn = null;
        public IConnection ConsumerConn
        {
            get
            {
                if (consumerConn == null)
                    Console.WriteLine("尚未连接到RabbitMQ服务器");
                return consumerConn;
            }
        }
        private object listAccessLock = new object();
        private List<ConnectChannel> channelList = new List<ConnectChannel>();
        public List<ConnectChannel> ChannelList
        {
            get { return channelList; }
        }
        public ConnectChannel GetConnectChannelByGuid(string channelGuid)
        {
            return channelList.Where(n => n.ChannelGuid == channelGuid).FirstOrDefault();
        }
        public ConnectChannel GetConnectChannelByName(string channelName)
        {
            return channelList.Where(n => n.ChannelName == channelName).FirstOrDefault();
        }
        public Func<RemoteMessage, bool> ReceiveMessageCallback { get; set; }
        /// <summary>
        /// 通过配置文件构造
        /// </summary>
        public RabbitMqProvider()
        {
            mqHostIp = ConfigurationHelper.Instance.Configuration.GetValue<string>("IP");
            mqHostPort = ConfigurationHelper.Instance.Configuration.GetValue<ushort>("Port");
            mqUserName = ConfigurationHelper.Instance.Configuration.GetValue<string>("Name");
            mqPasswd = ConfigurationHelper.Instance.Configuration.GetValue<string>("Password");
            mqVirtualHost = ConfigurationHelper.Instance.Configuration.GetValue<string>("VirtualHost");
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
                consumerConn = null;
                ret = false;
            }
            return ret;
        }
        /// <summary>
        /// 创建基础channel
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="exchangeType"></param>
        /// <param name="exchangeName"></param>
        /// <param name="msgCallback"></param>
        /// <returns></returns>
        public bool ConstructMqChannel(string channelName, Func<RemoteMessage, BasicDeliverEventArgs, bool> msgCallback)
        {
            if (string.IsNullOrEmpty(channelName))
            {
                LogHelper.LogError("The channel Name Must Not Be NULL");
                return false;
            }

            if (channelList.Find(n => n.ChannelName == channelName) != null)
            {
                LogHelper.LogError($"Channel {channelName} Aleady Exist.");
                return false;
            }
            ConnectChannel connectChannel = new ConnectChannel();
            connectChannel.ChannelName = channelName;
            connectChannel.ChannelGuid = Guid.NewGuid().ToString();
            connectChannel.QueueNames = new List<string>();
            connectChannel.ReceiveMessageCallback = msgCallback;
            connectChannel.AccessLock = new object();
            connectChannel.ExchangeType = ExchangeType.noexist;
            connectChannel.ConsumerChannel = null;
            try
            {
                if (ConsumerConn != null)
                {
                    connectChannel.ConsumerChannel = consumerConn.CreateModel();
                    if (connectChannel.ConsumerChannel != null)
                    {
                        lock (listAccessLock)
                        {
                            channelList.Add(connectChannel);
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                LogHelper.LogError(e.Message);
                return false;
            }
        }
        /// <summary>
        /// 为指定名称的channel创建Exchange
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="exchangeType"></param>
        /// <param name="exchangeName"></param>
        /// <returns></returns>
        public bool ConstructMqExchange(string channelName, ExchangeType exchangeType, string exchangeName)
        {
            if (string.IsNullOrEmpty(exchangeName))
            {
                LogHelper.LogError("Exchange Name Must Not Be NULL");
                return false;
            }
            ConnectChannel channel = channelList.Find(n => n.ChannelName == channelName);
            if (channel == null)
            {
                LogHelper.LogError($"Channel {channelName} Not Exist.");
                return false;
            }
            channel.ExchangeType = exchangeType;
            channel.ExchangeName = exchangeName;
            try
            {
                channel.ConsumerChannel.ExchangeDeclare(exchangeName, exchangeType.ToString(), true);
                return true;
            }
            catch (Exception e)
            {
                LogHelper.LogError(e.Message);
                return false;
            }

        }

        public bool ConstructMqQueue(string channelName, string queueName = null, string bindingKey = null)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                LogHelper.LogError("QueueName Must Not Be NULL");
                return false;
            }
            ConnectChannel channel = channelList.Find(n => n.ChannelName == channelName);
            if (channel == null)
            {
                LogHelper.LogError($"Channel {channelName} Not Exist.");
                return false;
            }
            if (!string.IsNullOrEmpty(channel.ExchangeName))
            {
                if (string.IsNullOrEmpty(bindingKey))
                {
                    LogHelper.LogError($"The Channel has Exchange,So you must input BindingKey");
                    return false;
                }
            }
            try
            {
                channel.ConsumerChannel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
                if (!string.IsNullOrEmpty(channel.ExchangeName))
                {
                    channel.ConsumerChannel.QueueBind(queueName, channel.ExchangeName, bindingKey);
                }
                lock (channel.AccessLock)
                {
                    channel.QueueNames.Add(queueName);
                }
                return true;

            }
            catch (Exception e)
            {
                LogHelper.LogError(e.Message);
                return false;
            }
        }
        /// <summary>
        /// 为指定消息队列创建生产者
        /// </summary>
        /// <param name="channelGuid"></param>
        public bool Consume(string channelName, string queueName)
        {
            ConnectChannel temp = null;
            lock (listAccessLock)
            {
                if (channelList != null)
                {
                    temp = channelList.Where(p => p != null && p.ChannelName == channelName).FirstOrDefault();
                }
            }
            if (temp == null)
            {
                LogHelper.LogError($"Channel {channelName} Not Exist");
                return false;
            }
            if (!temp.QueueNames.Contains(queueName))
            {
                LogHelper.LogError($"Queue {queueName} Not Exist");
                return false;
            }
            try
            {
                lock (temp.AccessLock)
                {
                    var consumer = new EventingBasicConsumer(temp.ConsumerChannel);
                    consumer.Received += Consumer_MultiReceived;
                    temp.ConsumerChannel.BasicConsume(queueName, false, temp.ChannelGuid, consumer);
                }
                return true;
            }
            catch (Exception e)
            {
                LogHelper.LogError(e.Message);
                return false;
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
            var message = Encoding.UTF8.GetString(body.ToArray());

            if (tempChannel != null)
            {
                lock (tempChannel.AccessLock)
                {
                    if (tempChannel.ConsumerChannel != null)
                    {
                        bool result = tempChannel.ReceiveMessageCallback(JsonConvert.DeserializeObject<RemoteMessage>(message), args) ;
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
        public bool Send(string channelName, RemoteMessage message, string queueName=null)
        {
            if (message == null)
            {
                LogHelper.LogError("Message Must Not Be NULL");
                return false;
            }
            var channel = channelList.Find(p => p.ChannelName == channelName);
            if (channel == null)
            {
                LogHelper.LogError($"Channel {channelName} Not Exist");
                return false;
            }
            if (queueName!=null && !channel.QueueNames.Contains(queueName))
            {
                LogHelper.LogError($"Queue {queueName} Not Exist");
                return false;
            }

            try
            {
                var properties = channel.ConsumerChannel.CreateBasicProperties();
                properties.Persistent = message.EnablePersistent;
                properties.CorrelationId = channel.ChannelGuid;

                //convert message to byte[]
                var msgBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
                
                //send message

                if (channel.ExchangeType == ExchangeType.noexist)
                {
                    if (queueName != null)
                    {
                        channel.ConsumerChannel.BasicPublish("", queueName, properties, msgBody);
                    }
                    else
                    {
                        LogHelper.LogError($"Queue Name Not Be NULL");
                        return false;
                    }
                }
                else if ((channel.ExchangeType == ExchangeType.topic || channel.ExchangeType == ExchangeType.direct) && !string.IsNullOrEmpty(message.TopicRoute))
                {
                    channel.ConsumerChannel.BasicPublish(channel.ExchangeName, message.TopicRoute, properties, msgBody);
                }
                else if (channel.ExchangeType == ExchangeType.fanout)
                {
                    channel.ConsumerChannel.BasicPublish(channel.ExchangeName, "", properties, msgBody);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQ Productor Send Message failed And Error message is {ex.Message}");
                return false;
            }
            return true;
        }
        public void DeleteQueue(string channel,string queue)
        {
            if (String.IsNullOrEmpty(channel) || String.IsNullOrEmpty(queue))
            {
                Console.WriteLine("Params Must Not Be NULL");
            }
            ConnectChannel tempChannel = null;
            lock (listAccessLock)
            {
                if (channelList != null)
                {
                    tempChannel = channelList.Where(p => p != null && p.ChannelName == channel).FirstOrDefault();
                }
            }
            if (tempChannel != null)
            {
                lock (tempChannel.AccessLock)
                {
                    if (tempChannel.QueueNames!=null && tempChannel.QueueNames.Contains(queue))
                    {
                        tempChannel.ConsumerChannel.QueueDelete(queue);
                        tempChannel.QueueNames.Remove(queue);
                    }
                    else
                    {
                        Console.WriteLine($"{queue} Not Exist");
                    }
                }
            }
            else
            {
                Console.WriteLine($"{channel} Not Exist");
            }
        }
        #region 关闭rabbitmq服务与通道
        public bool DestructMqConsumerChannel()
        {
            lock (listAccessLock)
            {
                if (channelList != null)
                {
                    foreach (var channel in channelList)
                    {
                        try
                        {
                            channel.ConsumerChannel.Close();
                            channel.ConsumerChannel.Dispose();
                        }
                        catch (Exception ex)
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
