namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class SendToOutgoingContextConnector:StageConnector<OutgoingSendContext,OutgoingContext>
    {
        public override Task Invoke(OutgoingSendContext context, Func<OutgoingContext, Task> next)
        {
            return next(new OutgoingContext(context));
        }
    }
}