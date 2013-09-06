namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ;

    [TestFixture]
    public class ActiveMqMessageSenderTests
    {
        private Mock<IMessageProducer> messageProducerMock;
        private ActiveMqMessageSender testee;

        [SetUp]
        public void SetUp()
        {
            messageProducerMock = new Mock<IMessageProducer>();
            testee = new ActiveMqMessageSender(messageProducerMock.Object);
        }

        [Test]
        public void WhenSendingAMessage_ThenItIsSentUsingTheMessageProducer()
        {
            const string Queue = "QueueName";
            var message = new TransportMessage();

            testee.Send(message, new Address(Queue, "SomeMachineName"));

            messageProducerMock.Verify(mp => mp.SendMessage(message, Queue, "queue://"));
        }
    }
}
