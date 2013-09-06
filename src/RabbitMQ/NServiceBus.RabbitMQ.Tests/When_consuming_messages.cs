namespace NServiceBus.Transports.RabbitMQ.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    [Explicit("requires rabbit node")]
    public class When_consuming_messages : RabbitMqContext
    {
        [SetUp]
        public void SetUp()
        {
            MakeSureQueueAndExchangeExists(ReceiverQueue);
        }

        [Test]
        public void Should_block_until_a_message_is_available()
        {
            var address = Address.Parse(ReceiverQueue);


            var message = new TransportMessage();

            sender.Send(message, address);


            var received = WaitForMessage();


            Assert.AreEqual(message.Id, received.Id);
        }


        [Test]
        public void Should_be_able_to_receive_messages_without_headers()
        {
            var address = Address.Parse(ReceiverQueue);

            var message = new TransportMessage();


            using (var channel = connectionManager.GetPublishConnection().CreateModel())
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
            var address = Address.Parse(ReceiverQueue);

            var message = new TransportMessage();


            using (var channel = connectionManager.GetPublishConnection().CreateModel())
            {
                var properties = channel.CreateBasicProperties();

                properties.MessageId = message.Id;

                channel.BasicPublish(string.Empty, address.Queue, true, false, properties, message.Body);
            }

            var received = WaitForMessage();


            Assert.NotNull(received.Id,"The message id should be defaulted to a new guid if not set");
        }


        [Test]
        public void Should_up_convert_the_native_type_to_the_enclosed_message_types_header_if_empty()
        {
            var address = Address.Parse(ReceiverQueue);

            var message = new TransportMessage();

            var typeName = typeof(MyMessage).FullName;

            using (var channel = connectionManager.GetPublishConnection().CreateModel())
            {
                var properties = channel.CreateBasicProperties();

                properties.MessageId = message.Id; 
                properties.Type = typeName;

                channel.BasicPublish(string.Empty, address.Queue, true, false, properties, message.Body);
            }

            var received = WaitForMessage();

            Assert.AreEqual(typeName, received.Headers[Headers.EnclosedMessageTypes]);
            Assert.AreEqual(typeof(MyMessage), Type.GetType(received.Headers[Headers.EnclosedMessageTypes]));
        }

        class MyMessage
        {

        }
    }
}