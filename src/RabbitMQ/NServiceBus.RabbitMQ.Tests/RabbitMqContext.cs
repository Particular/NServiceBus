namespace NServiceBus.Transports.RabbitMQ.Tests
{
    using System;
    using System.Collections.Concurrent;
    using Config;
    using EasyNetQ;
    using NServiceBus;
    using NUnit.Framework;
    using RabbitMQ;
    using Routing;
    using global::RabbitMQ.Client;
    using TransactionSettings = Unicast.Transport.TransactionSettings;

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
            DeleteExchange(exchangeName);

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

        void DeleteExchange(string exchangeName)
        {
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
        }


        [SetUp]
        public void SetUp()
        {
            var routingTopology = new ConventionalRoutingTopology();
            receivedMessages = new BlockingCollection<TransportMessage>();

            var config = new ConnectionConfiguration();
            config.ParseHosts("localhost:5672");
            
            var selectionStrategy = new DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo>();
            var connectionFactory = new ConnectionFactoryWrapper(config, selectionStrategy);
            connectionManager = new RabbitMqConnectionManager(connectionFactory, config);

            unitOfWork = new RabbitMqUnitOfWork { ConnectionManager = connectionManager,UsePublisherConfirms = true,MaxWaitTimeForConfirms = TimeSpan.FromSeconds(10) };

            sender = new RabbitMqMessageSender { UnitOfWork = unitOfWork, RoutingTopology = routingTopology };


            dequeueStrategy = new RabbitMqDequeueStrategy { ConnectionManager = connectionManager, PurgeOnStartup = true };
            
            MakeSureQueueExists(MYRECEIVEQUEUE);

            DeleteExchange(MYRECEIVEQUEUE);
            MakeSureExchangeExists(ExchangeNameConvention(Address.Parse(MYRECEIVEQUEUE),null));
            
            

            MessagePublisher = new RabbitMqMessagePublisher
                {
                    UnitOfWork = unitOfWork,
                    RoutingTopology = routingTopology
                };
            subscriptionManager = new RabbitMqSubscriptionManager
            {
                ConnectionManager = connectionManager,
                EndpointQueueName = MYRECEIVEQUEUE,
                RoutingTopology = routingTopology
            };

            dequeueStrategy.Init(Address.Parse(MYRECEIVEQUEUE), TransactionSettings.Default, (m) =>
            {
                receivedMessages.Add(m);
                return true;
            }, (s, exception) => { });

            dequeueStrategy.Start(1);
        }


        [TearDown]
        public void TearDown()
        {
            if (dequeueStrategy != null)
                dequeueStrategy.Stop();
            
            connectionManager.Dispose();
        }

        protected virtual string ExchangeNameConvention(Address address,Type eventType)
        {
            return "amq.topic";
        }


        protected TransportMessage WaitForMessage()
        {
            var waitTime = TimeSpan.FromSeconds(1);

            if (System.Diagnostics.Debugger.IsAttached)
                waitTime = TimeSpan.FromMinutes(10);

            TransportMessage message;
            receivedMessages.TryTake(out message, waitTime);

            return message;

        }

        BlockingCollection<TransportMessage> receivedMessages;

        protected const string MYRECEIVEQUEUE = "testreceiver";
        protected RabbitMqDequeueStrategy dequeueStrategy;
        protected RabbitMqConnectionManager connectionManager;
        protected RabbitMqMessageSender sender;
        protected RabbitMqMessagePublisher MessagePublisher;
        protected RabbitMqSubscriptionManager subscriptionManager;
        protected RabbitMqUnitOfWork unitOfWork;
    }
}