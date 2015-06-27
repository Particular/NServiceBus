namespace NServiceBus
{
    using System;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class PublishToOutgoingContextConnector:StageConnector<OutgoingPublishContext,OutgoingContext>
    {
        public override void Invoke(OutgoingPublishContext context, Action<OutgoingContext> next)
        {
            next(new OutgoingContext(context));
        }
    }
}