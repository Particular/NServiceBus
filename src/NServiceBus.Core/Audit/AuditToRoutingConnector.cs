namespace NServiceBus
{
    using Transport;
    using System;
    using System.Threading.Tasks;
    using Performance.TimeToBeReceived;
    using Pipeline;

    class AuditToRoutingConnector : StageConnector<IAuditContext, IRoutingContext>
    {
        public AuditToRoutingConnector(TimeSpan? timeToBeReceived)
        {
            this.timeToBeReceived = timeToBeReceived;
        }

        public override async Task Invoke(IAuditContext context, Func<IRoutingContext, Task> stage)
        {
            var dispatchProperties = new DispatchProperties();

            if (timeToBeReceived.HasValue)
            {
                dispatchProperties.DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived.Value);
            }

            var auditAction = context.AuditAction;

            foreach (var routingData in auditAction.GetRoutingData(context))
            {
                var routingContext = new RoutingContext(routingData.Item1, routingData.Item2, context);

                routingContext.Extensions.Set(dispatchProperties);

                await stage(routingContext).ConfigureAwait(false);
            }
        }

        TimeSpan? timeToBeReceived;
    }
}