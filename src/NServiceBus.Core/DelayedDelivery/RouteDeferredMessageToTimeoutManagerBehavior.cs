namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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
                if (context.RoutingStrategies.Any(l => l.GetType() != typeof(UnicastRoutingStrategy)))
                {
                    throw new Exception("Delayed delivery using the timeoutmanager is only supported for messages with unicast routing");
                }
                if (context.RoutingStrategies.Count > 1)
                {
                    var destinations = string.Join(", ",context.RoutingStrategies.Select(s => s.Apply(new Dictionary<string, string>())).Cast<UnicastAddressTag>().Select(l => l.Destination));
                    throw new Exception($"A deferred message cannot contain more than one destination: {destinations}. This is the case when publishing an event to multiple subscriber endpoints or sending a command with overriden distribution strategy.");
                }

                DiscardIfNotReceivedBefore discardIfNotReceivedBefore;
                if (context.TryGetDeliveryConstraint(out discardIfNotReceivedBefore))
                {
                    throw new Exception("Postponed delivery of messages with TimeToBeReceived set is not supported. Remove the TimeToBeReceived attribute to postpone messages of this type.");
                }

                var newRoutingStrategies = context.RoutingStrategies.Cast<UnicastRoutingStrategy>().Select(s => s.SendVia(timeoutManagerAddress)).ToArray();
                context.RoutingStrategies = newRoutingStrategies;

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