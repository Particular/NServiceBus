namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Audit;
    using DeliveryConstraints;
    using Routing;
    using Performance.TimeToBeReceived;
    using Pipeline;
    using TransportDispatch;

    class AuditToDispatchConnector : StageConnector<IAuditContext, IRoutingContext>
    {
        TimeSpan? timeToBeReceived;

        public AuditToDispatchConnector(TimeSpan? timeToBeReceived)
        {
            this.timeToBeReceived = timeToBeReceived;
        }

        public override Task Invoke(IAuditContext context, Func<IRoutingContext, Task> next)
        {
            var message = context.Message;

            State state;

            if (context.Extensions.TryGet(out state))
            {
                //transfer audit values to the headers of the messag to audit
                foreach (var kvp in state.AuditValues)
                {
                    message.Headers[kvp.Key] = kvp.Value;
                }
            }

            var deliveryConstraints = new List<DeliveryConstraint>();

            if (timeToBeReceived.HasValue)
            {
                deliveryConstraints.Add(new DiscardIfNotReceivedBefore(timeToBeReceived.Value));
            }

            var dispatchContext = new RoutingContext(message, new UnicastRoutingStrategy(context.AuditAddress), context);

            dispatchContext.Set(deliveryConstraints);

            return next(dispatchContext);
        }

        public class State
        {
            public Dictionary<string, string> AuditValues = new Dictionary<string, string>();
        }
    }
}