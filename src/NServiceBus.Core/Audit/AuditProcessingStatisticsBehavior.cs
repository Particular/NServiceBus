namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class AuditProcessingStatisticsBehavior : IBehavior<IAuditContext, IAuditContext>
    {
        internal const string ProcessingStartedKey = "NServiceBus.Auditing.ProcessingStarted";

        public Task Invoke(IAuditContext context, Func<IAuditContext, Task> next)
        {
            if (context.Extensions.TryGet(ProcessingStartedKey, out DateTimeOffset processingStarted))
            {
                context.AuditMetadata[Headers.ProcessingStarted] = DateTimeOffsetHelper.ToWireFormattedString(processingStarted);
                context.AuditMetadata[Headers.ProcessingEnded] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
            }

            return next(context);
        }
    }
}