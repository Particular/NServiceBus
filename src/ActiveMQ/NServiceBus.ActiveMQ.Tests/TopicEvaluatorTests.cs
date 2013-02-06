namespace NServiceBus.Transport.ActiveMQ
{
    using FluentAssertions;

    using NUnit.Framework;

    [TestFixture]
    public class TopicEvaluatorTests
    {
        private TopicEvaluator testee;

        [SetUp]
        public void SetUp()
        {
            this.testee = new TopicEvaluator();
        }    

        [Test]
        public void GetTopicFromMessageType_ShouldReturnTheFirstMessageTypePreceededByVirtualTopic()
        {
            var topic = this.testee.GetTopicFromMessageType(typeof(ISimpleMessage));

            topic.Should().Be("VirtualTopic." + typeof(ISimpleMessage).Name);
        }
    }

    public interface ISimpleMessage
    {
    }
}