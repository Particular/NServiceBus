namespace NServiceBus.Core.Tests.Pipeline.Outgoing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using Transport;
    using NUnit.Framework;
    using Testing;
    using System.Threading;
    using DelayedDelivery;
    using Extensibility;

    [TestFixture]
    public class AttachSenderRelatedInfoOnMessageTests
    {
        [Test]
        public async Task Should_set_the_time_sent_headerAsync()
        {
            var message = await InvokeBehaviorAsync();

            Assert.True(message.Headers.ContainsKey(Headers.TimeSent));
        }

        [Test]
        public async Task Should_not_override_the_time_sent_headerAsync()
        {
            var timeSent = DateTime.UtcNow.ToString();

            var message = await InvokeBehaviorAsync(new Dictionary<string, string>
            {
                {Headers.TimeSent, timeSent}
            });

            Assert.True(message.Headers.ContainsKey(Headers.TimeSent));
            Assert.AreEqual(timeSent, message.Headers[Headers.TimeSent]);
        }

        [Test]
        public async Task Should_set_the_nsb_version_headerAsync()
        {
            var message = await InvokeBehaviorAsync();

            Assert.True(message.Headers.ContainsKey(Headers.NServiceBusVersion));
        }

        [Test]
        public async Task Should_not_override_nsb_version_headerAsync()
        {
            var nsbVersion = "some-crazy-version-number";
            var message = await InvokeBehaviorAsync(new Dictionary<string, string>
            {
                 {Headers.NServiceBusVersion, nsbVersion}
            });

            Assert.True(message.Headers.ContainsKey(Headers.NServiceBusVersion));
            Assert.AreEqual(nsbVersion, message.Headers[Headers.NServiceBusVersion]);
        }

        [Test]
        public async Task Should_set_deliver_At_header_when_delay_delivery_with_setAsync()
        {
            var message = await InvokeBehaviorAsync(null, new DispatchProperties
            {
                DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(2))
            });

            Assert.True(message.Headers.ContainsKey(Headers.DeliverAt));
        }

        [Test]
        public async Task Should_set_deliver_at_header_when_do_not_deliver_before_is_setAsync()
        {
            var doNotDeliverBefore = DateTimeOffset.UtcNow;
            var message = await InvokeBehaviorAsync(null, new DispatchProperties
            {
                DoNotDeliverBefore = new DoNotDeliverBefore(doNotDeliverBefore)
            });

            Assert.True(message.Headers.ContainsKey(Headers.DeliverAt));
            Assert.AreEqual(DateTimeOffsetHelper.ToWireFormattedString(doNotDeliverBefore), message.Headers[Headers.DeliverAt]);
        }

        [Test]
        public async Task Should_not_override_deliver_at_headerAsync()
        {
            var doNotDeliverBefore = DateTimeOffset.UtcNow;
            var message = await InvokeBehaviorAsync(new Dictionary<string, string>
            {
                {Headers.DeliverAt, DateTimeOffsetHelper.ToWireFormattedString(doNotDeliverBefore)}
            }, new DispatchProperties
            {
                DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(2))
            });

            Assert.True(message.Headers.ContainsKey(Headers.DeliverAt));
            Assert.AreEqual(DateTimeOffsetHelper.ToWireFormattedString(doNotDeliverBefore), message.Headers[Headers.DeliverAt]);
        }

        static async Task<OutgoingMessage> InvokeBehaviorAsync(Dictionary<string, string> headers = null, DispatchProperties dispatchProperties = null, CancellationToken cancellationToken = default)
        {
            var message = new OutgoingMessage("id", headers ?? new Dictionary<string, string>(), null);
            var stash = new ContextBag();

            if (dispatchProperties != null)
            {
                stash.Set(dispatchProperties);
            }

            await new AttachSenderRelatedInfoOnMessageBehavior()
                .Invoke(new TestableRoutingContext { Message = message, Extensions = stash, RoutingStrategies = new List<UnicastRoutingStrategy> { new UnicastRoutingStrategy("_") }, CancellationToken = cancellationToken }, _ => Task.CompletedTask);

            return message;
        }
    }
}