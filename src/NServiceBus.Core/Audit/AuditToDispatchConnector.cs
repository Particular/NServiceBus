namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Audit;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class AuditToDispatchConnector : StageConnector<AuditContext, DispatchContext>
    {
        TimeSpan? timeToBeReceived;

        public AuditToDispatchConnector(TimeSpan? timeToBeReceived)
        {
            this.timeToBeReceived = timeToBeReceived;
        }

        public override void Invoke(AuditContext context, Action<DispatchContext> next)
        {
            var message = context.Get<OutgoingMessage>();

            State state;

            if (context.TryGet(out state))
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

            var dispatchContext = new DispatchContext(message, context);

            dispatchContext.Set(deliveryConstraints);

            next(dispatchContext);
        }

        public class State
        {
            public Dictionary<string, string> AuditValues = new Dictionary<string, string>();
        }
    }
}