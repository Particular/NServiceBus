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
            subscribedTopics = new List<string>();
            unsubscribedTopics = new List<string>();

            topicEvaluatorMock = new Mock<ITopicEvaluator>();
            testee = new SubscriptionManager(topicEvaluatorMock.Object);
        }

        [Test]
        public void WhenATopicIsSubscribed_ThenItIsReturnedWhenSomeoneSubscribesLater()
        {
            SetupTypeToTopicMapping(defaultType, DefaultTopic);

            testee.Subscribe(defaultType, Address.Local);
            var subscribedTopics = testee.Register(this);

            subscribedTopics.Should().Equal(new object[] { DefaultTopic });
        }

        [Test]
        public void WhenATopicIsSubscribed_ThenTopicSubscribedShouldBeRaised()
        {
            SetupTypeToTopicMapping(defaultType, DefaultTopic);

            testee.Register(this);
            testee.Subscribe(defaultType, Address.Local);

            subscribedTopics.Should().BeEquivalentTo(new object[] { DefaultTopic });
        }

        [Test]
        public void WhenAnAlreadySubscribedTopicIsSubscribed_ThenGetTopicsShouldContainItOnce()
        {
            SetupTypeToTopicMapping(defaultType, DefaultTopic);

            testee.Subscribe(defaultType, Address.Local);
            testee.Subscribe(defaultType, Address.Local);
            var topics = testee.Register(this);

            topics.Should().Equal(new object[] { DefaultTopic });
        }

        [Test]
        public void WhenAnAlreadySubscribedTopicIsSubscribed_ThenTopicSubscribedShouldBeRaised()
        {
            SetupTypeToTopicMapping(defaultType, DefaultTopic);

            testee.Subscribe(defaultType, Address.Local);
            testee.Register(this);
            testee.Subscribe(defaultType, Address.Local);

            subscribedTopics.Should().BeEmpty();
        }

        [Test]
        public void WhenATopicIsUnsubscribed_ThenGetTopicsShouldNotContainItAnymore()
        {
            SetupTypeToTopicMapping(defaultType, DefaultTopic);

            testee.Subscribe(defaultType, Address.Local);
            testee.Unsubscribe(defaultType, Address.Local);
            var topics = testee.Register(this);

            topics.Should().BeEmpty();
        }

        [Test]
        public void WhenATopicIsUnsubscribed_ThenTopicUnubscribedShouldBeRaised()
        {
            SetupTypeToTopicMapping(defaultType, DefaultTopic);

            testee.Subscribe(defaultType, Address.Local);
            testee.Register(this);
            testee.Unsubscribe(defaultType, Address.Local);

            unsubscribedTopics.Should().BeEquivalentTo(new object[] { DefaultTopic });
        }

        private void SetupTypeToTopicMapping(Type type, string Topic)
        {
            topicEvaluatorMock.Setup(te => te.GetTopicFromMessageType(type)).Returns(Topic);
        }
        public void TopicSubscribed(object sender, SubscriptionEventArgs e)
        {
            subscribedTopics.Add(e.Topic);
        }

        public void TopicUnsubscribed(object sender, SubscriptionEventArgs e)
        {
            unsubscribedTopics.Add(e.Topic);
        }
    }
}
