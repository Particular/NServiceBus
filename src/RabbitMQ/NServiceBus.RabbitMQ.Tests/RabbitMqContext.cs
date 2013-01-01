namespace NServiceBus.Transport.RabbitMQ.Tests
{
    using System;
    using System.Collections.Concurrent;
    using NUnit.Framework;
    using RabbitMq;
    using Unicast.Transport.Transactional;
    using global::RabbitMQ.Client;

    public class RabbitMqContext
    {
        protected void MakeSureQueueExists(string queueName)
        {
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queueName, true, false, false, null);
                channel.QueuePurge(queueName);
            }
        }

        protected void MakeSureExchangeExists(string exchangeName)
        {
            using (var channel = connection.CreateModel())
            {
                try
                {
                    channel.ExchangeDelete(exchangeName);
                }
                catch (Exception)
                {
                    
                }
           
              
            }

            using (var channel = connection.CreateModel())
            {
                try
                {
                    channel.ExchangeDeclare(exchangeName, "topic", true);
                }
                catch (Exception)
                {

                }

            }
        }


        [SetUp]
        public void SetUp()
        {
            receivedMessages = new BlockingCollection<TransportMessage>();
            factory = new ConnectionFactory { HostName = "localhost" };
            connection = factory.CreateConnection();
            sender = new RabbitMqMessageSender { Connection = connection };

            dequeueStrategy = new RabbitMqDequeueStrategy { Connection = connection, PurgeOnStartup = true };

            MakeSureQueueExists(MYRECEIVEQUEUE);

            MakeSureExchangeExists(PUBLISHERNAME + ".events");

            MessagePublisher = new RabbitMqMessagePublisher
                {
                    Connection = connection,
                    EndpointQueueName = PUBLISHERNAME
                };
            subscriptionManager = new RabbitMqSubscriptionManager
            {
                Connection = connection,
                EndpointQueueName = MYRECEIVEQUEUE
            };

            dequeueStrategy.Init(Address.Parse(MYRECEIVEQUEUE), new TransactionSettings { IsTransactional = true }, (m) =>
            {
                receivedMessages.Add(m);
                return true;
            });

            dequeueStrategy.Start(1);

        }


        [TearDown]
        public void TearDown()
        {
            dequeueStrategy.Stop();
            connection.Close();
            connection.Dispose();
        }


        protected TransportMessage WaitForMessage()
        {

            TransportMessage message;
            receivedMessages.TryTake(out message, TimeSpan.FromSeconds(1));

            return message;

        }

        BlockingCollection<TransportMessage> receivedMessages;

        protected const string PUBLISHERNAME = "publisherendpoint";
        protected const string MYRECEIVEQUEUE = "testreceiver";
        protected RabbitMqDequeueStrategy dequeueStrategy;
        protected ConnectionFactory factory;
        protected IConnection connection;
        protected RabbitMqMessageSender sender;
        protected RabbitMqMessagePublisher MessagePublisher;
        protected RabbitMqSubscriptionManager subscriptionManager;
    }
}