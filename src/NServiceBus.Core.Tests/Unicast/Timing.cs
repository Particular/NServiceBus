namespace NServiceBus.Unicast.Tests
{
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;


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