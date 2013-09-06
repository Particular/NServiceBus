namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using FluentAssertions;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ;

    [TestFixture]
    public class TopicEvaluatorTests
    {
        private TopicEvaluator testee;

        [SetUp]
        public void SetUp()
        {
            testee = new TopicEvaluator();
        }    

        [Test]
        public void GetTopicFromMessageType_ShouldReturnTheFirstMessageTypePreceededByVirtualTopic()
        {
            var topic = testee.GetTopicFromMessageType(typeof(ISimpleMessage));

            topic.Should().Be("VirtualTopic." + typeof(ISimpleMessage).FullName);
        }
    }

    public interface ISimpleMessage
    {
    }
}