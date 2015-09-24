namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class ReplyToOutgoingContextConnector : StageConnector<OutgoingReplyContext, OutgoingContext>
    {
        public override Task Invoke(OutgoingReplyContext context, Func<OutgoingContext, Task> next)
        {
            return next(new OutgoingContext(context));
        }
    }
}