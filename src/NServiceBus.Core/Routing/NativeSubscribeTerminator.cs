namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Transports;

    class NativeSubscribeTerminator : PipelineTerminator<SubscribeContext>
    {
        public NativeSubscribeTerminator(IManageSubscriptions subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
        }

        protected override Task Terminate(SubscribeContext context)
        {
            return subscriptionManager.SubscribeAsync(context.EventType, context);
        }

        IManageSubscriptions subscriptionManager;
    }
}