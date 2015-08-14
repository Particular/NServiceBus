namespace NServiceBus.Core.Tests.Timeout
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DelayedDelivery.TimeoutManager;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;
    using NUnit.Framework;

    class RouteDeferredMessageToTimeoutManagerBehaviorTests
    {
        [Test]
        public void Should_reroute_to_tm()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var delay = TimeSpan.FromDays(1);
            var message = new OutgoingMessage("id",new Dictionary<string, string>(),new byte[0]);

            var context = new DispatchContext(message,null);

            context.AddDeliveryConstraint(new DelayDeliveryWith(delay));
            context.Set<RoutingStrategy>(new DirectToTargetDestination("target"));

            behavior.Invoke(context, () => { });

            Assert.AreEqual("tm",((DirectToTargetDestination)context.GetRoutingStrategy()).Destination);

            Assert.AreEqual(message.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo],"target");
        }


        [Test]
        public void Delayed_delivery_using_the_tm_is_only_supported_for_sends()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var delay = TimeSpan.FromDays(1);

            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new DispatchContext(message, null);
            context.AddDeliveryConstraint(new DelayDeliveryWith(delay));
            context.Set<RoutingStrategy>(new ToAllSubscribers(null));

            var ex = Assert.Throws<Exception>(()=> behavior.Invoke(context, () => { }));

            Assert.True(ex.Message.Contains("Direct routing"));
        }

        [Test]
        public void Delayed_delivery_cant_be_combined_with_ttbr()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var delay = TimeSpan.FromDays(1);

            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new DispatchContext(message, null);
            context.AddDeliveryConstraint(new DelayDeliveryWith(delay));
            context.AddDeliveryConstraint(new DiscardIfNotReceivedBefore(TimeSpan.FromSeconds(30)));
            context.Set<RoutingStrategy>(new DirectToTargetDestination("target"));

            var ex = Assert.Throws<Exception>(() => behavior.Invoke(context, () => { }));

            Assert.True(ex.Message.Contains("TimeToBeReceived"));
        }
        [Test]
        public void Should_set_the_expiry_header_to_a_absolute_utc_time_calculated_based_on_delay()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var delay = TimeSpan.FromDays(1);

            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new DispatchContext(message, null);
            context.AddDeliveryConstraint(new DelayDeliveryWith(delay));
            context.Set<RoutingStrategy>(new DirectToTargetDestination("target"));
            
            behavior.Invoke(context,()=>{});

            Assert.LessOrEqual(DateTimeExtensions.ToUtcDateTime(message.Headers[TimeoutManagerHeaders.Expire]), DateTime.UtcNow + delay);
          }

        [Test]
        public void Should_set_the_expiry_header_to_a_absolute_utc_time()
        {
            var behavior = new RouteDeferredMessageToTimeoutManagerBehavior("tm");
            var at = DateTime.UtcNow + TimeSpan.FromDays(1);

            var message = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[0]);

            var context = new DispatchContext(message, null);
            context.AddDeliveryConstraint(new DoNotDeliverBefore(at));
            context.Set<RoutingStrategy>(new DirectToTargetDestination("target"));

            behavior.Invoke(context, () => { });

            Assert.AreEqual(message.Headers[TimeoutManagerHeaders.Expire], DateTimeExtensions.ToWireFormattedString(at));
        }
    }
}