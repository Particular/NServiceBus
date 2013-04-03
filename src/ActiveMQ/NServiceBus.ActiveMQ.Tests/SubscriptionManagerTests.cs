namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using System;
    using System.Collections.Generic;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ;

    [TestFixture]
    class SubscriptionManagerTests : ITopicSubscriptionListener
    {
        private const string DefaultTopic = "TheDefaultTopic";
        private readonly Type defaultType = typeof(SubscriptionManagerTests);

        private SubscriptionManager testee;
        private Mock<ITopicEvaluator> topicEvaluatorMock;

        private IList<string> subscribedTopics;
        private IList<string> unsubscribedTopics;

        [SetUp]
        public void SetUp()
        {
            this.subscribedTopics = new List<string>();
            this.unsubscribedTopics = new List<string>();

            this.topicEvaluatorMock = new Mock<ITopicEvaluator>();
            this.testee = new SubscriptionManager(this.topicEvaluatorMock.Object);
        }

        [Test]
        public void WhenATopicIsSubscribed_ThenItIsReturnedWhenSomeoneSubscribesLater()
        {
            this.SetupTypeToTopicMapping(defaultType, DefaultTopic);

            this.testee.Subscribe(defaultType, Address.Local);
            var subscribedTopics = this.testee.Register(this);

            subscribedTopics.Should().Equal(new object[] { DefaultTopic });
        }

        [Test]
        public void WhenATopicIsSubscribed_ThenTopicSubscribedShouldBeRaised()
        {
            this.SetupTypeToTopicMapping(defaultType, DefaultTopic);

            this.testee.Register(this);
            this.testee.Subscribe(defaultType, Address.Local);

            this.subscribedTopics.Should().BeEquivalentTo(new object[] { DefaultTopic });
        }

        [Test]
        public void WhenAnAlreadySubscribedTopicIsSubscribed_ThenGetTopicsShouldContainItOnce()
        {
            this.SetupTypeToTopicMapping(defaultType, DefaultTopic);

            this.testee.Subscribe(defaultType, Address.Local);
            this.testee.Subscribe(defaultType, Address.Local);
            var topics = this.testee.Register(this);

            topics.Should().Equal(new object[] { DefaultTopic });
        }

        [Test]
        public void WhenAnAlreadySubscribedTopicIsSubscribed_ThenTopicSubscribedShouldBeRaised()
        {
            this.SetupTypeToTopicMapping(defaultType, DefaultTopic);

            this.testee.Subscribe(defaultType, Address.Local);
            this.testee.Register(this);
            this.testee.Subscribe(defaultType, Address.Local);

            this.subscribedTopics.Should().BeEmpty();
        }

        [Test]
        public void WhenATopicIsUnsubscribed_ThenGetTopicsShouldNotContainItAnymore()
        {
            this.SetupTypeToTopicMapping(defaultType, DefaultTopic);

            this.testee.Subscribe(defaultType, Address.Local);
            this.testee.Unsubscribe(defaultType, Address.Local);
            var topics = this.testee.Register(this);

            topics.Should().BeEmpty();
        }

        [Test]
        public void WhenATopicIsUnsubscribed_ThenTopicUnubscribedShouldBeRaised()
        {
            this.SetupTypeToTopicMapping(defaultType, DefaultTopic);

            this.testee.Subscribe(defaultType, Address.Local);
            this.testee.Register(this);
            this.testee.Unsubscribe(defaultType, Address.Local);

            this.unsubscribedTopics.Should().BeEquivalentTo(new object[] { DefaultTopic });
        }

        private void SetupTypeToTopicMapping(Type type, string Topic)
        {
            this.topicEvaluatorMock.Setup(te => te.GetTopicFromMessageType(type)).Returns(Topic);
        }
        public void TopicSubscribed(object sender, SubscriptionEventArgs e)
        {
            this.subscribedTopics.Add(e.Topic);
        }

        public void TopicUnsubscribed(object sender, SubscriptionEventArgs e)
        {
            this.unsubscribedTopics.Add(e.Topic);
        }
    }
}
