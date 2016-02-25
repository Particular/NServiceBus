namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class OutgoingMessageTests
    {
        [Test]
        public void Should_set_the_time_sent_header()
        {
            var message = new OutgoingMessage("id", new Dictionary<string, string>(), null);

            Assert.True(message.Headers.ContainsKey(Headers.TimeSent));
        }

        [Test]
        public void Should_not_override_the_time_sent_header()
        {
            var timeSent = DateTime.UtcNow.ToString();
            var message = new OutgoingMessage("id", new Dictionary<string, string> { {Headers.TimeSent, timeSent} }, null);

            Assert.True(message.Headers.ContainsKey(Headers.TimeSent));
            Assert.AreEqual(timeSent, message.Headers[Headers.TimeSent]);
        }


        [Test]
        public void Should_set_the_nsb_version_header()
        {
            var message = new OutgoingMessage("id", new Dictionary<string, string>(), null);

            Assert.True(message.Headers.ContainsKey(Headers.NServiceBusVersion));
        }

        [Test]
        public void Should_not_override_nsb_version_header()
        {
            var nsbVersion = "some-crazy-version-number";
            var message = new OutgoingMessage("id", new Dictionary<string, string> { { Headers.NServiceBusVersion, nsbVersion } }, null);

            Assert.True(message.Headers.ContainsKey(Headers.NServiceBusVersion));
            Assert.AreEqual(nsbVersion, message.Headers[Headers.NServiceBusVersion]);
        }
    }
}