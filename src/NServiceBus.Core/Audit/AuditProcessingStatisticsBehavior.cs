namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Performance.Statistics;
    using Pipeline;

    class AuditProcessingStatisticsBehavior : IBehavior<IAuditContext, IAuditContext>
    {
        public Task Invoke(IAuditContext context, Func<IAuditContext, Task> next)
        {
            var processingStarted = context.GetPipelineStartTime();
            if (processingStarted.HasValue)
            {
                context.AuditMetadata[Headers.ProcessingStarted] = DateTimeOffsetHelper.ToWireFormattedString(processingStarted.Value);
                context.AuditMetadata[Headers.ProcessingEnded] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
            }

            return next(context);
        }
    }
}