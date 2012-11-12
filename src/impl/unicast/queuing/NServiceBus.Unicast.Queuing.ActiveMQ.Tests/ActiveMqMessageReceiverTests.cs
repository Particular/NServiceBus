namespace NServiceBus.Unicast.Queuing.ActiveMQ.Tests
{
    using Apache.NMS;
    using FluentAssertions;
    using Moq;
    using NServiceBus.Unicast.Transport;
    using NUnit.Framework;


    [TestFixture]
    public class ActiveMqMessageReceiverTests
    {
        private ActiveMqMessageReceiver testee;

        private Mock<INetTxConnection> connectionMock;
        private Mock<IActiveMqMessageMapper> activeMqMessageMapperMock;
        private Mock<ISubscriptionManager> subscriptionManagerMock;

        private Mock<INetTxSession> session;
        private Mock<IMessageConsumer> consumer;

        [SetUp] 
        public void SetUp()
        {
            this.connectionMock = new Mock<INetTxConnection>();
            this.activeMqMessageMapperMock = new Mock<IActiveMqMessageMapper>();
            this.subscriptionManagerMock = new Mock<ISubscriptionManager>();
            this.testee = new ActiveMqMessageReceiver(
                this.connectionMock.Object, 
                this.activeMqMessageMapperMock.Object, 
                this.subscriptionManagerMock.Object);
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

            this.Initialize(new Address(Queue, "machine"));
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
            this.InitializeWithLocalAddress();

            this.SetupMapMessageToTransportMessage(message, transportMessage);
            var topicConsumer = this.SetupCreateConsumer(session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.RaiseTopicSubscribed(Topic);
            this.RaiseEventReceived(topicConsumer, message);

            receivedEvent.Should().NotBeNull();
            receivedEvent.Message.Should().Be(transportMessage);
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
            this.Initialize(new Address("queue", "machine"));

            var topicConsumer = this.SetupCreateConsumer(session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

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
            this.InitializeWithLocalAddress();

            var topicConsumer = this.SetupCreateConsumer(session, string.Format("Consumer.{0}.{1}", ConsumerName, Topic));

            this.RaiseTopicSubscribed(Topic);
            this.RaiseTopicUnsubscribed(Topic);

            topicConsumer.Verify(c => c.Dispose());
        }

        private void InitializeWithLocalAddress()
        {
            Address.InitializeLocalAddress("somequeue");
            this.Initialize(Address.Local);
        }
        
        private void Initialize(Address address)
        {
            this.session = this.SetupCreateSession();

            this.consumer = this.SetupCreateConsumer(session, address.Queue);

            this.testee.Init(address, true);
        }

        private IQueue SetupGetQueue(Mock<INetTxSession> sessionMock, string queue)
        {
            var destination = new Mock<IQueue>().Object;
            sessionMock.Setup(s => s.GetQueue(queue)).Returns(destination);
            return destination;
        }
        
        private Mock<IMessageConsumer> SetupCreateConsumer(Mock<INetTxSession> sessionMock, IDestination destination)
        {
            var consumerMock = new Mock<IMessageConsumer>();
            sessionMock.Setup(s => s.CreateConsumer(destination)).Returns(consumerMock.Object);
            return consumerMock;
        }

        private Mock<IMessageConsumer> SetupCreateConsumer(Mock<INetTxSession> sessionMock, string queue)
        {
            var destination = this.SetupGetQueue(session, queue);
            return this.SetupCreateConsumer(sessionMock, destination);
        }

        private Mock<INetTxSession> SetupCreateSession()
        {
            var sessionMock = new Mock<INetTxSession>();
            this.connectionMock.Setup(c => c.CreateNetTxSession()).Returns(sessionMock.Object);
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