using System;
using System.Threading;
using NServiceBus.Unicast.Messages;

namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class NativeSubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        public NativeSubscribeTerminator(Func<ISubscriptionManager> subscriptionManager, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.subscriptionManager = subscriptionManager;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        protected override Task Terminate(ISubscribeContext context)
        {
            var metadata = messageMetadataRegistry.GetMessageMetadata(context.EventType);
            return subscriptionManager().Subscribe(metadata, context.Extensions, CancellationToken.None);
        }

        readonly Func<ISubscriptionManager> subscriptionManager;
        private readonly MessageMetadataRegistry messageMetadataRegistry;
    }
}