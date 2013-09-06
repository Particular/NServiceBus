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
            messageProducerMock = new Mock<IMessageProducer>();
            topicEvaluatorMock = new Mock<ITopicEvaluator>();
            testee = new ActiveMqMessagePublisher(topicEvaluatorMock.Object, messageProducerMock.Object);
        }

        [Test]
        public void WhenPublishingAMessage_ThenItIsSentUsingTheMessageProducer()
        {
            const string Topic = "TheTopic";
            var message = new TransportMessage();
            var type = GetType();

            topicEvaluatorMock.Setup(te => te.GetTopicFromMessageType(type)).Returns(Topic);

            testee.Publish(message, new[] { type });

            messageProducerMock.Verify(mp => mp.SendMessage(message, Topic, "topic://"));
        }
    }
}