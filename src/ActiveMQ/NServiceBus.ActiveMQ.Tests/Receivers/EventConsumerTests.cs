namespace NServiceBus.Transports.ActiveMQ.Tests.Receivers
{
    using System;
    using System.Collections.Generic;
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ;
    using NServiceBus.Transports.ActiveMQ.Receivers;

    [TestFixture]
    public class EventConsumerTests
    {
        private EventConsumer testee;
        private Mock<IProcessMessages> messageProcessorMock;
        private NotifyTopicSubscriptionsMock subscriptionManagerMock;
        
        [SetUp]
        public void SetUp()
        {
            messageProcessorMock = new Mock<IProcessMessages>();
            subscriptionManagerMock = new NotifyTopicSubscriptionsMock();

            testee = new EventConsumer(subscriptionManagerMock, messageProcessorMock.Object);
        }

        [Test]
        public void WhenSubscriptionIsAddedToStartedConsumer_ThenTopicIsSubscribed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";
            var message = new Mock<IMessage>().Object;

            var topicConsumer = SetupCreateConsumer(string.Format("queue://Consumer.{0}.{1}", ConsumerName, Topic));

            testee.ConsumerName = ConsumerName;
            testee.Start();
            subscriptionManagerMock.RaiseTopicSubscribed(Topic);
            RaiseEventReceived(topicConsumer, message);

            messageProcessorMock.Verify(mp => mp.ProcessMessage(message));
        }

        [Test]
        public void WhenEventReceivedAfterStop_ThenProcessorIsNotInvoked()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";
            var message = new Mock<IMessage>().Object;

            var topicConsumer = SetupCreateConsumer(string.Format("queue://Consumer.{0}.{1}", ConsumerName, Topic));

            testee.ConsumerName = ConsumerName;
            testee.Start();
            subscriptionManagerMock.RaiseTopicSubscribed(Topic);
            testee.Stop();
            RaiseEventReceived(topicConsumer, message);

            messageProcessorMock.Verify(mp => mp.ProcessMessage(message), Times.Never());
        }
        
        [Test]
        public void WhenSubscriptionIsAddedToNotStartedConsumer_ThenTopicIsNotSubscribed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";
            var message = new Mock<IMessage>().Object;

            var topicConsumer = SetupCreateConsumer(string.Format("queue://Consumer.{0}.{1}", ConsumerName, Topic));

            testee.ConsumerName = ConsumerName;
            subscriptionManagerMock.RaiseTopicSubscribed(Topic);
            RaiseEventReceived(topicConsumer, message);

            messageProcessorMock.Verify(mp => mp.ProcessMessage(It.IsAny<IMessage>()), Times.Never());
        }

        [Test]
        public void WhenStarted_ThenCurrentTopicAreSubscribed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";
            var message = new Mock<IMessage>().Object;

            var topicConsumer = SetupCreateConsumer(string.Format("queue://Consumer.{0}.{1}", ConsumerName, Topic));
            subscriptionManagerMock.InitialTopics.Add(Topic);

            testee.ConsumerName = ConsumerName;
            testee.Start();
            RaiseEventReceived(topicConsumer, message);

            messageProcessorMock.Verify(mp => mp.ProcessMessage(message));
        }

        [Test]
        public void WhenTopicIsUnsubscribed_ThenConsumerIsDisposed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";

            var topicConsumer = SetupCreateConsumer(string.Format("queue://Consumer.{0}.{1}", ConsumerName, Topic));

            testee.ConsumerName = ConsumerName;
            testee.Start();
            subscriptionManagerMock.RaiseTopicSubscribed(Topic);
            subscriptionManagerMock.RaiseTopicUnsubscribed(Topic);

            topicConsumer.Verify(c => c.Dispose());
        }

        [Test]
        public void WhenDisposed_ThenConsumersAreDisposed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";

            var topicConsumer = SetupCreateConsumer(string.Format("queue://Consumer.{0}.{1}", ConsumerName, Topic));

            testee.ConsumerName = ConsumerName;
            testee.Start();
            subscriptionManagerMock.RaiseTopicSubscribed(Topic);
            testee.Stop();
            testee.Dispose();

            topicConsumer.Verify(c => c.Dispose());
        }

        [Test]
        public void WhenConsumerNameHasDot_ThenItIsReplacedByDashForSubscriptions_SoThatTheVirtualTopicNamePatternIsNotBroken()
        {
            const string Topic = "Some.Topic";
            const string ConsumerName = "A.B.C";
            const string ExpectedConsumerName = "A-B-C";
            var message = new Mock<IMessage>().Object;

            var topicConsumer = SetupCreateConsumer(string.Format("queue://Consumer.{0}.{1}", ExpectedConsumerName, Topic));

            testee.ConsumerName = ConsumerName;
            testee.Start();
            subscriptionManagerMock.RaiseTopicSubscribed(Topic);
            RaiseEventReceived(topicConsumer, message);

            messageProcessorMock.Verify(mp => mp.ProcessMessage(message));
        }

        private Mock<IMessageConsumer> SetupCreateConsumer(string queue)
        {
            var consumerMock = new Mock<IMessageConsumer>();
            messageProcessorMock.Setup(mp => mp.CreateMessageConsumer(queue)).Returns(consumerMock.Object);
            return consumerMock;
        }

        private IQueue SetupGetQueue(Mock<ISession> sessionMock, string queue)
        {
            var destinationMock = new Mock<IQueue>();
            sessionMock.Setup(s => s.GetQueue(queue)).Returns(destinationMock.Object);
            destinationMock.Setup(destination => destination.QueueName).Returns(queue);
            return destinationMock.Object;
        }

        private void RaiseEventReceived(Mock<IMessageConsumer> topicConsumer, IMessage message)
        {
            topicConsumer.Raise(c => c.Listener += null, message);
        }

        private class NotifyTopicSubscriptionsMock : INotifyTopicSubscriptions
        {
            public event EventHandler<SubscriptionEventArgs> TopicSubscribed = delegate { };
            public event EventHandler<SubscriptionEventArgs> TopicUnsubscribed = delegate { };
            public List<string> InitialTopics = new List<string>();

            public IEnumerable<string> Register(ITopicSubscriptionListener listener)
            {
                TopicSubscribed += listener.TopicSubscribed;
                TopicUnsubscribed += listener.TopicUnsubscribed;

                return InitialTopics;
            }

            public void Unregister(ITopicSubscriptionListener listener)
            {
                TopicSubscribed -= listener.TopicSubscribed;
                TopicUnsubscribed -= listener.TopicUnsubscribed;
            }

            public void RaiseTopicSubscribed(string topic)
            {
                TopicSubscribed(this, new SubscriptionEventArgs(topic));
            }

            public void RaiseTopicUnsubscribed(string topic)
            {
                TopicUnsubscribed(this, new SubscriptionEventArgs(topic));
            }
        }
    }
}