namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using System.Collections.Generic;
    using Apache.NMS;
    using Apache.NMS.ActiveMQ.Commands;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ;
    using NServiceBus.Transports.ActiveMQ.SessionFactories;

    public class ActiveMqSchedulerManagementCommandsTest
    {
        private ActiveMqSchedulerManagementCommands testee;
        private Mock<ISessionFactory> sessionFactoryMock;

        [SetUp]
        public void SetUp()
        {
            sessionFactoryMock = new Mock<ISessionFactory>();

            testee = new ActiveMqSchedulerManagementCommands
                {
                    SessionFactory = sessionFactoryMock.Object
                };
        }

        [Test]
        public void WhenStopped_SessionIsReleased()
        {
            var session = SetupCreateSession();

            testee.Start();
            testee.Stop();

            sessionFactoryMock.Verify(sf => sf.Release(session.Object));
        }

        [Test]
        public void WhenRequestDeferredMessages_AMessageIsSentToTheAMQSchedulerTopic()
        {
            IMessage sentMessage = null;
            var destination = new ActiveMQTopic("someTopic");
            var session = SetupCreateSession();
            var producer = SetupCreateTopicProducer(session, ScheduledMessage.AMQ_SCHEDULER_MANAGEMENT_DESTINATION);
            producer.Setup(p => p.Send(It.IsAny<IMessage>())).Callback<IMessage>(m => sentMessage = m);

            testee.RequestDeferredMessages(destination);

            sentMessage.Should().NotBeNull();
            sentMessage.NMSReplyTo.Should().Be(destination);
            sentMessage.Properties.Keys.Should().Contain(ScheduledMessage.AMQ_SCHEDULER_ACTION);
            sentMessage.Properties[ScheduledMessage.AMQ_SCHEDULER_ACTION].Should().Be(ScheduledMessage.AMQ_SCHEDULER_ACTION_BROWSE);
        }

        [Test]
        public void WhenProcessingJob_MessagesAreDeleted()
        {
            const string Selector = "Selector";
            const int Id = 42;
            var message = new ActiveMQMessage();
            message.Properties[ScheduledMessage.AMQ_SCHEDULED_ID] = Id;
            var messages = new Queue<IMessage>();
            messages.Enqueue(message);
            IMessage sentDeletionMessage = null;

            var session = SetupCreateSession();
            var consumer = SetupCreateTemporaryTopicConsumer(session, Selector, "");
            consumer.Setup(c => c.ReceiveNoWait()).Returns(() => messages.Count > 0 ? messages.Dequeue() : null);
            var producer = SetupCreateTopicProducer(session, ScheduledMessage.AMQ_SCHEDULER_MANAGEMENT_DESTINATION);
            producer.Setup(p => p.Send(It.IsAny<IMessage>())).Callback<IMessage>(m => sentDeletionMessage = m);

            testee.Start();
            var job = testee.CreateActiveMqSchedulerManagementJob(Selector);
            testee.ProcessJob(job);

            sentDeletionMessage.Should().NotBeNull();
            sentDeletionMessage.Properties[ScheduledMessage.AMQ_SCHEDULER_ACTION].Should().Be(ScheduledMessage.AMQ_SCHEDULER_ACTION_REMOVE);
            sentDeletionMessage.Properties[ScheduledMessage.AMQ_SCHEDULED_ID].Should().Be(Id);
        }


        private Mock<IMessageConsumer> SetupCreateTemporaryTopicConsumer(Mock<ISession> session, string Selector, string topicName)
        {
            var consumer = new Mock<IMessageConsumer>();
            var destination = new ActiveMQTempTopic(topicName);
            session.Setup(s => s.CreateTemporaryTopic()).Returns(destination);
            session.Setup(s => s.CreateConsumer(destination, Selector)).Returns(consumer.Object);
            return consumer;
        }

        private static Mock<Apache.NMS.IMessageProducer> SetupCreateTopicProducer(Mock<ISession> producerSession, string topicName)
        {
            var producer = new Mock<Apache.NMS.IMessageProducer>();
            var topic = new Mock<ITopic>();

            topic.Setup(d => d.TopicName).Returns(topicName);
            producerSession.Setup(s => s.GetTopic(topicName)).Returns(topic.Object);
            producerSession.Setup(s => s.CreateProducer(topic.Object)).Returns(producer.Object);

            return producer;
        }

        private Mock<ISession> SetupCreateSession()
        {
            var session = new Mock<ISession>();
            session.Setup(s => s.CreateMessage()).Returns(() => new ActiveMQMessage());
            sessionFactoryMock.Setup(sf => sf.GetSession()).Returns(session.Object);
            return session;
        }
    }
}