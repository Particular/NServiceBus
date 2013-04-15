namespace NServiceBus.Transports.RabbitMQ.Tests.ClusteringTests
{
    using System;
    using System.Collections.Concurrent;
    using System.Transactions;
    using Config;
    using EasyNetQ;
    using EasyNetQ.ConnectionString;
    using Logging.Loggers.NLogAdapter;
    using NLog.Targets;
    using NServiceBus;
    using NUnit.Framework;
    using NServiceBus.Transports.RabbitMQ;
    using Settings;
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

            using (var channel = connectionManager.GetConnection(ConnectionPurpose.Administration).CreateModel())
            {
                try
                {
                    channel.ExchangeDelete(exchangeName);
                }
                catch (Exception)
                {

                }


            }

            using (var channel = connectionManager.GetConnection(ConnectionPurpose.Administration).CreateModel())
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

        protected void OutputLogging() {
            var log4View = new NLogViewerTarget {Address = "udp://127.0.0.1:12345", IncludeCallSite = true, AppInfo = "IntegrationTest"};
            log4View.Parameters.Add(new NLogViewerParameterInfo {Layout = "${exception:format=tostring}", Name = "Exception"});
            log4View.Parameters.Add(new NLogViewerParameterInfo {Layout = "${stacktrace}", Name = "StackTrace"});
            var console = new ColoredConsoleTarget {Layout = "${longdate:universalTime=true} | ${logger:padding=-50} | ${message} | ${exception:format=tostring}"};
//            Configure.Instance
//                     .NLog(log4View, console);
            NLogConfigurator.Configure(new object[]{log4View, console}, "DEBUG");
        }

        protected void DoSetup(string connectionString, string queueName) {
            OutputLogging();
            receivedMessages = new BlockingCollection<TransportMessage>();

            var config = new ConnectionStringParser().Parse(connectionString);
            var selectionStrategy = new DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo>();
            var connectionFactory = new ConnectionFactoryWrapper(config, selectionStrategy);
            connectionManager = new RabbitMqConnectionManager(connectionFactory, new ConnectionRetrySettings());

            unitOfWork = new RabbitMqUnitOfWork {ConnectionManager = connectionManager, UsePublisherConfirms = true, MaxWaitTimeForConfirms = TimeSpan.FromSeconds(10)};

            sender = new RabbitMqMessageSender {UnitOfWork = unitOfWork};

            RoutingKeyBuilder = new RabbitMqRoutingKeyBuilder
                {
                    GenerateRoutingKey = DefaultRoutingKeyConvention.GenerateRoutingKey
                };

            dequeueStrategy = new RabbitMqDequeueStrategy {ConnectionManager = connectionManager, PurgeOnStartup = true};

            MakeSureQueueExists(queueName);

            MakeSureExchangeExists(ExchangeNameConvention(Address.Parse(queueName), null));

            MessagePublisher = new RabbitMqMessagePublisher
                {
                    UnitOfWork = unitOfWork,
                    ExchangeName = ExchangeNameConvention,
                    RoutingKeyBuilder = RoutingKeyBuilder
                };
            subscriptionManager = new RabbitMqSubscriptionManager
                {
                    ConnectionManager = connectionManager,
                    EndpointQueueName = queueName,
                    ExchangeName = ExchangeNameConvention,
                    RoutingKeyBuilder = RoutingKeyBuilder
                };

            dequeueStrategy.Init(Address.Parse(queueName), TransactionSettings.Default, (m) =>
                {
                    receivedMessages.Add(m);
                    return true;
                }, (s, exception) =>
                    {
                    });

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

        protected const string PUBLISHERNAME = "publisherendpoint";
        protected RabbitMqDequeueStrategy dequeueStrategy;
        protected RabbitMqConnectionManager connectionManager;
        protected RabbitMqMessageSender sender;
        protected RabbitMqMessagePublisher MessagePublisher;
        protected RabbitMqSubscriptionManager subscriptionManager;
        protected RabbitMqUnitOfWork unitOfWork;
        protected RabbitMqRoutingKeyBuilder RoutingKeyBuilder;
    }
}