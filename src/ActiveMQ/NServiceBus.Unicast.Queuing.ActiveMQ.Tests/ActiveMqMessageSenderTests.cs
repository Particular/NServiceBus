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
        private Mock<IDestinationEvaluator> destinationEvaluatorMock;

        [SetUp]
        public void SetUp()
        {
            this.connectionMock = new Mock<INetTxConnection>();
            this.subscriptionManagerMock = new Mock<ISubscriptionManager>();
            this.activeMqMessageMapperMock = new Mock<IActiveMqMessageMapper>();
            this.topicEvaluatiorMock = new Mock<ITopicEvaluator>();
            this.destinationEvaluatorMock = new Mock<IDestinationEvaluator>();

            this.testee = new ActiveMqMessageSender(
                this.connectionMock.Object, 
                this.subscriptionManagerMock.Object, 
                this.activeMqMessageMapperMock.Object,
                this.topicEvaluatiorMock.Object,
                this.destinationEvaluatorMock.Object);
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
            this.SetupGetTopicFromMessageType(SubscriptionMessageType, Topic);

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
            this.SetupGetTopicFromMessageType(SubscriptionMessageType, Topic);

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

            var message = CreateTransportMessage(MessageIntentEnum.Publish);
            message.Headers[Headers.EnclosedMessageTypes] = SubscriptionMessageType;

            this.SetupGetTopicFromMessageType(SubscriptionMessageType, Topic);
            var jmsMessage = this.SetupCreateJmsMessageFromTransportMessage(message, sessionMock.Object);
            var topic = this.SetupGetTopic(sessionMock, Topic);

            this.testee.Send(message, Address.Local);

            producerMock.Verify(p => p.Send(topic, jmsMessage));
        }

        [Test]
        public void WhenSendingASendMessage_ThenItIsSentToTheDestinationQueue()
        {
            const string Queue = "QueueName";

            var sessionMock = this.SetupCreateSession();
            var producerMock = this.SetupCreateProducer(sessionMock);
            var message = CreateTransportMessage(MessageIntentEnum.Send);
            var jmsMessage = this.SetupCreateJmsMessageFromTransportMessage(message, sessionMock.Object);
            var queue = this.SetupGetQueue(sessionMock, Queue);

            this.testee.Send(message, new Address(Queue, "SomeMachineName", true));

            producerMock.Verify(p => p.Send(queue, jmsMessage));
        }

        private static TransportMessage CreateTransportMessage(MessageIntentEnum messageIntent)
        {
            var headers = new Dictionary<string, string>();
            return new TransportMessage { MessageIntent = messageIntent, Headers = headers, };
        }

        private void SetupGetTopicFromMessageType(string SubscriptionMessageType, string Topic)
        {
            this.topicEvaluatiorMock.Setup(te => te.GetTopicFromMessageType(SubscriptionMessageType)).Returns(Topic);
        }

        private IQueue SetupGetQueue(Mock<INetTxSession> sessionMock, string queue)
        {
            var destination = new Mock<IQueue>().Object;
            this.destinationEvaluatorMock.Setup(d => d.GetDestination(sessionMock.Object, queue, "queue://")).Returns(destination);
            return destination;
        }

        private ITopic SetupGetTopic(Mock<INetTxSession> sessionMock, string topic)
        {
            var destination = new Mock<ITopic>().Object;
            this.destinationEvaluatorMock.Setup(d => d.GetDestination(sessionMock.Object, topic, "topic://")).Returns(destination);
            return destination;
        }

        private IMessage SetupCreateJmsMessageFromTransportMessage(TransportMessage message, INetTxSession session)
        {
            var jmsMessage = new Mock<IMessage>().Object;
            this.activeMqMessageMapperMock.Setup(m => m.CreateJmsMessage(message, session)).Returns(jmsMessage);
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
