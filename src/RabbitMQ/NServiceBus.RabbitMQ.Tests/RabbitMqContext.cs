namespace NServiceBus.RabbitMQ.Tests
{
    using NUnit.Framework;
    using global::RabbitMQ.Client;

    public class RabbitMqContext
    {
        

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