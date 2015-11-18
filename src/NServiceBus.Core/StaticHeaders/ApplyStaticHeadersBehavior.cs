namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.StaticHeaders;

    class ApplyStaticHeadersBehavior:Behavior<OutgoingLogicalMessageContext>
    {
        CurrentStaticHeaders currentStaticHeaders;

        public ApplyStaticHeadersBehavior(CurrentStaticHeaders currentStaticHeaders)
        {
            this.currentStaticHeaders = currentStaticHeaders;
        }

        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            foreach (var staticHeader in currentStaticHeaders)
            {
                context.Headers[staticHeader.Key] = staticHeader.Value;
            }

            return next();
        }
    }
}