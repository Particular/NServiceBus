namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using Apache.NMS;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class DestinationEvaluatorTests
    {
        private readonly DestinationEvaluator testee;

        public DestinationEvaluatorTests()
        {
            testee = new DestinationEvaluator();
        }

        [Test]
        public void GetDestination_ForQueue_ShouldGetQueueDestinationFromSession()
        {
            const string QueueName = "Queue";
            var session = new Mock<ISession>();
            var destination = new Mock<IQueue>().Object;

            session.Setup(s => s.GetQueue(QueueName)).Returns(destination);

            var result = testee.GetDestination(session.Object, QueueName, "queue://");

            result.Should().BeSameAs(destination);
        }

        [Test]
        public void GetDestination_ForQueueWithQueuePrefix_ShouldGetQueueDestinationFromSession()
        {
            const string QueueName = "Queue";
            var session = new Mock<ISession>();
            var destination = new Mock<IQueue>().Object;

            session.Setup(s => s.GetQueue(QueueName)).Returns(destination);

            var result = testee.GetDestination(session.Object, "queue://" + QueueName, "queue://");

            result.Should().BeSameAs(destination);
        }
    
        [Test]
        public void GetDestination_ForQueueWithTempQueuePrefix_ShouldReturnTemporaryQueueDestination()
        {
            const string QueueName = "temp-queue://Queue";
            var session = new Mock<ISession>();

            var result = testee.GetDestination(session.Object, QueueName, "queue://");

            result.IsTemporary.Should().BeTrue();
            result.IsQueue.Should().BeTrue();
            result.ToString().Should().Be(QueueName);
        }

        [Test]
        public void GetDestination_ForTopic_ShouldGetTopicDestinationFromSession()
        {
            const string TopicName = "Topic";
            var session = new Mock<ISession>();
            var destination = new Mock<ITopic>().Object;

            session.Setup(s => s.GetTopic(TopicName)).Returns(destination);

            var result = testee.GetDestination(session.Object, TopicName, "topic://");

            result.Should().BeSameAs(destination);
        }

        [Test]
        public void GetDestination_ForTopicWithQueuePrefix_ShouldGetTopicDestinationFromSession()
        {
            const string TopicName = "Topic";
            var session = new Mock<ISession>();
            var destination = new Mock<ITopic>().Object;

            session.Setup(s => s.GetTopic(TopicName)).Returns(destination);

            var result = testee.GetDestination(session.Object, "topic://" + TopicName, "topic://");

            result.Should().BeSameAs(destination);
        }

        [Test]
        public void GetDestination_ForTopicWithTempQueuePrefix_ShouldReturnTemporaryTopicDestination()
        {
            const string TopicName = "temp-topic://Topic";
            var session = new Mock<ISession>();

            var result = testee.GetDestination(session.Object, TopicName, "topic://");

            result.IsTemporary.Should().BeTrue();
            result.IsTopic.Should().BeTrue();
            result.ToString().Should().Be(TopicName);
        }
    }
}
