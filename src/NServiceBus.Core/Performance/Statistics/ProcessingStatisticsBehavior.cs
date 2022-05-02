namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class ProcessingStatisticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            context.Extensions.Set(AuditProcessingStatisticsBehavior.ProcessingStartedKey, DateTimeOffset.UtcNow);
            await next(context).ConfigureAwait(false);
        }
    }
}
