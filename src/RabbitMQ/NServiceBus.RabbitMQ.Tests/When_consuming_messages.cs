namespace NServiceBus.Transport.RabbitMQ.Tests
{
    using NUnit.Framework;

    [TestFixture, Category("Integration")]
    public class When_consuming_messages : RabbitMqContext
    {
        [SetUp]
        public void SetUp()
        {

        }

        [Test]
        public void Should_block_until_a_message_is_available()
        {
            var address = Address.Parse(MYRECEIVEQUEUE);


            var message = new TransportMessage();

            sender.Send(message, address);


            var received = WaitForMessage();


            Assert.AreEqual(message.Id, received.Id);
        }



    }
}