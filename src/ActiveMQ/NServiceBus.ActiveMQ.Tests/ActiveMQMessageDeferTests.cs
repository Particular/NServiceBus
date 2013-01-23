namespace NServiceBus.ActiveMQ
{
    using System;

    using FluentAssertions;

    using Moq;

    using NServiceBus.Transport.ActiveMQ;
    using NServiceBus.Unicast.Queuing;

    using NUnit.Framework;

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
    }
}