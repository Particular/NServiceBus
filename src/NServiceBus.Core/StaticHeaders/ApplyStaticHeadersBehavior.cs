namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.StaticHeaders;
    using NServiceBus.TransportDispatch;

    class ApplyStaticHeadersBehavior:Behavior<OutgoingContext>
    {
        CurrentStaticHeaders currentStaticHeaders;

        public ApplyStaticHeadersBehavior(CurrentStaticHeaders currentStaticHeaders)
        {
            this.currentStaticHeaders = currentStaticHeaders;
        }

        public override void Invoke(OutgoingContext context, Action next)
        {
            foreach (var staticHeader in currentStaticHeaders)
            {
                context.SetHeader(staticHeader.Key,staticHeader.Value);
            }

            next();
        }
    }
}