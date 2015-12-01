namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

    class NativeUnsubscribeTerminator : PipelineTerminator<UnsubscribeContext>
    {
        public NativeUnsubscribeTerminator(IManageSubscriptions subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
        }

        protected override Task Terminate(UnsubscribeContext context)
        {
            return subscriptionManager.Unsubscribe(context.EventType, context.Extensions);
        }

        IManageSubscriptions subscriptionManager;
    }
}