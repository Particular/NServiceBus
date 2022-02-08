namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class AuditToRoutingConnector : StageConnector<IAuditContext, IRoutingContext>
    {
        public override async Task Invoke(IAuditContext context, Func<IRoutingContext, Task> stage)
        {
            var auditAction = context.AuditAction;
            var auditActionContext = context.PreventChanges();

            foreach (var routingContext in auditAction.GetRoutingContexts(auditActionContext))
            {
                await stage(routingContext).ConfigureAwait(false);
            }
        }
    }
}