namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

    class NativeUnsubscribeTerminator : PipelineTerminator<UnsubscribeContext>
    {
        public NativeUnsubscribeTerminator(IManageSubscriptions subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
        }

        public override void Terminate(UnsubscribeContext context)
        {
            subscriptionManager.Unsubscribe(context.EventType, context);
        }

        IManageSubscriptions subscriptionManager;
    }
}