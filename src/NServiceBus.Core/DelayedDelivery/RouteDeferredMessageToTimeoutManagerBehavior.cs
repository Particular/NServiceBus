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

    class RouteDeferredMessageToTimeoutManagerBehavior : IBehavior<IRoutingContext, IRoutingContext>
    {
        public RouteDeferredMessageToTimeoutManagerBehavior(string timeoutManagerAddress)
        {
            this.timeoutManagerAddress = timeoutManagerAddress;
        }

        public Task Invoke(IRoutingContext context, Func<IRoutingContext, Task> next)
        {
            DateTime deliverAt;
            if (!IsDeferred(context, out deliverAt))
            {
                return next(context);
            }

            DiscardIfNotReceivedBefore discardIfNotReceivedBefore;
            if (context.Extensions.TryGetDeliveryConstraint(out discardIfNotReceivedBefore))
            {
                throw new Exception("Postponed delivery of messages with TimeToBeReceived set is not supported. Remove the TimeToBeReceived attribute to postpone messages of this type.");
            }

            var newRoutingStrategies = context.RoutingStrategies.Select(s => RerouteToTimeoutManager(s, context, deliverAt));
            context.RoutingStrategies = newRoutingStrategies.ToArray();

            return next(context);
        }

        RoutingStrategy RerouteToTimeoutManager(RoutingStrategy routingStrategy, IRoutingContext context, DateTime deliverAt)
        {
            var headers = new Dictionary<string, string>(context.Message.Headers);
            var originalTag = routingStrategy.Apply(headers);
            var unicastTag = originalTag as UnicastAddressTag;
            if (unicastTag == null)
            {
                throw new Exception("Delayed delivery using the Timeout Manager is only supported for messages with unicast routing");
            }
            return new TimeoutManagerRoutingStrategy(timeoutManagerAddress, unicastTag.Destination, deliverAt);
        }

        static bool IsDeferred(IExtendable context, out DateTime deliverAt)
        {
            deliverAt = DateTime.MinValue;
            DoNotDeliverBefore doNotDeliverBefore;
            DelayDeliveryWith delayDeliveryWith;
            if (context.Extensions.TryRemoveDeliveryConstraint(out doNotDeliverBefore))
            {
                deliverAt = doNotDeliverBefore.At;
                return true;
            }
            if (context.Extensions.TryRemoveDeliveryConstraint(out delayDeliveryWith))
            {
                deliverAt = DateTime.UtcNow + delayDeliveryWith.Delay;
                return true;
            }
            return false;
        }

        string timeoutManagerAddress;
    }
}