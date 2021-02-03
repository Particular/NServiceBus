namespace NServiceBus.Core.Tests.Pipeline.Outgoing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;
    using Transport;

    [TestFixture]
    public class AttachSenderRelatedInfoOnMessageTests
    {
        [Test]
        public async Task Should_set_the_time_sent_header()
        {
            var message = await InvokeBehavior();

            Assert.True(message.Headers.ContainsKey(Headers.TimeSent));
        }

        [Test]
        public async Task Should_not_override_the_time_sent_header()
        {
            var timeSent = DateTime.UtcNow.ToString();

            var message = await InvokeBehavior(new Dictionary<string, string>
            {
                {Headers.TimeSent, timeSent}
            });

            Assert.True(message.Headers.ContainsKey(Headers.TimeSent));
            Assert.AreEqual(timeSent, message.Headers[Headers.TimeSent]);
        }


        [Test]
        public async Task Should_set_the_nsb_version_header()
        {
            var message = await InvokeBehavior();

            Assert.True(message.Headers.ContainsKey(Headers.NServiceBusVersion));
        }

        [Test]
        public async Task Should_not_override_nsb_version_header()
        {
            var nsbVersion = "some-crazy-version-number";
            var message = await InvokeBehavior(new Dictionary<string, string>
            {
                 {Headers.NServiceBusVersion, nsbVersion}
            });

            Assert.True(message.Headers.ContainsKey(Headers.NServiceBusVersion));
            Assert.AreEqual(nsbVersion, message.Headers[Headers.NServiceBusVersion]);
        }

        static async Task<OutgoingMessage> InvokeBehavior(Dictionary<string, string> headers = null)
        {
            var message = new OutgoingMessage("id", headers ?? new Dictionary<string, string>(), null);

            await new AttachSenderRelatedInfoOnMessageBehavior()
                .Invoke(new TestableRoutingContext { Message = message, RoutingStrategies = new List<UnicastRoutingStrategy> { new UnicastRoutingStrategy("_") } }, _ => TaskEx.CompletedTask);

            return message;
        }
    }
}