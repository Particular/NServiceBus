namespace NServiceBus.Unicast.Tests
{
    using Contexts;
    using Monitoring;
    using NUnit.Framework;
    using Rhino.Mocks;
    using UnitOfWork;

    [TestFixture]
    class When_processing_a_message_with_timing_turned_on : using_the_unicastBus
    {
        [Test]
        public void Should_set_the_processing_headers()
        {
            FuncBuilder.Register<IManageUnitsOfWork>(() => new ProcessingStatistics{Bus = bus});

            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();

            ReceiveMessage(receivedMessage);

            Assert.True(bus.CurrentMessageContext.Headers.ContainsKey("NServiceBus.ProcessingStarted"));
            Assert.True(bus.CurrentMessageContext.Headers.ContainsKey("NServiceBus.ProcessingEnded"));
        }
    }

    [TestFixture]
    class When_sending_a_message_with_timing_turned_on : using_the_unicastBus
    {
        [Test]
        public void Should_set_the_time_sent_header()
        {
            RegisterMessageType<CommandMessage>();

            bus.Send(new CommandMessage());
            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers.ContainsKey("NServiceBus.TimeSent")), Arg<SendOptions>.Is.Anything));
        }
    }
}