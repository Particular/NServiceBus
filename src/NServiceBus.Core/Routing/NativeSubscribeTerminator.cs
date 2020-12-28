using System;
using NServiceBus.Transports;
using NServiceBus.Unicast.Messages;

namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;

    class NativeSubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        public NativeSubscribeTerminator(Lazy<ISubscriptionManager> subscriptionManager, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.subscriptionManager = subscriptionManager;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        protected override Task Terminate(ISubscribeContext context)
        {
            var eventMetadata = messageMetadataRegistry.GetMessageMetadata(context.EventType);
            return subscriptionManager.Value.Subscribe(eventMetadata, context.Extensions);
        }

        readonly Lazy<ISubscriptionManager> subscriptionManager;
        readonly MessageMetadataRegistry messageMetadataRegistry;
    }
}