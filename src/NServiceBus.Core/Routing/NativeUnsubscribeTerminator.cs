namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Transports;

    class NativeUnsubscribeTerminator : PipelineTerminator<UnsubscribeContext>
    {
        public NativeUnsubscribeTerminator(IManageSubscriptions subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
        }

        protected override Task Terminate(UnsubscribeContext context)
        {
            return subscriptionManager.UnsubscribeAsync(context.EventType, context);
        }

        IManageSubscriptions subscriptionManager;
    }
}