namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Extensibility;
    using Performance.TimeToBeReceived;
    using Pipeline;
    using Routing;
    using Transport;

    class RouteDeferredMessageToTimeoutManagerBehavior : IBehavior<IRoutingContext, IRoutingContext>
    {
        public RouteDeferredMessageToTimeoutManagerBehavior(string timeoutManagerAddress)
        {
            this.timeoutManagerAddress = timeoutManagerAddress;
        }

        public Task Invoke(IRoutingContext context, Func<IRoutingContext, Task> next)
        {
            if (!IsDeferred(context, out var deliverAt))
            {
                return next(context);
            }

            var dispatchProperties = context.Extensions.Get<DispatchProperties>(); // TODO: Unsure this is available here, hooray to the locator pattern :-(
            
            if (dispatchProperties.DiscardIfNotReceivedBefore != null)
            {
                throw new Exception($"Postponed delivery of messages with TimeToBeReceived set is not supported. Remove the TimeToBeReceived attribute to postpone messages of type '{context.Message.Headers[Headers.EnclosedMessageTypes]}'.");
            }

            var newRoutingStrategies = context.RoutingStrategies.Select(s => RerouteToTimeoutManager(s, context, deliverAt));
            context.RoutingStrategies = newRoutingStrategies.ToArray();

            return next(context);
        }

        RoutingStrategy RerouteToTimeoutManager(RoutingStrategy routingStrategy, IRoutingContext context, DateTimeOffset deliverAt)
        {
            var headers = new Dictionary<string, string>(context.Message.Headers);
            var originalTag = routingStrategy.Apply(headers);
            if (!(originalTag is UnicastAddressTag unicastTag))
            {
                throw new Exception("Delayed delivery using the Timeout Manager is only supported for messages with unicast routing");
            }
            return new TimeoutManagerRoutingStrategy(timeoutManagerAddress, unicastTag.Destination, deliverAt);
        }

        static bool IsDeferred(IExtendable context, out DateTimeOffset deliverAt)
        {
            deliverAt = DateTimeOffset.MinValue;

            var dispatchProperties = context.Extensions.Get<DispatchProperties>();

            if (dispatchProperties.DoNotDeliverBefore != null)
            {
                deliverAt = dispatchProperties.DoNotDeliverBefore.At;
                dispatchProperties.Remove(DispatchProperties.DoNotDeliverBeforeKeyName);
                return true;
            }
            if (dispatchProperties.DelayDeliveryWith != null)
            {
                deliverAt = DateTimeOffset.UtcNow + dispatchProperties.DelayDeliveryWith.Delay;
                dispatchProperties.Remove(DispatchProperties.DelayDeliveryWithKeyName);
                return true;
            }
            return false;
        }

        readonly string timeoutManagerAddress;
    }
}
