namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DelayedDelivery.TimeoutManager;
    using DeliveryConstraints;
    using Performance.TimeToBeReceived;
    using Pipeline;
    using Routing;
    using TransportDispatch;

    class RouteDeferredMessageToTimeoutManagerBehavior : Behavior<RoutingContext>
    {
        public RouteDeferredMessageToTimeoutManagerBehavior(string timeoutManagerAddress)
        {
            this.timeoutManagerAddress = timeoutManagerAddress;
        }


        public override Task Invoke(RoutingContext context, Func<Task> next)
        {
            DelayedDeliveryConstraint constraint;

            if (context.TryGetDeliveryConstraint(out constraint))
            {
                if (context.AddressLabels.Any(l => l.GetType() != typeof(DirectAddressLabel)))
                {
                    throw new Exception("Delayed delivery using the timeoutmanager is only supported for messages with unicast routing");
                }
                if (context.AddressLabels.Count > 1)
                {
                    throw new Exception("A deferred message cannot contain more than one destination: " + string.Join(", ",context.AddressLabels.Cast<DirectAddressLabel>().Select(l => l.Destination)));
                }

                DiscardIfNotReceivedBefore discardIfNotReceivedBefore;
                if (context.TryGetDeliveryConstraint(out discardIfNotReceivedBefore))
                {
                    throw new Exception("Postponed delivery of messages with TimeToBeReceived set is not supported. Remove the TimeToBeReceived attribute to postpone messages of this type.");
                }

                context.Message.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo] = context.AddressLabels.Cast<DirectAddressLabel>().First().Destination;
                context.AddressLabels = new[]
                {
                    new DirectAddressLabel(timeoutManagerAddress)
                }; 

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

                context.Message.Headers[TimeoutManagerHeaders.Expire] = DateTimeExtensions.ToWireFormattedString(deliverAt);
                context.RemoveDeliveryConstaint(constraint);
            }

            return next();
        }

        string timeoutManagerAddress;

        public class Registration : RegisterStep
        {
            public Registration()
                : base("RouteDeferredMessageToTimeoutManager", typeof(RouteDeferredMessageToTimeoutManagerBehavior), "Reroutes deferred messages to the timeout manager")
            {
            }
        }
    }
}