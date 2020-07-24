using Chegevala.Core.EntityModel.Models;
using Chegevala.Core.RabbitMQ;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chegevala.Core
{
    public class CoreService
    {
        #region 基础队列名称设置
        protected const string MyServiceName = "Chegevala";
        protected const string MyServiceLoginChannelName = "loginchannel";
        protected const string MyServiceLoginQueueName = "loginqueue";
        protected const string MyServiceReceiveChannelName = "receivechannel";
        protected const string MyServiceReceiveQueueName = "receivequeue";
        protected const string MyServiceMessageChannelName = "messagechannel";
        protected const string MyServiceMessageQueueName = "messagequeue";
        protected const string MyServiceMessageExchangeName = "messageexchange";
        #endregion

        public RabbitMqProvider rabbitMqProvider = null;

        protected bool RunSimpleChannel(string channel,string queue,Func<RemoteMessage,BasicDeliverEventArgs,bool> msgCallback)
        {
            if (rabbitMqProvider.ConstructMqChannel(channel, msgCallback))
                if (rabbitMqProvider.ConstructMqQueue(channel, queue))
                        return true;
            return false;
        }
        protected bool RunExchangeChannel(string channel, string queue,ExchangeType exchangeType ,string exchange, string bindingKey,Func<RemoteMessage, BasicDeliverEventArgs, bool> msgCallback)
        {
            if (rabbitMqProvider.ConstructMqChannel(channel, msgCallback))
                if(rabbitMqProvider.ConstructMqExchange(channel,exchangeType,exchange))
                    if (rabbitMqProvider.ConstructMqQueue(channel, queue, bindingKey))
                            return true;
            return false;
        }

        protected bool RunSimpleConsumeChannel(string channel, string queue, Func<RemoteMessage, BasicDeliverEventArgs, bool> msgCallback)
        {
            if (rabbitMqProvider.ConstructMqChannel(channel, msgCallback))
                if (rabbitMqProvider.ConstructMqQueue(channel, queue))
                    if (rabbitMqProvider.Consume(channel, queue))
                        return true;
            return false;
        }

        protected bool RunExchangeConsumeChannel(string channel, string queue, ExchangeType exchangeType, string exchange, string bindingKey,Func<RemoteMessage, BasicDeliverEventArgs, bool> msgCallback)
        {
            if (rabbitMqProvider.ConstructMqChannel(channel, msgCallback))
                if (rabbitMqProvider.ConstructMqExchange(channel, exchangeType, exchange))
                    if (rabbitMqProvider.ConstructMqQueue(channel, queue, bindingKey))
                        if (rabbitMqProvider.Consume(channel, queue))
                            return true;
            return false;
        }

    }
}
