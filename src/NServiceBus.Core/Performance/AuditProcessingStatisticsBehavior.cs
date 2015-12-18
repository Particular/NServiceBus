namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Audit;
    using NServiceBus.Pipeline;

    class AuditProcessingStatisticsBehavior : Behavior<IAuditContext>
    {
        public override Task Invoke(IAuditContext context, Func<Task> next)
        {

            ProcessingStatisticsBehavior.State state;

            if (context.Extensions.TryGet(out state))
            {
                context.AddAuditData(Headers.ProcessingStarted,DateTimeExtensions.ToWireFormattedString(state.ProcessingStarted));
                context.AddAuditData(Headers.ProcessingEnded, DateTimeExtensions.ToWireFormattedString(state.ProcessingEnded));
            }

            return next();
        }
    }
}