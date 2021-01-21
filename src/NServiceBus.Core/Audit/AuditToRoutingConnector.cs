namespace NServiceBus
{
    using Transport;
    using System;
    using System.Collections.Generic;
    using System.Threading;
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

        public override Task Invoke(IAuditContext context, Func<IRoutingContext, CancellationToken, Task> stage, CancellationToken token)
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

            var dispatchProperties = new DispatchProperties();

            if (timeToBeReceived.HasValue)
            {
                dispatchProperties.DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived.Value);
            }

            var dispatchContext = this.CreateRoutingContext(context.Message, new UnicastRoutingStrategy(context.AuditAddress), context);

            dispatchContext.Extensions.Set(dispatchProperties);

            return stage(dispatchContext, token);
        }

        TimeSpan? timeToBeReceived;

        public class State
        {
            public Dictionary<string, string> AuditValues = new Dictionary<string, string>();
        }
    }
}