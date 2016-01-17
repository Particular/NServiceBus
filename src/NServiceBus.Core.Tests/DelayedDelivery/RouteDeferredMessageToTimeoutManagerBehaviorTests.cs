namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;

    class RouteDeferredMessageToTimeoutManagerBehaviorTests
    {
        [Test]
        public async Task Should_reroute_to_tm()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var delay = TimeSpan.FromDays(1);
            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new RoutingContext(message, new UnicastRoutingStrategy("target"), null);

            context.AddDeliveryConstraint(new DelayDeliveryWith(delay));

            await behavior.Invoke(context, () => TaskEx.CompletedTask);

            Assert.AreEqual("tm", ((UnicastAddressTag)context.RoutingStrategies.First().Apply(new Dictionary<string, string>())).Destination);

            Assert.AreEqual(message.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo], "target");
        }


        [Test]
        public void Delayed_delivery_using_the_tm_is_only_supported_for_sends()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var delay = TimeSpan.FromDays(1);

            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new RoutingContext(message, new MulticastRoutingStrategy(null), null);
            context.AddDeliveryConstraint(new DelayDeliveryWith(delay));

            var ex = Assert.Throws<Exception>(async () => await behavior.Invoke(context, () => TaskEx.CompletedTask));

            Assert.True(ex.Message.Contains("unicast routing"));
        }

        [Test]
        public void Delayed_delivery_cant_be_combined_with_ttbr()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var delay = TimeSpan.FromDays(1);

            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new RoutingContext(message, new UnicastRoutingStrategy("target"), null);
            context.AddDeliveryConstraint(new DelayDeliveryWith(delay));
            context.AddDeliveryConstraint(new DiscardIfNotReceivedBefore(TimeSpan.FromSeconds(30)));

            var ex = Assert.Throws<Exception>(async () => await behavior.Invoke(context, () => TaskEx.CompletedTask));

            Assert.True(ex.Message.Contains("TimeToBeReceived"));
        }
        [Test]
        public async Task Should_set_the_expiry_header_to_a_absolute_utc_time_calculated_based_on_delay()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var delay = TimeSpan.FromDays(1);

            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new RoutingContext(message, new UnicastRoutingStrategy("target"), null);
            context.AddDeliveryConstraint(new DelayDeliveryWith(delay));

            await behavior.Invoke(context, () => TaskEx.CompletedTask);

            Assert.LessOrEqual(DateTimeExtensions.ToUtcDateTime(message.Headers[TimeoutManagerHeaders.Expire]), DateTime.UtcNow + delay);
        }

        [Test]
        public async Task Should_set_the_expiry_header_to_a_absolute_utc_time()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var at = DateTime.UtcNow + TimeSpan.FromDays(1);

            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new RoutingContext(message, new UnicastRoutingStrategy("target"), null);
            context.AddDeliveryConstraint(new DoNotDeliverBefore(at));

            await behavior.Invoke(context, () => TaskEx.CompletedTask);

            Assert.AreEqual(message.Headers[TimeoutManagerHeaders.Expire], DateTimeExtensions.ToWireFormattedString(at));
        }
    }
}