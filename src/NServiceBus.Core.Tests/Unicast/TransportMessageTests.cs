namespace NServiceBus.Unicast.Tests
{
    using System.Collections.Generic;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class OutgoingMessageTests
    {
        [Test]
        public void Should_set_the_time_sent_header()
        {
            var message = new OutgoingMessage("id",new Dictionary<string, string>(),null );

            Assert.True(message.Headers.ContainsKey(Headers.TimeSent));
        }

        [Test]
        public void Should_set_the_nsbversion_header()
        {
            var message = new OutgoingMessage("id", new Dictionary<string, string>(), null);

            Assert.True(message.Headers.ContainsKey(Headers.NServiceBusVersion));
        }
    }
}