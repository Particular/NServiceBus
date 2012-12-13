namespace NServiceBus.RabbitMQ.Tests
{
    using NUnit.Framework;
    using Unicast.Queuing;
    using global::RabbitMQ.Client;


    [TestFixture, Explicit("Integration tests")]
    public class Sending
    {
        [Test]
        public void Should_send_a_message_to_the_given_queue()
        {
            MakeSureQueueExists(connection);

            sender.Send(new TransportMessage
                {
                    Body = System.Text.Encoding.UTF8.GetBytes("<TestMessage/>")
                }, Address.Parse("TestEndpoint@localhost"));
        }


        [Test]
        public void Should_throw_when_sending_to_a_nonexisting_queue()
        {
            Assert.Throws<QueueNotFoundException>(() =>
                 sender.Send(new TransportMessage(), Address.Parse("NonExistingQueue@localhost")));
        }


        static void MakeSureQueueExists(IConnection connection)
        {
            using (var channel = connection.CreateModel())
                channel.QueueDeclare("testendpoint", true, false, false, null);
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
        ConnectionFactory factory;
        IConnection connection;
        RabbitMqMessageSender sender;

    }
}