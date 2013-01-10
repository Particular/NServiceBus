﻿namespace NServiceBus.Transport.ActiveMQ
{
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqMessageSenderTests
    {
        private Mock<IMessageProducer> messageProducerMock;
        private ActiveMqMessageSender testee;

        [SetUp]
        public void SetUp()
        {
            this.messageProducerMock = new Mock<IMessageProducer>();
            this.testee = new ActiveMqMessageSender(this.messageProducerMock.Object);
        }

        [Test]
        public void WhenSendingAMessage_ThenItIsSentUsingTheMessageProducer()
        {
            const string Queue = "QueueName";
            var message = new TransportMessage();

            this.testee.Send(message, new Address(Queue, "SomeMachineName", true));

            this.messageProducerMock.Verify(mp => mp.SendMessage(message, Queue, "queue://"));
        }
    }
}
