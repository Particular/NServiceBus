using NServiceBus.Transports;

namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Performance.TimeToBeReceived;
    using Pipeline;
    using Routing;

    class AuditToRoutingConnector : StageConnector<IAuditContext, IRoutingContext>
    {
        public AuditToRoutingConnector(TimeSpan? timeToBeReceived)
        {
            this.timeToBeReceived = timeToBeReceived;
        }

        public override Task Invoke(IAuditContext context, Func<IRoutingContext, Task> stage)
        {
            var message = context.Message;

            if (context.Extensions.TryGet(out State state))
            {
                //transfer audit values to the headers of the message to audit
                foreach (var kvp in state.AuditValues)
                {
                    message.Headers[kvp.Key] = kvp.Value;
                }
            }

            var transportProperties = new TransportProperties();

            if (timeToBeReceived.HasValue)
            {
                transportProperties.DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived.Value);
            }

            var dispatchContext = this.CreateRoutingContext(context.Message, new UnicastRoutingStrategy(context.AuditAddress), context);

            dispatchContext.Extensions.Set(transportProperties);

            return stage(dispatchContext);
        }

        TimeSpan? timeToBeReceived;

        public class State
        {
            public Dictionary<string, string> AuditValues = new Dictionary<string, string>();
        }
    }
}