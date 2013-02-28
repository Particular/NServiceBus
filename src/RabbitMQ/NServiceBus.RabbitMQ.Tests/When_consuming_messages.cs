namespace NServiceBus.Transports.RabbitMQ.Tests
{
    using NServiceBus;
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





        [Test]
        public void Should_be_able_to_receive_messages_without_headers()
        {
            var address = Address.Parse(MYRECEIVEQUEUE);

            var message = new TransportMessage();


            using (var channel = this.connectionManager.GetConnection(ConnectionPurpose.Publish).CreateModel())
            {
                var properties = channel.CreateBasicProperties();

                properties.MessageId = message.Id;
        
                channel.BasicPublish(string.Empty, address.Queue, true, false, properties, message.Body);
            }
          
            var received = WaitForMessage();


            Assert.AreEqual(message.Id, received.Id);
        }

        [Test]
        public void Should_be_able_to_receive_a_blank_message()
        {
            var address = Address.Parse(MYRECEIVEQUEUE);

            var message = new TransportMessage();


            using (var channel = this.connectionManager.GetConnection(ConnectionPurpose.Publish).CreateModel())
            {
                var properties = channel.CreateBasicProperties();

            
                channel.BasicPublish(string.Empty, address.Queue, true, false, properties, message.Body);
            }

            var received = WaitForMessage();


            Assert.AreEqual(null, received.Id);
        }
    }
}