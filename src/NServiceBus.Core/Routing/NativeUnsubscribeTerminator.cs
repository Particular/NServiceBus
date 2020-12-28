using System;
using NServiceBus.Transports;
using NServiceBus.Unicast.Messages;

namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;

    class NativeUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        public NativeUnsubscribeTerminator(Lazy<ISubscriptionManager> subscriptionManager, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.subscriptionManager = subscriptionManager;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        protected override Task Terminate(IUnsubscribeContext context)
        {
            var eventMetadata = messageMetadataRegistry.GetMessageMetadata(context.EventType);
            return subscriptionManager.Value.Unsubscribe(eventMetadata, context.Extensions);
        }

        readonly Lazy<ISubscriptionManager> subscriptionManager;
        readonly MessageMetadataRegistry messageMetadataRegistry;
    }
}