namespace NServiceBus.RabbitMQ.Tests
{
    using NUnit.Framework;
    using global::RabbitMQ.Client;


    [TestFixture,Explicit("Integration tests")]
    public class Sending
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void Should_send_a_message_to_the_given_queue()
        {
            var factory = new ConnectionFactory {HostName = "localhost"};


            using (var connection = factory.CreateConnection())
            {

                MakeSureQueueExists(connection);

                var sender = new RabbitMqMessageSender
                {
                    Connection = connection
                };

                sender.Send(new TransportMessage
                    {
                        Body =  System.Text.Encoding.UTF8.GetBytes("<TestMessage/>")
                    }, Address.Parse("TestEndpoint@localhost"));
            }


        }

        static void MakeSureQueueExists(IConnection connection)
        {
            using (var channel = connection.CreateModel())
                channel.QueueDeclare("testendpoint", false, false, false, null);
        }
    }
}