namespace NServiceBus.Unicast.Queuing.ActiveMQ.Tests
{
    using System.Collections.Generic;

    using Apache.NMS;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqMessageSenderTests
    {
        private ActiveMqMessageSender testee;
        private Mock<INetTxConnection> connectionMock;
        private Mock<ISubscriptionManager> subscriptionManagerMock;
        private Mock<IActiveMqMessageMapper> activeMqMessageMapperMock;
        private Mock<ITopicEvaluator> topicEvaluatiorMock;

        [SetUp]
        public void SetUp()
        {
            this.connectionMock = new Mock<INetTxConnection>();
            this.subscriptionManagerMock = new Mock<ISubscriptionManager>();
            this.activeMqMessageMapperMock = new Mock<IActiveMqMessageMapper>();
            this.topicEvaluatiorMock = new Mock<ITopicEvaluator>();

            this.testee = new ActiveMqMessageSender(
                this.connectionMock.Object, 
                this.subscriptionManagerMock.Object, 
                this.activeMqMessageMapperMock.Object,
                this.topicEvaluatiorMock.Object);
        }

        [Test]
        public void WhenSendingASubscriptionMessage_ThenTheTopicIsSubscribedOnTheSubscriptionManager()
        {
            const string Topic = "SomeTopic";
            const string SubscriptionMessageType = "SomeMessageType";

            var headers = new Dictionary<string, string>();
            headers[UnicastBus.SubscriptionMessageType] = SubscriptionMessageType;
            var message = new TransportMessage
                {
                    MessageIntent = MessageIntentEnum.Subscribe,
                    Headers = headers,
                };
            this.topicEvaluatiorMock.Setup(te => te.GetTopicFromMessageType(SubscriptionMessageType)).Returns(Topic);

            this.testee.Send(message, Address.Local);

            this.subscriptionManagerMock.Verify(sm => sm.Subscribe(Topic));
        }

        [Test]
        public void WhenSendingAnUnsubscriptionMessage_ThenTheTopicIsUnsubscribedOnTheSubscriptionManager()
        {
            const string Topic = "SomeTopic";
            const string SubscriptionMessageType = "SomeMessageType";

            var headers = new Dictionary<string, string>();
            headers[UnicastBus.SubscriptionMessageType] = SubscriptionMessageType;
            var message = new TransportMessage
            {
                MessageIntent = MessageIntentEnum.Unsubscribe,
                Headers = headers,
            };
            this.topicEvaluatiorMock.Setup(te => te.GetTopicFromMessageType(SubscriptionMessageType)).Returns(Topic);

            this.testee.Send(message, Address.Local);

            this.subscriptionManagerMock.Verify(sm => sm.Unsubscribe(Topic));
        }

        [Test]
        public void WhenSendingAPublicationMessage_ThenItIsSentToTheTopic()
        {
            const string Topic = "SomeTopic";
            const string SubscriptionMessageType = "SomeMessageType";

            var sessionMock = this.SetupCreateSession();
            var producerMock = this.SetupCreateProducer(sessionMock);

            var headers = new Dictionary<string, string>();
            headers[Headers.EnclosedMessageTypes] = SubscriptionMessageType;
            var message = new TransportMessage
            {
                MessageIntent = MessageIntentEnum.Publish,
                Headers = headers,
            };
            this.topicEvaluatiorMock.Setup(te => te.GetTopicFromMessageType(SubscriptionMessageType)).Returns(Topic);
            var jmsMessage = this.SetupCreateJmsMessage(message, sessionMock);
            var topic = this.SetupGetTopic(sessionMock, Topic);

            this.testee.Send(message, Address.Local);

            producerMock.Verify(p => p.Send(topic, jmsMessage));
        }

        [Test]
        public void WhenSendingASendMessage_ThenItIsSentToTheDestinationQueue()
        {
            const string Queue = "somequeue";

            var sessionMock = this.SetupCreateSession();
            var producerMock = this.SetupCreateProducer(sessionMock);
            var headers = new Dictionary<string, string>();
            var message = new TransportMessage
            {
                MessageIntent = MessageIntentEnum.Send,
                Headers = headers,
            };
            var jmsMessage = this.SetupCreateJmsMessage(message, sessionMock);
            var queue = this.SetupGetQueue(sessionMock, Queue);

            this.testee.Send(message, new Address(Queue, "SomeMachineName"));

            producerMock.Verify(p => p.Send(queue, jmsMessage));
        }

        private IQueue SetupGetQueue(Mock<INetTxSession> sessionMock, string queue)
        {
            var destination = new Mock<IQueue>().Object;
            sessionMock.Setup(s => s.GetQueue(queue)).Returns(destination);
            return destination;
        }

        private ITopic SetupGetTopic(Mock<INetTxSession> sessionMock, string topic)
        {
            var destination = new Mock<ITopic>().Object;
            sessionMock.Setup(s => s.GetTopic(topic)).Returns(destination);
            return destination;
        }

        private IMessage SetupCreateJmsMessage(TransportMessage message, Mock<INetTxSession> sessionMock)
        {
            var jmsMessage = new Mock<IMessage>().Object;
            this.activeMqMessageMapperMock.Setup(m => m.CreateJmsMessage(message, sessionMock.Object)).Returns(jmsMessage);
            return jmsMessage;
        }

        private Mock<IMessageProducer> SetupCreateProducer(Mock<INetTxSession> sessionMock)
        {
            var producerMock = new Mock<IMessageProducer>();
            sessionMock.Setup(s => s.CreateProducer()).Returns(producerMock.Object);
            return producerMock;
        }

        private Mock<INetTxSession> SetupCreateSession()
        {
            var sessionMock = new Mock<INetTxSession>();
            this.connectionMock.Setup(c => c.CreateNetTxSession()).Returns(sessionMock.Object);
            return sessionMock;
        }
    }
}
