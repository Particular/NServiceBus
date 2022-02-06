namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class AuditToRoutingConnector : StageConnector<IAuditContext, IRoutingContext>
    {
        public AuditToRoutingConnector(TimeSpan? timeToBeReceived)
        {
            this.timeToBeReceived = timeToBeReceived;
        }

        public override async Task Invoke(IAuditContext context, Func<IRoutingContext, Task> stage)
        {
            var auditAction = context.AuditAction;

            foreach (var routingContext in auditAction.GetRoutingContexts(context, timeToBeReceived))
            {
                await stage(routingContext).ConfigureAwait(false);
            }
        }

        TimeSpan? timeToBeReceived;
    }
}