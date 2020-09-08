namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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

        public Task Invoke(IRoutingContext context, Func<IRoutingContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            if (!IsDeferred(context, out var deliverAt))
            {
                return next(context, cancellationToken);
            }

            if (context.Extensions.TryGetDeliveryConstraint(out DiscardIfNotReceivedBefore _))
            {
                throw new Exception($"Postponed delivery of messages with TimeToBeReceived set is not supported. Remove the TimeToBeReceived attribute to postpone messages of type '{context.Message.Headers[Headers.EnclosedMessageTypes]}'.");
            }

            var newRoutingStrategies = context.RoutingStrategies.Select(s => RerouteToTimeoutManager(s, context, deliverAt));
            context.RoutingStrategies = newRoutingStrategies.ToArray();

            return next(context, cancellationToken);
        }

        RoutingStrategy RerouteToTimeoutManager(RoutingStrategy routingStrategy, IRoutingContext context, DateTime deliverAt)
        {
            var headers = new Dictionary<string, string>(context.Message.Headers);
            var originalTag = routingStrategy.Apply(headers);
            if (!(originalTag is UnicastAddressTag unicastTag))
            {
                throw new Exception("Delayed delivery using the Timeout Manager is only supported for messages with unicast routing");
            }
            return new TimeoutManagerRoutingStrategy(timeoutManagerAddress, unicastTag.Destination, deliverAt);
        }

        static bool IsDeferred(IExtendable context, out DateTime deliverAt)
        {
            deliverAt = DateTime.MinValue;
            if (context.Extensions.TryRemoveDeliveryConstraint(out DoNotDeliverBefore doNotDeliverBefore))
            {
                deliverAt = doNotDeliverBefore.At;
                return true;
            }
            if (context.Extensions.TryRemoveDeliveryConstraint(out DelayDeliveryWith delayDeliveryWith))
            {
                deliverAt = DateTime.UtcNow + delayDeliveryWith.Delay;
                return true;
            }
            return false;
        }

        readonly string timeoutManagerAddress;
    }
}