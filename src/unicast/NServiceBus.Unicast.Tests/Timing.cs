namespace NServiceBus.Unicast.Tests
{
    using Contexts;
    using NUnit.Framework;
    using Timing;
    using UnitOfWork;

    [TestFixture]
    public class When_processing_a_message_with_timing_turned_on : using_the_unicastbus
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
}