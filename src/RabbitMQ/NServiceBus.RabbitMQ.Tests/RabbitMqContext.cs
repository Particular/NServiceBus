namespace NServiceBus.RabbitMQ.Tests
{
    using NUnit.Framework;
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


        [SetUp]
        public void SetUp()
        {
            factory = new ConnectionFactory { HostName = "localhost" };
            connection = factory.CreateConnection();
            sender = new RabbitMqMessageSender { Connection = connection };
        }

        [TearDown]
        public void TearDown()
        {
            connection.Close();
            connection.Dispose();
        }
        protected ConnectionFactory factory;
        protected IConnection connection;
        protected RabbitMqMessageSender sender;
    }
}