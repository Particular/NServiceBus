namespace NServiceBus
{
    using Unicast.Messages;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class NativeUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        public NativeUnsubscribeTerminator(ISubscriptionManager subscriptionManager, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.subscriptionManager = subscriptionManager;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        protected override Task Terminate(IUnsubscribeContext context, CancellationToken token)
        {
            var eventMetadata = messageMetadataRegistry.GetMessageMetadata(context.EventType);
            return subscriptionManager.Unsubscribe(eventMetadata, context.Extensions);
        }

        readonly ISubscriptionManager subscriptionManager;
        readonly MessageMetadataRegistry messageMetadataRegistry;
    }
}