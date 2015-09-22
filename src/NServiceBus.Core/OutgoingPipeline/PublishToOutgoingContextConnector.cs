namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class PublishToOutgoingContextConnector : StageConnector<OutgoingPublishContext,OutgoingContext>
    {
        public override Task Invoke(OutgoingPublishContext context, Func<OutgoingContext, Task> next)
        {
            return next(new OutgoingContext(context));
        }
    }
}