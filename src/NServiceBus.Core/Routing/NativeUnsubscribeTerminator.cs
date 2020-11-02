using System;
using System.Threading;
using NServiceBus.Unicast.Messages;

namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class NativeUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        public NativeUnsubscribeTerminator(Func<IManageSubscriptions> subscriptionManager, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.subscriptionManager = subscriptionManager;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        protected override Task Terminate(IUnsubscribeContext context)
        {
            var messageMetadata = messageMetadataRegistry.GetMessageMetadata(context.EventType);
            return subscriptionManager().Unsubscribe(messageMetadata, context.Extensions, CancellationToken.None);
        }

        readonly Func<IManageSubscriptions> subscriptionManager;
        private readonly MessageMetadataRegistry messageMetadataRegistry;
    }
}