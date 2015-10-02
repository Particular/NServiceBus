namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using Pipeline;
    using Pipeline.Contexts;

    class SendToOutgoingContextConnector : StageConnector<OutgoingSendContext, OutgoingLogicalMessageContext>
    {
        public override Task Invoke(OutgoingSendContext context, Func<OutgoingLogicalMessageContext, Task> next)
        {
            return next(new OutgoingLogicalMessageContext(context.Message, context));
        }
    }
}