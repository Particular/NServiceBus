namespace NServiceBus.Unicast.Queuing.ActiveMQ.Tests
{
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    class SubscriptionManagerTests
    {
        private SubscriptionManager testee;

        [SetUp]
        public void SetUp()
        {
            this.testee = new SubscriptionManager();
        }

        [Test]
        public void WhenATopicIsSubscribed_ThenGetTopicsShouldContainIt()
        {
            const string Topic = "SomeTopic";

            this.testee.Subscribe(Topic);
            var subscribedTopics = this.testee.GetTopics();

            subscribedTopics.Should().Equal(new object[] { Topic });
        }

        [Test]
        public void WhenATopicIsSubscribed_ThenTopicSubscribedShouldBeRaised()
        {
            const string Topic = "SomeTopic";
            this.testee.MonitorEvents();

            this.testee.Subscribe(Topic);

            this.testee.ShouldRaise("TopicSubscribed").WithArgs<SubscriptionEventArgs>(e => e.Topic == Topic);
        }

        [Test]
        public void WhenAnAlreadySubscribedTopicIsSubscribed_ThenGetTopicsShouldContainItOnce()
        {
            const string Topic = "SomeTopic";

            this.testee.Subscribe(Topic);
            var subscribedTopics = this.testee.GetTopics();

            subscribedTopics.Should().Equal(new object[] { Topic });
        }
        
        [Test]
        public void WhenAnAlreadySubscribedTopicIsSubscribed_ThenTopicSubscribedShouldBeRaised()
        {
            const string Topic = "SomeTopic";
            this.testee.Subscribe(Topic);
            this.testee.MonitorEvents();

            this.testee.Subscribe(Topic);

            this.testee.ShouldNotRaise("TopicSubscribed");
        }

        [Test]
        public void WhenATopicIsUnsubscribed_ThenGetTopicsShouldNotContainIt()
        {
            const string Topic = "SomeTopic";

            this.testee.Subscribe(Topic);
            this.testee.Unsubscribe(Topic);
            var subscribedTopics = this.testee.GetTopics();

            subscribedTopics.Should().Equal(new object[] { });
        }

        [Test]
        public void WhenATopicIsUnsubscribed_ThenTopicUnubscribedShouldBeRaised()
        {
            const string Topic = "SomeTopic";
            this.testee.MonitorEvents();

            this.testee.Subscribe(Topic);

            this.testee.ShouldRaise("TopicSubscribed").WithArgs<SubscriptionEventArgs>(e => e.Topic == Topic);
        }
    }
}
