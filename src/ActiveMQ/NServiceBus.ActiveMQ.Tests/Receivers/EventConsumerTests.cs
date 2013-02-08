namespace NServiceBus.Transport.ActiveMQ.Receivers
{
    using System;
    using System.Collections.Generic;

    using Apache.NMS;

    using Moq;

    using NServiceBus.Transport.ActiveMQ;

    using NUnit.Framework;

    [TestFixture]
    public class EventConsumerTests
    {
        private EventConsumer testee;
        private Mock<IProcessMessages> messageProcessorMock;
        private NotifyTopicSubscriptionsMock subscriptionManagerMock;
        private Mock<ISession> session;

        [SetUp]
        public void SetUp()
        {
            this.session = new Mock<ISession>();

            this.messageProcessorMock = new Mock<IProcessMessages>();
            this.subscriptionManagerMock = new NotifyTopicSubscriptionsMock();

            this.testee = new EventConsumer(this.subscriptionManagerMock, this.messageProcessorMock.Object);
        }

        [Test]
        public void WhenSubscriptionIsAddedToStartedConsumer_ThenTopicIsSubscribed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";
            var message = new Mock<IMessage>().Object;

            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.testee.ConsumerName = ConsumerName;
            this.testee.Start();
            this.subscriptionManagerMock.RaiseTopicSubscribed(Topic);
            this.RaiseEventReceived(topicConsumer, message);

            this.messageProcessorMock.Verify(mp => mp.ProcessMessage(message));
        }

        [Test]
        public void WhenEventReceivedAfterStop_ThenProcessorIsNotInvoked()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";
            var message = new Mock<IMessage>().Object;

            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.testee.ConsumerName = ConsumerName;
            this.testee.Start();
            this.subscriptionManagerMock.RaiseTopicSubscribed(Topic);
            this.testee.Stop();
            this.RaiseEventReceived(topicConsumer, message);

            this.messageProcessorMock.Verify(mp => mp.ProcessMessage(message), Times.Never());
        }
        
        [Test]
        public void WhenSubscriptionIsAddedToNotStartedConsumer_ThenTopicIsNotSubscribed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";
            var message = new Mock<IMessage>().Object;

            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.testee.ConsumerName = ConsumerName;
            this.subscriptionManagerMock.RaiseTopicSubscribed(Topic);
            this.RaiseEventReceived(topicConsumer, message);

            this.messageProcessorMock.Verify(mp => mp.ProcessMessage(It.IsAny<IMessage>()), Times.Never());
        }

        [Test]
        public void WhenStarted_ThenCurrentTopicAreSubscribed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";
            var message = new Mock<IMessage>().Object;

            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));
            this.subscriptionManagerMock.InitialTopics.Add(Topic);

            this.testee.ConsumerName = ConsumerName;
            this.testee.Start();
            this.RaiseEventReceived(topicConsumer, message);

            this.messageProcessorMock.Verify(mp => mp.ProcessMessage(message));
        }

        [Test]
        public void WhenTopicIsUnsubscribed_ThenConsumerIsDisposed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";

            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.testee.ConsumerName = ConsumerName;
            this.testee.Start();
            this.subscriptionManagerMock.RaiseTopicSubscribed(Topic);
            this.subscriptionManagerMock.RaiseTopicUnsubscribed(Topic);

            topicConsumer.Verify(c => c.Dispose());
        }

        [Test]
        public void WhenDisposed_ThenConsumersAreDisposed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";

            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.testee.ConsumerName = ConsumerName;
            this.testee.Start();
            this.subscriptionManagerMock.RaiseTopicSubscribed(Topic);
            this.testee.Stop();
            this.testee.Dispose();

            topicConsumer.Verify(c => c.Dispose());
        }

        private Mock<IMessageConsumer> SetupCreateConsumer(Mock<ISession> sessionMock, IDestination destination)
        {
            var consumerMock = new Mock<IMessageConsumer>();
            sessionMock.Setup(s => s.CreateConsumer(destination)).Returns(consumerMock.Object);
            return consumerMock;
        }

        private Mock<IMessageConsumer> SetupCreateConsumer(Mock<ISession> sessionMock, string queue)
        {
            var destination = this.SetupGetQueue(this.session, queue);
            return this.SetupCreateConsumer(sessionMock, destination);
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
                this.TopicSubscribed += listener.TopicSubscribed;
                this.TopicUnsubscribed += listener.TopicUnsubscribed;

                return this.InitialTopics;
            }

            public void Unregister(ITopicSubscriptionListener listener)
            {
                this.TopicSubscribed -= listener.TopicSubscribed;
                this.TopicUnsubscribed -= listener.TopicUnsubscribed;
            }

            public void RaiseTopicSubscribed(string topic)
            {
                this.TopicSubscribed(this, new SubscriptionEventArgs(topic));
            }

            public void RaiseTopicUnsubscribed(string topic)
            {
                this.TopicUnsubscribed(this, new SubscriptionEventArgs(topic));
            }
        }
    }
}