namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using System;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ;

    [TestFixture]
    public class ActiveMQMessageDeferTests
    {
        private ActiveMQMessageDefer testee;

        private Mock<ISendMessages> messageSenderMock;

        [SetUp]
        public void SetUp()
        {
            this.messageSenderMock = new Mock<ISendMessages>();

            this.testee = new ActiveMQMessageDefer { MessageSender = this.messageSenderMock.Object };
        }      

        [Test]
        public void WhenDeferMessage_AMQScheduledDelayShouldBeAdded()
        {
            var address = new Address("SomeQueue", "SomeMachine");
            var time = DateTime.UtcNow + TimeSpan.FromMinutes(1);
            var message = new TransportMessage();

            this.testee.Defer(message, time, address);

            this.messageSenderMock.Verify(s => s.Send(message, address));
            message.Headers.Should().ContainKey(ScheduledMessage.AMQ_SCHEDULED_DELAY);
            Int32.Parse(message.Headers[ScheduledMessage.AMQ_SCHEDULED_DELAY]).Should().BeInRange(59500,60100);
        }

        [Test]
        public void WhenClearDeferredMessages_MessageIsSentToTheActiveMqSchedulerManagement()
        {
            const string headerKey = "theKey";
            const string headerValue = "someValue";
            Address.InitializeLocalAddress("localqueue");

            var expectedSelector = string.Format("{0} = '{1}'", headerKey, headerValue);
            Address sentToAddress = null;
            TransportMessage sentMessage = null;
            this.messageSenderMock
                .Setup(ms => ms.Send(It.IsAny<TransportMessage>(), It.IsAny<Address>()))
                .Callback<TransportMessage, Address>((m, a) => { sentToAddress = a; sentMessage = m; });
 
            this.testee.ClearDeferredMessages(headerKey, headerValue);

            sentToAddress.Queue.Should().Be("localqueue.ActiveMqSchedulerManagement");
            sentMessage.Headers.Should().Contain(ActiveMqSchedulerManagement.ClearScheduledMessagesSelectorHeader, expectedSelector);
        }
    }
}