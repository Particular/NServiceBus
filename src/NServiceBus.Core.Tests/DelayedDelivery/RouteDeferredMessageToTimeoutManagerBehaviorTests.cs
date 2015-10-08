namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DelayedDelivery.TimeoutManager;
    using DeliveryConstraints;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using TransportDispatch;
    using Transports;

    class RouteDeferredMessageToTimeoutManagerBehaviorTests
    {
        [Test]
        public async Task Should_reroute_to_tm()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var delay = TimeSpan.FromDays(1);
            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new RoutingContext(message, new DirectToTargetDestination("target"), null);

            context.AddDeliveryConstraint(new DelayDeliveryWith(delay));

            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.AreEqual("tm", ((DirectToTargetDestination) context.RoutingStrategy).Destination);

            Assert.AreEqual(message.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo], "target");
        }


        [Test]
        public void Delayed_delivery_using_the_tm_is_only_supported_for_sends()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var delay = TimeSpan.FromDays(1);

            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new RoutingContext(message, new ToAllSubscribers(null), null);
            context.AddDeliveryConstraint(new DelayDeliveryWith(delay));

            var ex = Assert.Throws<Exception>(async () => await behavior.Invoke(context, () => Task.FromResult(0)));

            Assert.True(ex.Message.Contains("Direct routing"));
        }

        [Test]
        public void Delayed_delivery_cant_be_combined_with_ttbr()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var delay = TimeSpan.FromDays(1);

            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new RoutingContext(message, new DirectToTargetDestination("target"), null);
            context.AddDeliveryConstraint(new DelayDeliveryWith(delay));
            context.AddDeliveryConstraint(new DiscardIfNotReceivedBefore(TimeSpan.FromSeconds(30)));

            var ex = Assert.Throws<Exception>(async () => await behavior.Invoke(context, () => Task.FromResult(0)));

            Assert.True(ex.Message.Contains("TimeToBeReceived"));
        }

        [Test]
        public async Task Should_set_the_expiry_header_to_a_absolute_utc_time_calculated_based_on_delay()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var delay = TimeSpan.FromDays(1);

            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new RoutingContext(message, new DirectToTargetDestination("target"), null);
            context.AddDeliveryConstraint(new DelayDeliveryWith(delay));

            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.LessOrEqual(DateTimeExtensions.ToUtcDateTime(message.Headers[TimeoutManagerHeaders.Expire]), DateTime.UtcNow + delay);
        }

        [Test]
        public async Task Should_set_the_expiry_header_to_a_absolute_utc_time()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var at = DateTime.UtcNow + TimeSpan.FromDays(1);

            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new RoutingContext(message, new DirectToTargetDestination("target"), null);
            context.AddDeliveryConstraint(new DoNotDeliverBefore(at));

            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.AreEqual(message.Headers[TimeoutManagerHeaders.Expire], DateTimeExtensions.ToWireFormattedString(at));
        }
    }
}