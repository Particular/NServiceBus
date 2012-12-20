namespace NServiceBus.RabbitMQ.Tests
{
    using System.Threading;
    using NUnit.Framework;
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
                channel.ExchangeDeclare(exchangeName,"topic",true);
            }
        }


        [SetUp]
        public void SetUp()
        {
            factory = new ConnectionFactory { HostName = "localhost" };
            connection = factory.CreateConnection();
            sender = new RabbitMqMessageSender { Connection = connection };

            dequeueStrategy = new RabbitMqDequeueStrategy{Connection = connection};

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
            var sr = new ManualResetEvent(false);
            TransportMessage received = null;

            dequeueStrategy.TryProcessMessage += (m) =>
            {
                received = m;
                sr.Set();
                return true;
            };

            dequeueStrategy.Init(Address.Parse(MYRECEIVEQUEUE), new TransactionSettings { IsTransactional = true });
            dequeueStrategy.Start(1);

            sr.WaitOne(1000);

            return received;

        }
        
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