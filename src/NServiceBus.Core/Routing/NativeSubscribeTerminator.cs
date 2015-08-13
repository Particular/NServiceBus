namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

    class NativeSubscribeTerminator : PipelineTerminator<SubscribeContext>
    {
        public NativeSubscribeTerminator(IManageSubscriptions subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
        }

        public override void Terminate(SubscribeContext context)
        {
            subscriptionManager.Subscribe(context.EventType, context);
        }

        IManageSubscriptions subscriptionManager;
    }
}