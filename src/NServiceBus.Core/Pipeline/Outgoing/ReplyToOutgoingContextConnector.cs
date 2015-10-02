namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using Pipeline;
    using Pipeline.Contexts;

    class ReplyToOutgoingContextConnector : StageConnector<OutgoingReplyContext, OutgoingLogicalMessageContext>
    {
        public override Task Invoke(OutgoingReplyContext context, Func<OutgoingLogicalMessageContext, Task> next)
        {
            return next(new OutgoingLogicalMessageContext(context.Message, context));
        }
    }
}