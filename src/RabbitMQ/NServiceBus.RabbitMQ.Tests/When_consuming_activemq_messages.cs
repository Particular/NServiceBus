namespace NServiceBus.RabbitMQ.Tests
{
    using NUnit.Framework;

    [TestFixture, Explicit("Integration tests")]
    public class When_consuming_activemq_messages:RabbitMqContext
    {
        [Test]
        public void Should_block_until_a_message_is_available()
        {
            var consumer = new RabbitMqConsumer
                {
                    Connection = connection,
                    QueueName = "testReceiveQueue"
                };

            consumer.Start();
        }
    }
}