namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;
    using Unicast.Messages;

    class NativeSubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        public NativeSubscribeTerminator(ISubscriptionManager subscriptionManager, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.subscriptionManager = subscriptionManager;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        protected override async Task Terminate(ISubscribeContext context)
        {
            var eventMetadata = new MessageMetadata[context.EventTypes.Length];
            for (int i = 0; i < context.EventTypes.Length; i++)
            {
                eventMetadata[i] = messageMetadataRegistry.GetMessageMetadata(context.EventTypes[i]);
            }
            try
            {
                await subscriptionManager.SubscribeAll(eventMetadata, context.Extensions, context.CancellationToken).ConfigureAwait(false);
            }
            catch (AggregateException e)
            {
                if (context.Extensions.TryGet<bool>(MessageSession.SubscribeAllFlagKey, out var flag) && flag)
                {
                    throw;
                }

                // if this is called from Subscribe, rethrow the expected single exception
                throw e.InnerException ?? e;
            }
        }

        readonly ISubscriptionManager subscriptionManager;
        readonly MessageMetadataRegistry messageMetadataRegistry;
    }
}