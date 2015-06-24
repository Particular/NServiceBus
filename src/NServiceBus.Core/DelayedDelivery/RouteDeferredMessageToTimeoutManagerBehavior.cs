namespace NServiceBus
{
    using System;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Timeout;
    using NServiceBus.TransportDispatch;

    class RouteDeferredMessageToTimeoutManagerBehavior : Behavior<DispatchContext>
    {
        public RouteDeferredMessageToTimeoutManagerBehavior(string timeoutManagerAddress)
        {
            this.timeoutManagerAddress = timeoutManagerAddress;
        }


        public override void Invoke(DispatchContext context, Action next)
        {
            DelayedDeliveryConstraint constraint;

            if (context.TryGetDeliveryConstraint(out constraint))
            {
                var currentRoutingStrategy = context.GetRoutingStrategy() as DirectToTargetDestination;

                if (currentRoutingStrategy == null)
                {
                    throw new Exception("Delayed delivery using the timeoutmanager is only supported for messages with Direct routing");
                }

                DiscardIfNotReceivedBefore discardIfNotReceivedBefore;
                if (context.TryGetDeliveryConstraint(out discardIfNotReceivedBefore))
                {
                    throw new Exception("Postponed delivery of messages with TimeToBeReceived set is not supported. Remove the TimeToBeReceived attribute to postpone messages of this type.");
                }


                context.Set<RoutingStrategy>(new DirectToTargetDestination(timeoutManagerAddress));
                context.SetHeader(TimeoutManagerHeaders.RouteExpiredTimeoutTo, currentRoutingStrategy.Destination);

                DateTime deliverAt;
                var delayConstraint = constraint as DelayDeliveryWith;

                if (delayConstraint != null)
                {
                    deliverAt = DateTime.UtcNow + delayConstraint.Delay;
                }
                else
                {
                    deliverAt = ((DoNotDeliverBefore)constraint).At;
                }

                context.SetHeader(TimeoutManagerHeaders.Expire, DateTimeExtensions.ToWireFormattedString(deliverAt));
                context.RemoveDeliveryConstaint(constraint);
            }

            next();
        }

        readonly string timeoutManagerAddress;


        public class Registration : RegisterStep
        {
            public Registration()
                : base("RouteDeferredMessageToTimeoutManager", typeof(RouteDeferredMessageToTimeoutManagerBehavior), "Reroutes deferred messages to the timeout manager")
            {
            }
        }
    }
}