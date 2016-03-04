namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Transports;

    class NativeUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        public NativeUnsubscribeTerminator(IManageSubscriptions subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
        }

        protected override Task Terminate(IUnsubscribeContext context)
        {
            return subscriptionManager.Unsubscribe(context.EventType, context.Extensions);
        }

        IManageSubscriptions subscriptionManager;
    }
}