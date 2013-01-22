namespace NServiceBus.Transport.ActiveMQ
{
    using System;

    using Apache.NMS;

    using FluentAssertions;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class MessageProducerTests
    {
        private Mock<ISessionFactory> sessionFactoryMock;
        private Mock<IActiveMqMessageMapper> activeMqMessageMapperMock;
        private Mock<IDestinationEvaluator> destinationEvaluatorMock;
        private MessageProducer testee;

        [SetUp]
        public void SetUp()
        {
            this.sessionFactoryMock = new Mock<ISessionFactory>(MockBehavior.Loose);
            this.activeMqMessageMapperMock = new Mock<IActiveMqMessageMapper>();
            this.destinationEvaluatorMock = new Mock<IDestinationEvaluator>();

            this.testee = new MessageProducer(
                this.sessionFactoryMock.Object, 
                this.activeMqMessageMapperMock.Object,
                this.destinationEvaluatorMock.Object);
        }

        [Test]
        public void WhenSendingASendMessage_OnExcpetion_TheSessionIsReleasedAfterwards()
        {
            const string Reason = "TheExcpetionReason";
            var sessionMock = this.SetupCreateSession();
            this.activeMqMessageMapperMock.Setup(mm => mm
                                                           .CreateJmsMessage(It.IsAny<TransportMessage>(), sessionMock.Object))
                .Throws(new Exception(Reason));

            Action action = () => this.testee.SendMessage(new TransportMessage(), string.Empty, string.Empty);

            action.ShouldThrow<Exception>(Reason);
            this.sessionFactoryMock.Verify(sf => sf.Release(sessionMock.Object));
        }

        [Test]
        public void WhenSendingASendMessage_TheSessionIsReleasedAfterwards()
        {
            var sessionMock = this.SetupCreateSession();

            this.testee.SendMessage(new TransportMessage(), string.Empty, string.Empty);

            this.sessionFactoryMock.Verify(sf => sf.Release(sessionMock.Object));
        }

        [Test]
        public void WhenSendingASendMessage_()
        {
            const string Destination = "TheDestination";
            const string DestinationPrefix = "TheDestinationPrefix";

            var message = new TransportMessage();
            var sessionMock = this.SetupCreateSession();
            var producerMock = this.SetupCreateProducer(sessionMock);
            var jmsMessage = this.SetupCreateJmsMessageFromTransportMessage(message, sessionMock.Object);
            var destination = this.SetupGetDestination(sessionMock, Destination, DestinationPrefix);

            this.testee.SendMessage(message, Destination, DestinationPrefix);

            producerMock.Verify(p => p.Send(destination, jmsMessage));
        }

        private IDestination SetupGetDestination(Mock<ISession> sessionMock, string Destination, string DestinationPrefix)
        {
            var destination = new Mock<IDestination>().Object;
            this.destinationEvaluatorMock.Setup(de => de.GetDestination(sessionMock.Object, Destination, DestinationPrefix))
                .Returns(destination);
            return destination;
        }

        private Mock<ISession> SetupCreateSession()
        {
            var sessionMock = new Mock<ISession>();
            this.sessionFactoryMock.Setup(c => c.GetSession()).Returns(sessionMock.Object);
            this.SetupCreateProducer(sessionMock);
            return sessionMock;
        }

        private IMessage SetupCreateJmsMessageFromTransportMessage(TransportMessage message, ISession session)
        {
            var jmsMessage = new Mock<IMessage>().Object;
            this.activeMqMessageMapperMock.Setup(m => m.CreateJmsMessage(message, session)).Returns(jmsMessage);
            return jmsMessage;
        }

        private Mock<Apache.NMS.IMessageProducer> SetupCreateProducer(Mock<ISession> sessionMock)
        {
            var producerMock = new Mock<Apache.NMS.IMessageProducer>();
            sessionMock.Setup(s => s.CreateProducer()).Returns(producerMock.Object);
            return producerMock;
        }
    }
}