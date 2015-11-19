namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

    class NativeSubscribeTerminator : PipelineTerminator<SubscribeContext>
    {
        public NativeSubscribeTerminator(IManageSubscriptions subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
        }

        protected override Task Terminate(SubscribeContext context)
        {
            return subscriptionManager.Subscribe(context.EventType, context);
        }

        IManageSubscriptions subscriptionManager;
    }
}