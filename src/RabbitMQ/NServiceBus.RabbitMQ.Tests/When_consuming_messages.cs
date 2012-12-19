namespace NServiceBus.RabbitMQ.Tests
{
    using NUnit.Framework;

    [TestFixture, Explicit("Integration tests")]
    public class When_consuming_messages:RabbitMqContext
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


            Assert.AreEqual(message.Id,received.Id);
        }

       
        
    }
}