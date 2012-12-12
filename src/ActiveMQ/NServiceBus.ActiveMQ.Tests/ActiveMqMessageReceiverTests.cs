namespace NServiceBus.ActiveMQ
{
    using System;

    using Apache.NMS;

    using FluentAssertions;

    using Moq;

    using NServiceBus.Unicast.Transport;

    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqMessageReceiverTests
    {
        private ActiveMqMessageReceiver testee;

        private Mock<ISessionFactory> sessionFactoryMock;
        private Mock<IActiveMqMessageMapper> activeMqMessageMapperMock;
        private Mock<ISubscriptionManager> subscriptionManagerMock;

        private Mock<INetTxSession> session;

        private Mock<IActiveMqPurger> purger;

        private Mock<IMessageConsumer> consumer;

        [SetUp] 
        public void SetUp()
        {
            this.sessionFactoryMock = new Mock<ISessionFactory>();
            this.activeMqMessageMapperMock = new Mock<IActiveMqMessageMapper>();
            this.subscriptionManagerMock = new Mock<ISubscriptionManager>();
            this.purger = new Mock<IActiveMqPurger>();

            this.testee = new ActiveMqMessageReceiver(
                this.sessionFactoryMock.Object, 
                this.activeMqMessageMapperMock.Object, 
                this.subscriptionManagerMock.Object,
                this.purger.Object);
        }

        [Test]
        public void WhenMessageIsReceived_ThenMessageReceivedIsRaised()
        {
            const string Queue = "somequeue";
            var messageMock = new Mock<IMessage>();
            var transportMessage = new TransportMessage();
            TransportMessageReceivedEventArgs receivedEvent = null;

            this.testee.MessageReceived += (sender, e) => receivedEvent = e;
            this.SetupMapMessageToTransportMessage(messageMock.Object, transportMessage);

            this.StartTestee(new Address(Queue, "machine"));
            this.consumer.Raise(c => c.Listener += null, messageMock.Object);

            receivedEvent.Should().NotBeNull();
            receivedEvent.Message.Should().Be(transportMessage);
        }

        [Test]
        public void WhenSubscriptionIsAddedToReceiverWithLocalAddress_ThenTopicIsSubscribed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";
            var message = new Mock<IMessage>().Object;
            TransportMessageReceivedEventArgs receivedEvent = null;
            var transportMessage = new TransportMessage();

            this.testee.MessageReceived += (sender, e) => receivedEvent = e;
            this.testee.ConsumerName = ConsumerName;
            this.StartTesteeWithLocalAddress();

            this.SetupMapMessageToTransportMessage(message, transportMessage);
            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.RaiseTopicSubscribed(Topic);
            this.RaiseEventReceived(topicConsumer, message);

            receivedEvent.Should().NotBeNull();
            receivedEvent.Message.Should().Be(transportMessage);
        }

        [Test]
        public void WhenSubscriptionIsAddedToReceiverWithLocalAddressAndPurgeRequired_ThenPurge()
        {
            const string AnyTopic = "SomeTopic";

            this.testee.PurgeOnStartup = true;
            this.testee.ConsumerName = "A";

            this.StartTesteeWithLocalAddress();
            var destination = this.SetupGetQueue(this.session, string.Format("Consumer.{0}.{1}", "A", AnyTopic));

            this.RaiseTopicSubscribed(AnyTopic);

            this.purger.Verify(s => s.Purge(It.IsAny<ISession>(), destination));
        }

        [Test]
        public void WhenSubscriptionIsAddedToReceiverWithLocalAddressAndPurgeNotRequired_ThenNotPurge()
        {
            this.testee.PurgeOnStartup = false;

            this.StartTesteeWithLocalAddress();

            this.RaiseTopicSubscribed("AnyTopic");

            this.purger.Verify(s => s.Purge(It.IsAny<ISession>(), It.IsAny<IDestination>()), Times.Never());
        }

        [Test]
        public void WhenSubscriptionIsAddedToReceiverWithNotLocalAddress_ThenTopicIsNotSubscribed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";
            var messageMock = new Mock<IMessage>();
            TransportMessageReceivedEventArgs receivedEvent = null;

            this.testee.MessageReceived += (sender, e) => receivedEvent = e;
            this.testee.ConsumerName = ConsumerName;
            this.StartTestee(new Address("queue", "machine"));

            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.RaiseTopicSubscribed(Topic);
            topicConsumer.Raise(c => c.Listener += null, messageMock.Object);

            receivedEvent.Should().BeNull();
        }

        [Test]
        public void WhenTopicIsUnsubscribed_ThenConsumerIsDisposed()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";

            this.testee.ConsumerName = ConsumerName;
            this.StartTesteeWithLocalAddress();

            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.RaiseTopicSubscribed(Topic);
            this.RaiseTopicUnsubscribed(Topic);

            topicConsumer.Verify(c => c.Dispose());
        }

        [Test]
        public void Start_WhenPurgeRequired_ThenPurge()
        {
            this.testee.PurgeOnStartup = true;

            this.StartTestee(new Address("anyqueue", "anymachine"));

            this.purger.Verify(s => s.Purge(this.session.Object, It.Is<IQueue>(d => d.QueueName.Contains("anyqueue"))));
        }

        [Test]
        public void Start_WhenPurgeNotRequired_ThenNotPurge()
        {
            this.testee.PurgeOnStartup = false;

            this.StartTestee(new Address("anyqueue", "anymachine"));

            this.purger.Verify(s => s.Purge(It.IsAny<ISession>(), It.IsAny<IDestination>()), Times.Never());
        }

        [Test]
        public void Dispose_ShouldReleaseResources()
        {
            const string Topic = "SomeTopic";
            const string ConsumerName = "A";

            this.testee.ConsumerName = ConsumerName;
            this.StartTesteeWithLocalAddress();

            var topicConsumer = this.SetupCreateConsumer(this.session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.RaiseTopicSubscribed(Topic);

            this.testee.Dispose();

            topicConsumer.Verify(c => c.Close());
            topicConsumer.Verify(c => c.Dispose());

            this.consumer.Verify(c => c.Close());
            this.consumer.Verify(c => c.Dispose());
        }

        private void StartTesteeWithLocalAddress()
        {
            Address.InitializeLocalAddress("somequeue");
            this.StartTestee(Address.Local);
        }
        
        private void StartTestee(Address address)
        {
            this.session = this.SetupCreateSession();

            this.consumer = this.SetupCreateConsumer(this.session, address.Queue);

            this.testee.Start(address);
        }

        private IQueue SetupGetQueue(Mock<INetTxSession> sessionMock, string queue)
        {
            var destinationMock = new Mock<IQueue>();
            sessionMock.Setup(s => s.GetQueue(queue)).Returns(destinationMock.Object);
            destinationMock.Setup(destination => destination.QueueName).Returns(queue);
            return destinationMock.Object;
        }
        
        private Mock<IMessageConsumer> SetupCreateConsumer(Mock<INetTxSession> sessionMock, IDestination destination)
        {
            var consumerMock = new Mock<IMessageConsumer>();
            sessionMock.Setup(s => s.CreateConsumer(destination)).Returns(consumerMock.Object);
            return consumerMock;
        }

        private Mock<IMessageConsumer> SetupCreateConsumer(Mock<INetTxSession> sessionMock, string queue)
        {
            var destination = this.SetupGetQueue(this.session, queue);
            return this.SetupCreateConsumer(sessionMock, destination);
        }

        private Mock<INetTxSession> SetupCreateSession()
        {
            var sessionMock = new Mock<INetTxSession> { DefaultValue = DefaultValue.Mock };
            this.sessionFactoryMock.Setup(c => c.CreateSession()).Returns(sessionMock.Object);
            return sessionMock;
        }

        private void SetupMapMessageToTransportMessage(IMessage messageMock, TransportMessage transportMessage)
        {
            this.activeMqMessageMapperMock.Setup(m => m.CreateTransportMessage(messageMock)).Returns(transportMessage);
        }

        private void RaiseTopicSubscribed(string topic)
        {
            this.subscriptionManagerMock.Raise(sm => sm.TopicSubscribed += null, new SubscriptionEventArgs(topic));
        }

        private void RaiseTopicUnsubscribed(string topic)
        {
            this.subscriptionManagerMock.Raise(sm => sm.TopicUnsubscribed += null, new SubscriptionEventArgs(topic));
        }

        private void RaiseEventReceived(Mock<IMessageConsumer> topicConsumer, IMessage message)
        {
            topicConsumer.Raise(c => c.Listener += null, message);
        }
    }
}