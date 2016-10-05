namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class AuditProcessingStatisticsBehavior : IBehavior<IAuditContext, IAuditContext>
    {
        public Task Invoke(IAuditContext context, Func<IAuditContext, Task> next)
        {
            ProcessingStatisticsBehavior.State state;

            if (context.Extensions.TryGet(out state))
            {
                context.AddAuditData(Headers.ProcessingStarted, DateTimeExtensions.ToWireFormattedString(state.ProcessingStarted));
                // We can't take the processing time from the state since we don't know it yet.
                context.AddAuditData(Headers.ProcessingEnded, DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow));
            }

            return next(context);
        }
    }
}