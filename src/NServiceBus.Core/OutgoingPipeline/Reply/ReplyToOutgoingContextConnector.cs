namespace NServiceBus
{
    using System;
    using NServiceBus.OutgoingPipeline.Reply;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class ReplyToOutgoingContextConnector : StageConnector<OutgoingReplyContext, OutgoingContext>
    {
        public override void Invoke(OutgoingReplyContext context, Action<OutgoingContext> next)
        {
            next(new OutgoingContext(context));
        }
    }
}