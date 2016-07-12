namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class NativeSubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        public NativeSubscribeTerminator(IManageSubscriptions subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
        }

        protected override Task Terminate(ISubscribeContext context)
        {
            return subscriptionManager.Subscribe(context.EventType, context.Extensions);
        }

        IManageSubscriptions subscriptionManager;
    }
}