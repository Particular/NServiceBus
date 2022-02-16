namespace NServiceBus.Core.Tests.Pipeline.Outgoing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
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

        [Test]
        public async Task Should_set_deliver_At_header_when_delay_delivery_with_setAsync()
        {
            var options = new SendOptions();
            var delayTime = TimeSpan.FromSeconds(2);
            options.DelayDeliveryWith(delayTime);
            var message = await InvokeBehavior(null, options);
            var expectedTime = DateTimeExtensions.ToUtcDateTime(message.Headers[Headers.TimeSent]).Add(delayTime);

            Assert.True(message.Headers.ContainsKey(Headers.DeliverAt));
            Assert.AreEqual(DateTimeExtensions.ToWireFormattedString(expectedTime), message.Headers[Headers.DeliverAt]);
        }

        [Test]
        public async Task Should_set_deliver_At_header_when_do_not_deliver_before_setAsync()
        {
            var options = new SendOptions();
            var doNotDeliverBefore = DateTimeOffset.UtcNow;
            options.DoNotDeliverBefore(doNotDeliverBefore);
            var message = await InvokeBehavior(null, options);

            Assert.True(message.Headers.ContainsKey(Headers.DeliverAt));
            Assert.AreEqual(DateTimeExtensions.ToWireFormattedString(doNotDeliverBefore.UtcDateTime), message.Headers[Headers.DeliverAt]);
        }

        [Test]
        public async Task Should_not_override_deliver_at_headerAsync()
        {
            var options = new SendOptions();
            var doNotDeliverBefore = DateTimeOffset.UtcNow;
            options.DelayDeliveryWith(TimeSpan.FromSeconds(2));
            var message = await InvokeBehavior(new Dictionary<string, string>
            {
                {Headers.DeliverAt, DateTimeExtensions.ToWireFormattedString(doNotDeliverBefore.UtcDateTime)}
            }, options);

            Assert.True(message.Headers.ContainsKey(Headers.DeliverAt));
            Assert.AreEqual(DateTimeExtensions.ToWireFormattedString(doNotDeliverBefore.UtcDateTime), message.Headers[Headers.DeliverAt]);
        }

        static async Task<OutgoingMessage> InvokeBehavior(Dictionary<string, string> headers = null, SendOptions options = null)
        {
            var message = new OutgoingMessage("id", headers ?? new Dictionary<string, string>(), null);
            var stash = new ContextBag();

            if (options != null)
            {
                stash.Set(options);
            }

            await new AttachSenderRelatedInfoOnMessageBehavior()
                .Invoke(new TestableRoutingContext { Message = message, Extensions = stash, RoutingStrategies = new List<UnicastRoutingStrategy> { new UnicastRoutingStrategy("_") } }, _ => TaskEx.CompletedTask);

            return message;
        }
    }
}