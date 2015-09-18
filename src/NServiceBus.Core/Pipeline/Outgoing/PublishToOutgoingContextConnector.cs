namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using Pipeline;
    using Pipeline.Contexts;

    class PublishToOutgoingContextConnector : StageConnector<OutgoingPublishContext, OutgoingLogicalMessageContext>
    {
        public override Task Invoke(OutgoingPublishContext context, Func<OutgoingLogicalMessageContext, Task> next)
        {
            return next(new OutgoingLogicalMessageContext(context.Message, context));
        }
    }
}