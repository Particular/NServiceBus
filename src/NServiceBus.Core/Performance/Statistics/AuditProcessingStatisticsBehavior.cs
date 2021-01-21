namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class AuditProcessingStatisticsBehavior : IBehavior<IAuditContext, IAuditContext>
    {
        public Task Invoke(IAuditContext context, Func<IAuditContext, CancellationToken, Task> next, CancellationToken token)
        {
            if (context.Extensions.TryGet(out ProcessingStatisticsBehavior.State state))
            {
                context.AddAuditData(Headers.ProcessingStarted, DateTimeOffsetHelper.ToWireFormattedString(state.ProcessingStarted));
                // We can't take the processing time from the state since we don't know it yet.
                context.AddAuditData(Headers.ProcessingEnded, DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow));
            }

            return next(context, token);
        }
    }
}