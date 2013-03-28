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
            Address.SetParser<ActiveMQAddress>();

            this.messageProducerMock = new Mock<IMessageProducer>();
            this.testee = new ActiveMqMessageSender(this.messageProducerMock.Object);
        }

        [Test]
        public void WhenSendingAMessage_ThenItIsSentUsingTheMessageProducer()
        {
            const string Queue = "QueueName";
            var message = new TransportMessage();

            this.testee.Send(message, new ActiveMQAddress(Queue));

            this.messageProducerMock.Verify(mp => mp.SendMessage(message, Queue, "queue://"));
        }
    }
}
