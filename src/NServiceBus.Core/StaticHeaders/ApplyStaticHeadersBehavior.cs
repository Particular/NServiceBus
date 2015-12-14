namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;

    class ApplyStaticHeadersBehavior:Behavior<IOutgoingLogicalMessageContext>
    {
        CurrentStaticHeaders currentStaticHeaders;

        public ApplyStaticHeadersBehavior(CurrentStaticHeaders currentStaticHeaders)
        {
            this.currentStaticHeaders = currentStaticHeaders;
        }

        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            foreach (var staticHeader in currentStaticHeaders)
            {
                context.Headers[staticHeader.Key] = staticHeader.Value;
            }

            return next();
        }
    }
}