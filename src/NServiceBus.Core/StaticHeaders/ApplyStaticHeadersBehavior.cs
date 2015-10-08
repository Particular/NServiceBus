namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Pipeline.Contexts;
    using StaticHeaders;
    using TransportDispatch;

    class ApplyStaticHeadersBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        public ApplyStaticHeadersBehavior(CurrentStaticHeaders currentStaticHeaders)
        {
            this.currentStaticHeaders = currentStaticHeaders;
        }

        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            foreach (var staticHeader in currentStaticHeaders)
            {
                context.SetHeader(staticHeader.Key, staticHeader.Value);
            }

            return next();
        }

        CurrentStaticHeaders currentStaticHeaders;
    }
}