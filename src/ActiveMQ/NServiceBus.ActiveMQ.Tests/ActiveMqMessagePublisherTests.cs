namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ;

    [TestFixture]
    public class ActiveMqMessagePublisherTests
    {
        private Mock<IMessageProducer> messageProducerMock;
        private Mock<ITopicEvaluator> topicEvaluatorMock;
        private ActiveMqMessagePublisher testee;

        [SetUp]
        public void SetUp()
        {
            this.messageProducerMock = new Mock<IMessageProducer>();
            this.topicEvaluatorMock = new Mock<ITopicEvaluator>();
            this.testee = new ActiveMqMessagePublisher(this.topicEvaluatorMock.Object, this.messageProducerMock.Object);
        }

        [Test]
        public void WhenPublishingAMessage_ThenItIsSentUsingTheMessageProducer()
        {
            const string Topic = "TheTopic";
            var message = new TransportMessage();
            var type = this.GetType();

            this.topicEvaluatorMock.Setup(te => te.GetTopicFromMessageType(type)).Returns(Topic);

            this.testee.Publish(message, new[] { type });

            this.messageProducerMock.Verify(mp => mp.SendMessage(message, Topic, "topic://"));
        }
    }
}