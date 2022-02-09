namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class AuditProcessingStatisticsBehavior : IBehavior<IAuditContext, IAuditContext>
    {
        public Task Invoke(IAuditContext context, Func<IAuditContext, Task> next)
        {
            if (context.Extensions.TryGet(out ProcessingStatisticsBehavior.State state))
            {
                context.AuditMetadata[Headers.ProcessingStarted] = DateTimeOffsetHelper.ToWireFormattedString(state.ProcessingStarted);
                // We can't take the processing time from the state since we don't know it yet.
                context.AuditMetadata[Headers.ProcessingEnded] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
            }

            return next(context);
        }
    }
}