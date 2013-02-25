namespace NServiceBus.RabbitMQ.Tests
{
    using System;
    using System.Collections.Concurrent;
    using NServiceBus;
    using RabbitMq;
    using NUnit.Framework;
    using global::RabbitMQ.Client;
    using TransactionSettings = Unicast.Transport.Transactional.TransactionSettings;

    public class RabbitMqContext
    {
        protected void MakeSureQueueExists(string queueName)
        {
            using (var channel = connectionManager.GetConnection(ConnectionPurpose.Administration).CreateModel())
            {
                channel.QueueDeclare(queueName, true, false, false, null);
                channel.QueuePurge(queueName);
            }
        }

        protected void MakeSureExchangeExists(string exchangeName)
        {
            if (exchangeName == "amq.topic")
                return;

            var connection = connectionManager.GetConnection(ConnectionPurpose.Administration);
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
            connectionManager = new RabbitMqConnectionManager(new ConnectionFactory { HostName = "localhost" });

            unitOfWork = new RabbitMqUnitOfWork { ConnectionManager = connectionManager };

            sender = new RabbitMqMessageSender { UnitOfWork = unitOfWork };

            dequeueStrategy = new RabbitMqDequeueStrategy { ConnectionManager = connectionManager, PurgeOnStartup = true };

            MakeSureQueueExists(MYRECEIVEQUEUE);

            MakeSureExchangeExists(ExchangeNameConvention(Address.Parse(MYRECEIVEQUEUE),null));

            MessagePublisher = new RabbitMqMessagePublisher
                {
                    UnitOfWork = unitOfWork,
                    ExchangeName = ExchangeNameConvention
                };
            subscriptionManager = new RabbitMqSubscriptionManager
            {
                ConnectionManager = connectionManager,
                EndpointQueueName = MYRECEIVEQUEUE,
                ExchangeName = ExchangeNameConvention
            };

            dequeueStrategy.Init(Address.Parse(MYRECEIVEQUEUE), new TransactionSettings { IsTransactional = true }, (m) =>
            {
                receivedMessages.Add(m);
                return true;
            }, (s, exception) => { });

            dequeueStrategy.Start(1);
        }


        [TearDown]
        public void TearDown()
        {
            connectionManager.Dispose();

            if (dequeueStrategy != null)
                dequeueStrategy.Stop();

        }

        protected virtual string ExchangeNameConvention(Address address,Type eventType)
        {
            return "amq.topic";
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
        protected RabbitMqConnectionManager connectionManager;
        protected RabbitMqMessageSender sender;
        protected RabbitMqMessagePublisher MessagePublisher;
        protected RabbitMqSubscriptionManager subscriptionManager;
        protected RabbitMqUnitOfWork unitOfWork;
    }
}